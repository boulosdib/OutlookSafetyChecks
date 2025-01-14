﻿using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using NetTools;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace CheccoSafetyTools
{
    abstract class cst_Util
    {
        // static thread-safe members
        private static String rgxWordPattern = @"\b(\w+)\b";
        private static String rgxLeetPattern = @"([a-zA-Z]\d+[a-zA-Z])";

        #region  Array/list utils
        public static bool isValidArray(dynamic[] tArr)
        {
            if (tArr == null) return false;
            return (tArr.Length > 0);
        }

        public static bool isValidCollection(IEnumerable<dynamic> tArr)
        {
            if (tArr == null) return false;
            return (tArr.Count() > 0);
        }

        #endregion

        #region string utils
        public static List<String> getWordList(String tStr)
        {
            List<String> rc = new List<String>();
            try
            {
                // looking for strings that display leetspeak
                if (isValidString(tStr))
                {
                    // foreach word in the string:
                    Regex rgxWord = new Regex(rgxWordPattern, RegexOptions.Compiled);
                    MatchCollection mWords = rgxWord.Matches(tStr);
                    foreach (Match match in mWords)
                    {
                        String word = match.Value.Trim();
                        if (isValidString(word) && word.Length > 1)
                        {
                            rc.Add(word);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // if (mLogger != null) mLogger.logException(ex, "AddInSafetyCheck::getWordList(" + tStr + ")");
            }
            return rc;
        }

        public static bool isValidString(String tStr, bool trimFirst = true)
        {
            if (tStr == null) return false;
            String chk = trimFirst ? tStr.Trim() : tStr;
            return (chk.Length > 0);
        }

        public static bool isValidURL(String tStr)
        {
            cst_URL tURL = cst_URL.parseURL(tStr);
            return (tURL != null);
        }

        public static bool containsLeet(String word)
        {
            bool rc = false;
            // Determine Leet substitutions (O=0, I=1, Z=2, E=3, H=4, S=5, G=6, T=7, B=8, Q=9)
            Regex rgxLeet = new Regex(rgxLeetPattern, RegexOptions.Compiled);
            Match mLeet = rgxLeet.Match(word);
            rc = (mLeet.Groups.Count > 1);
            return rc;
        }

        public static String toAscii(String tStr)
        {
            String rc = "";
            try
            {
                var tBytes = System.Text.Encoding.UTF8.GetBytes(tStr);
                rc = System.Text.Encoding.ASCII.GetString(tBytes);
            }
            catch (Exception)
            {
                // ignore all errors
                rc = "";
            }
            // trying to be safe
            return rc;
        }

        public static String RemoveDiacritics(String text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            text = text.Normalize(NormalizationForm.FormD);
            var chars = text.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray();
            return new String(chars).Normalize(NormalizationForm.FormC);
        }

        public static String B64encode(String tStr)
        {
            String rc = "";
            try
            {
                var tBytes = System.Text.Encoding.UTF8.GetBytes(tStr);
                rc = System.Convert.ToBase64String(tBytes);
            }
            catch (Exception)
            {
                // ignore all errors
                rc = "";
            }
            // trying to be safe
            return rc;
        }

        public static String B64decode(String tStr)
        {
            String rc = "";
            try
            {
                var tBytes = System.Convert.FromBase64String(tStr);
                rc = System.Text.Encoding.UTF8.GetString(tBytes);
            }
            catch (Exception)
            {
                // ignore all errors
                rc = "";
            }
            // trying to be safe
            return rc;
        }
        #endregion
    } // class

    public class cst_URL
    {
        public readonly String mURL = null;
        public readonly String mProtocol = null;
        public readonly String mHost = null;
        public readonly String mPort = null;
        public readonly String mPath = null;
        public readonly String mQuery = null;
        public readonly String mRef = null;
        public readonly Uri mUri = null;

        private static String rgxURIpattern = @"(?<url>"
                + @"(?<protocol>[a-z]+):(//)?"
                + @"(?<host>[^\s:/\?#]+)?"
                + @"(:(?<port>[\d]+))?"
                + @"(?<path>/+[^\?#]+)?"
                + @"(\?(?<query>[^#]+))?"
                + @"(#(?<ref>.+))?"
                + @")";
        // @"(?<url>(http(s?)://|file://|mailto:|(s?)ftp(s?):|scp:|www.)([a-z]|[A-Z]|[0-9]|[\\-]|[/.]|[~])*)";

        public cst_URL(string tURL, 
            string tProtocol, string tHost, string tPort,
            string tPath, string tQuery, string tRef)
        {
            this.mURL = tURL;
            this.mProtocol = tProtocol;
            this.mHost = tHost;
            this.mPort = tPort;
            this.mPath = tPath;
            this.mQuery = tQuery;
            this.mRef = tRef;
            if ( !cst_Util.isValidString(tProtocol) ||
                (!cst_Util.isValidString(tHost) && !cst_Util.isValidString(tPath)))
            {
                throw new Exception("Does not meet minimum URL Fields");
            }
            this.mUri = new Uri(tURL);
        }

        public cst_URL(string tURL)
        {
            this.mUri = new Uri(tURL);
            this.mURL = tURL;
            this.mProtocol = this.mUri.Scheme;
            this.mHost = this.mUri.Host;
            this.mPort = this.mUri.Port.ToString();
            this.mPath = this.mUri.AbsolutePath;
            this.mQuery = this.mUri.Query;
            this.mRef = this.mUri.Fragment;
        }

        private static cst_URL parseFields(GroupCollection tFound)
        {
            cst_URL rc = null;
            try
            {
                String tURL = tFound["url"].Value;
                String tProtocol = tFound["protocol"].Value;
                String tHost = tFound["host"].Value;
                String tPort = tFound["port"].Value;
                String tPath = tFound["path"].Value;
                String tQuery = tFound["query"].Value;
                String tRef = tFound["ref"].Value;
                if (cst_Util.isValidString(tProtocol) &&
                    (cst_Util.isValidString(tHost) || cst_Util.isValidString(tPath)))
                {
                    rc = new cst_URL(tURL, tProtocol, tHost, tPort, tPath, tQuery, tRef);
                }
            }
            catch { }
            return rc;
        }

        public static cst_URL parseURL(String tStr,bool strict=true)
        {
            cst_URL rc = null;
            try
            {
                // look for exactly one instance
                String localPattern = strict ? 
                    @"^" + rgxURIpattern + @"$" : 
                    @"\b*" + rgxURIpattern + @"\b*";
                Regex rgxURI = new Regex(localPattern,
                                        RegexOptions.Compiled |
                                        RegexOptions.IgnoreCase |
                                        RegexOptions.ExplicitCapture);
                MatchCollection matches = rgxURI.Matches(tStr);
                if (matches.Count == 1)
                {
                    rc = cst_URL.parseFields(matches[0].Groups);
                }
            }
            catch { }
            return rc;
        }

        public static List<cst_URL> parseTextForURLs(String tStr, ushort max = 0)
        {
            List<cst_URL> rc = new List<cst_URL>();
            try
            {
                Regex rgxURI = new Regex(rgxURIpattern,
                                        RegexOptions.Compiled |
                                        RegexOptions.IgnoreCase |
                                        RegexOptions.ExplicitCapture);
                MatchCollection matches = rgxURI.Matches(tStr);
                foreach (Match match in matches)
                {
                    cst_URL tFound = cst_URL.parseFields(matches[0].Groups);
                    if ( tFound != null ) rc.Add(tFound);
                }
            }
            catch { }
            return rc;
        }

    }

    public class cst_Log
    {
        public const ushort LOG_NONE = 0;
        public const ushort LOG_ERROR = 1;
        public const ushort LOG_INFO = 2;
        public const ushort LOG_VERBOSE = 3;
        public const ushort LOG_ALL = 99;

        private readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public List<TextBox> arrLogger = new List<TextBox>();
        private List<TextBox> arrStatus = new List<TextBox>();

        private uint lines = 0;

       private void updateLogUI(String s, bool erase = false)
        {
            if (erase) lines = 0;
            lines++;
            String msg = s.Trim();
            if (cst_Util.isValidString(msg))
                msg = "[" + lines + "] " + msg;
            appendLogUI(msg, erase);
            updateStatusLine(msg, true);
        }

        private void updateTextBox(TextBox ctl, String msg, bool erase = false)
        {
            try
            {
                if (erase)
                {
                    if (ctl.InvokeRequired)
                        ctl.Invoke(new Action(delegate () {
                            ctl.Clear();
                        }));
                    else
                        ctl.Clear();
                }
                if (cst_Util.isValidString(msg))
                {
                    if (ctl.InvokeRequired)
                        ctl.Invoke(new Action(delegate () {
                            ctl.AppendText("\r\n" + msg);
                        }));
                    else
                        ctl.AppendText("\r\n" + msg);
                }
                if (ctl.InvokeRequired)
                    ctl.Invoke(new Action(delegate () {
                        ctl.Refresh();
                    }));
                else
                    ctl.Refresh();
            }
            catch (Exception ex)
            {
                log.Error("cst_Util::updateTextBox()", ex);
            }
        }

        private void appendLogUI(String s, bool erase = false)
        {
            // update logging window
            foreach (TextBox ctlLogger in arrLogger)
            {
                updateTextBox(ctlLogger, s, erase);
            } 
        }

        private void updateStatusLine(String s, bool erase = false)
        {
            // update status line
            foreach (TextBox ctlStatus in arrStatus)
            {
                updateTextBox(ctlStatus, s, erase);
            }
        }

        private String prepareLogMsg(String details, String context)
        {
            Thread z = Thread.CurrentThread;
            String rc = "[Thread " + z.ManagedThreadId + "]: ";
            if (cst_Util.isValidString(context))
            {
                rc += context.Trim();
            }
            if (cst_Util.isValidString(details))
            {
                rc += " - " + details.Trim();
            }
            return rc;
        }

        public void logVerbose(String details, String context, bool erase = false)
        {
            if (OutlookSafetyChex.Properties.Settings.Default.log_Level >= LOG_VERBOSE
                && cst_Util.isValidString(details))
            {
                String msg = prepareLogMsg(details, context);
                log.Debug(msg);
                updateLogUI("[VERBOSE]: " + msg, erase);
            }
        }

        public void logInfo(String details, String context, bool erase = false)
        {
            if (OutlookSafetyChex.Properties.Settings.Default.log_Level >= LOG_INFO &&
                cst_Util.isValidString(details))
            {
                String msg = prepareLogMsg(details, context);
                log.Info(msg);
                updateLogUI("[INFO]: " + msg, erase);
            }
        }

        public void logException(Exception ex, String context, bool erase = false)
        {
            if (OutlookSafetyChex.Properties.Settings.Default.log_Level >= LOG_ERROR &&
                ex != null)
            {
                String msg = prepareLogMsg(ex.Message, context);
                log.Error(context, ex);
                updateLogUI("[EXCEPTION]: " + msg, erase);
            }
        }

        public void logMessage(String details, String context, bool erase = false)
        {
            if (cst_Util.isValidString(details))
            {
                String msg = prepareLogMsg(details, context);
                log.Info(msg);
                updateLogUI(msg, erase);
            }
        }

        public void setLoggingUI(TextBox[] wndLogger, TextBox[] wndStatus = null)
        {
            arrLogger = new List<TextBox>();
            if (wndLogger != null) arrLogger.AddRange(wndLogger);
            arrStatus = new List<TextBox>();
            if (wndStatus != null) arrStatus.AddRange(wndStatus);
        }

    }

    public class cst_Web
    {
        private cst_Log mLogger = null;

        private static String rgxIPAddrPattern = @"(\d+\.\d+\.\d+\.\d+)";
        // non-static member
        public HtmlParser htmlParser = new HtmlParser();
        public IdnMapping idnMapping = new IdnMapping();

 
        public cst_Web(cst_Log tLogger)
        {
            mLogger = tLogger;
        }

        public String pullDomain(String fqdn)
        {
            String rc = fqdn;
            try
            {
                String[] parts = fqdn.Split('.');
                int Z = parts.Length - 1;
                if (Z > 1) rc = parts[Z - 1] + "." + parts[Z];
                // special cases
                if (rc.Equals("co.uk", StringComparison.OrdinalIgnoreCase) && parts.Length > 2)
                {
                    rc = parts[Z - 2] + '.' + rc;
                }
            }
            catch (Exception ex)
            {
                if (mLogger != null) mLogger.logException(ex, "cst_Util::pullDomain(" + fqdn + ")");
            }
            // trying to be safe
            return rc;
        }

        public String pullTLD(String fqdn)
        {
            String rc = fqdn;
            try
            {
                String[] parts = fqdn.Split('.');
                int Z = parts.Length - 1;
                if (Z > 0) rc = parts[Z];
            }
            catch (Exception ex)
            {
                if (mLogger != null) mLogger.logException(ex, "cst_Util::pullTLD(" + fqdn + ")");
            }
            // trying to be safe
            return rc;
        }

        public String parseIPaddress(String tStr)
        {
            String rc = "";
            try
            {
                if (cst_Util.isValidString(tStr))
                {
                    Regex rgxIPAddr = new Regex(rgxIPAddrPattern, RegexOptions.Compiled);
                    Match m = rgxIPAddr.Match(tStr.Trim());
                    if (m.Groups.Count > 1)
                    {
                        rc = m.Groups[1].Value.Trim();
                    }
                }
            }
            catch (Exception ex)
            {
                if (mLogger != null) mLogger.logException(ex, "cst_Util::parseIPaddress(" + tStr + ")");
            }
            return rc;
        }

        public IPAddress toIPaddress(String tStr)
        {
            IPAddress rc = null;
            try
            {
                IPAddressRange rcIP = null;
                if (IPAddressRange.TryParse(tStr, out rcIP))
                    rc = rcIP.Begin;
            }
            catch { }
            return rc;
        }

        public List<IPAddress> listIPaddress(String tStr, ushort maxLen = 0)
        {
            List<IPAddress> rc = new List<IPAddress>();
            try
            {
                IPAddressRange rcIP = null;
                if (IPAddressRange.TryParse(tStr, out rcIP))
                {
                    foreach (IPAddress ip in rcIP)
                    {
                        if (ip != null)
                        {
                            rc.Add(ip);
                            if (rc.Count == maxLen) break;
                        }
                    }
                }
            }
            catch { }
            return rc;
        }

        public bool isValidIPAddress(String tStr)
        {
            IPAddress ip = toIPaddress(tStr);
            bool rc = (ip != null);
            /*
            try
            {
                if (isValidString(tStr))
                {
                    String rgxStr = @"(\d+\.\d+\.\d+\.\d+)";
                    Regex rgx = new Regex(rgxStr);
                    Match m = rgx.Match(tStr.Trim());
                    rc = (m.Groups.Count > 1);
                }
            }
			catch (Exception ex)
            {
                if (mLogger != null) mLogger.logException(ex, "cst_Util::isValidIPAddress(" + tStr + ")");
			}
            */
            return rc;
        }

        public String sanitizeEmail(String inAddr, bool strict)
        {
            String rc = strict ? "" : inAddr;
            try
            {
                // sanitizing address
                String rgxStr = @"[<']?\s*([a-zA-Z0-9_=\-\.]+@[a-zA-Z0-9_\-\.]+\.[a-zA-Z0-9]+)\s*['>]?";
                Regex rgx = new Regex(rgxStr);
                Match m = rgx.Match(inAddr.Trim());
                if (m.Groups.Count > 1)
                {
                    rc = m.Groups[1].Value.Trim();
                }
            }
            catch (Exception ex)
            {
                if (mLogger != null) mLogger.logException(ex, "cst_Util::sanitizeEmail(" + inAddr + ")");
            }
            return rc;
        }
        public String wgetContentType(String tURL, Dictionary<String, String> arrHeaders = null)
        {
            String rc = null;
            WebHeaderCollection arr = wgetHead(tURL, arrHeaders);
            if (arr != null)
            {
                rc = arr.Get("Content-Type");
                if (!cst_Util.isValidString(rc)) rc = arr.Get("[Exception]");
            }
            return rc;
        }

        public WebHeaderCollection wgetHead(String tURL, Dictionary<String, String> arrHeaders = null)
        {
            WebHeaderCollection rc = null;
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                //ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                HttpWebRequest tReq = WebRequest.CreateHttp(tURL);
                tReq.Timeout = 2000;
                tReq.AllowAutoRedirect = true;
                tReq.Method = "HEAD";
                if (arrHeaders != null)
                {
                    foreach (String t in arrHeaders.Keys)
                    {
                        tReq.Headers.Add(t, arrHeaders[t]);
                    }
                }
                WebResponse tResp = tReq.GetResponse();
                if (tResp != null)
                {
                    rc = tResp.Headers;
                }
            }
            catch (WebException we)
            {
                if (mLogger != null) mLogger.logException(we, "cst_Util::wgetHead(" + tURL + ")");
                rc.Add("[Exception]", we.Message);
            }
            catch (Exception ex)
            {
                if (mLogger != null) mLogger.logException(ex, "cst_Util::wgetHead(" + tURL + ")");
                rc.Add("[Exception]", ex.Message);
            }
            return rc;
        }

        public byte[] wgetBinary(String tURL, String[] allowableContentTypes = null, Dictionary<String, String> arrHeaders = null)
        {
            byte[] rc = null;
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                //ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                using (WebClient tClient = new WebClient())
                {
                    if (arrHeaders != null)
                    {
                        foreach (String t in arrHeaders.Keys)
                        {
                            tClient.Headers.Add(t, arrHeaders[t]);
                        }
                    }
                    rc = tClient.DownloadData(tURL);
                    // do we need to validate allowable content type?
                    if (allowableContentTypes != null && allowableContentTypes.Length > 0)
                    {
                        int foundContent = 0;
                        // Obtain the WebHeaderCollection instance containing the header name/value pair from the response.
                        WebHeaderCollection tResponseHeaders = tClient.ResponseHeaders;
                        // Loop through the ResponseHeaders and display the header name/value pairs.
                        String[] arrResp = tResponseHeaders.GetValues("Content-Type");
                        foreach (String tVal in arrResp)
                        {
                            foreach (String tContentType in allowableContentTypes)
                            {
                                if (tVal.StartsWith(tContentType, StringComparison.CurrentCultureIgnoreCase))
                                {
                                    foundContent++;
                                }
                            }
                        }
                        if (foundContent == 0)
                        {
                            if (mLogger != null) mLogger.logInfo("No Allowable Content-Type Found", "cst_Util::wgetBinary(" + tURL + ")");
                            rc = null;
                        }
                    }
                }
            }
            catch (WebException webEx)
            {
                if (mLogger != null) mLogger.logException(webEx, "cst_Util::wgetData(" + tURL + ")");
            }
            catch (Exception ex)
            {
                if (mLogger != null) mLogger.logException(ex, "cst_Util::wgetData(" + tURL + ")");
            }
            return rc;
        }

        public String wgetString(String tURL, String[] allowableContentTypes = null, Dictionary<String, String> arrHeaders = null)
        {
            String rc = null;
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                //ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                using (WebClient tClient = new WebClient())
                {
                    if (arrHeaders != null)
                    {
                        foreach (String t in arrHeaders.Keys)
                        {
                            tClient.Headers.Add(t, arrHeaders[t]);
                        }
                    }
                    rc = tClient.DownloadString(tURL);
                    // do we need to validate allowable content type?
                    if (allowableContentTypes != null && allowableContentTypes.Length > 0)
                    {
                        int foundContent = 0;
                        // Obtain the WebHeaderCollection instance containing the header name/value pair from the response.
                        WebHeaderCollection tResponseHeaders = tClient.ResponseHeaders;
                        // Loop through the ResponseHeaders and display the header name/value pairs.
                        String[] arrResp = tResponseHeaders.GetValues("Content-Type");
                        foreach (String tVal in arrResp)
                        {
                            foreach (String tContentType in allowableContentTypes)
                            {
                                if (tVal.StartsWith(tContentType, StringComparison.CurrentCultureIgnoreCase))
                                {
                                    foundContent++;
                                }
                            }
                        }
                        if (foundContent == 0)
                        {
                            if (mLogger != null) mLogger.logInfo("No Allowable Content-Type Found", "cst_Util::wgetString(" + tURL + ")");
                            rc = null;
                        }
                    }
                }
            }
            catch (WebException webEx)
            {
                if (mLogger != null) mLogger.logException(webEx, "cst_Util::wgetString(" + tURL + ")");
            }
            catch (Exception ex)
            {
                if (mLogger != null) mLogger.logException(ex, "cst_Util::wgetString(" + tURL + ")");
            }
            return rc;
        }

        public IHtmlDocument wgetHTML(String tURL, Dictionary<String, String> arrHeaders = null)
        {
            IHtmlDocument rc = null;
            try
            {
                String tHtml = wgetString(tURL, new[] { MediaTypeNames.Text.Html },
                                            arrHeaders);
                if (cst_Util.isValidString(tHtml))
                {
                    rc = htmlParser.ParseDocument(tHtml);
                }
            }
            catch (Exception ex)
            {
                if (mLogger != null) mLogger.logException(ex, "cst_Util::wgetHTML(" + tURL + ")");
            }
            return rc;
        }

        public JToken wgetJSON(String tURL, Dictionary<String, String> arrHeaders = null)
        {
            JToken rc = null;
            try
            {
                String results = wgetString(tURL, new[] { MediaTypeNames.Text.Plain, "application/json" },
                                            arrHeaders);
                if (cst_Util.isValidString(results))
                {
                    rc = JToken.Parse(results);
                    /*
                    if ( results.StartsWith("{") ) rc = JObject.Parse(results);
                    else if (results.StartsWith("[") ) rc = JArray.Parse(results);
                    */
                }
            }
            catch (Exception ex)
            {
                if (mLogger != null) mLogger.logException(ex, "cst_Util::wgetJSON(" + tURL + ")");
            }
            return rc;
        }
    }

     public class AssemblyInfo
    {
        // The assembly information values.
        public string Title = "",
            Description = "",
            Company = "",
            Product = "",
            Copyright = "",
            Version = "";

        // Return a particular assembly attribute value.
        public static T GetAssemblyAttribute<T>(Assembly assembly)
            where T : Attribute
        {
            // Get attributes of this type.
            object[] attributes =
                assembly.GetCustomAttributes(typeof(T), true);

            // If we didn't get anything, return null.
            if ((attributes == null) || (attributes.Length == 0))
                return null;

            // Convert the first attribute value into
            // the desired type and return it.
            return (T)attributes[0];
        }

        // Constructors.
        public AssemblyInfo()
            : this(Assembly.GetExecutingAssembly())
        {
        }

        public AssemblyInfo(Assembly assembly)
        {
            // Get values from the assembly.
            AssemblyTitleAttribute titleAttr =
                GetAssemblyAttribute<AssemblyTitleAttribute>(assembly);
            if (titleAttr != null) Title = titleAttr.Title;

            AssemblyDescriptionAttribute assemblyAttr =
                GetAssemblyAttribute<AssemblyDescriptionAttribute>(assembly);
            if (assemblyAttr != null) Description =
                assemblyAttr.Description;

            AssemblyCompanyAttribute companyAttr =
                GetAssemblyAttribute<AssemblyCompanyAttribute>(assembly);
            if (companyAttr != null) Company = companyAttr.Company;

            AssemblyProductAttribute productAttr =
                GetAssemblyAttribute<AssemblyProductAttribute>(assembly);
            if (productAttr != null) Product = productAttr.Product;

            AssemblyCopyrightAttribute copyrightAttr =
                GetAssemblyAttribute<AssemblyCopyrightAttribute>(assembly);
            if (copyrightAttr != null) Copyright = copyrightAttr.Copyright;

            Version = assembly.GetName().Version.ToString();
        }
    }
} // namespace
