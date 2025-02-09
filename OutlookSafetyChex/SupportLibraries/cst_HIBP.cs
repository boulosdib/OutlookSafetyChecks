﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading;

namespace CheccoSafetyTools
{
    public class cst_HIBP
	{
		protected readonly cst_Log mLogger = null;
		protected readonly cst_Web mWebUtil = null;

		public cst_HIBP(cst_Log tLogger, cst_Web tWebUtil)
		{
			this.mLogger = tLogger;
			this.mWebUtil = tWebUtil;
		}

		private static Dictionary<String, String> tHeaders = new Dictionary<String,String>() {
            {  "User-Agent", "Pwnage-Checker-CheccoSafetyTools" },
        };
		private readonly static String HIBP_URL= "https://haveibeenpwned.com/api/v2";
		private readonly static Dictionary<String, JToken> accountCache = new Dictionary<String, JToken>();
		private readonly static Dictionary<String, JToken> emailCache = new Dictionary<String, JToken>();
		private readonly static Dictionary<String, JToken> domainCache = new Dictionary<String, JToken>();

		/* Services
			"breachedaccount/{accountname}"
			"pasteaccount/{email}",
			"breaches?domain={domain}",
		*/

		public void clearCaches()
		{
			accountCache.Clear();
			emailCache.Clear();
			domainCache.Clear();
		}

		private Dictionary<String, String> parseHIBP(JToken json)
		{
			Dictionary<String, String> rc = new Dictionary<String, String>();
			try
			{
				if (json != null)
				{
					foreach ( JToken jsTok in json.Children() )
					{
                        Boolean tVerified = jsTok.Value<Boolean>("IsVerified");
                        Boolean tActive = jsTok.Value<Boolean>("IsActive");
                        Boolean tFake = jsTok.Value<Boolean>("IsFabricated");
						if ( tVerified && tActive && !tFake )
						{
							String tKey = jsTok.Value<String>("Name");
							String tDomain = jsTok.Value<String>("Domain");
							DateTime tDate = jsTok.Value<DateTime>("BreachDate");
							JArray jaClasses = jsTok.Value<JArray>("DataClasses");
							String tDetails = jsTok.Value<String>("Description");
                            String tTarget = cst_Util.isValidString(tDomain) ? tDomain : tKey;
							String tDesc = "Breached \"" + tTarget + "\" (" + tDate.ToShortDateString() + "): ";
							foreach ( JToken jsClass in jaClasses )
							{
								String tClass = jsClass.Value<String>();
								tDesc += tClass + ", ";
							}
							rc.Add(tKey, tDesc);							  
						}
					}
				}
			}
			catch (Exception ex)
			{
				if (mLogger != null) mLogger.logException(ex, "cst_HIBP::parseHIBP");
			}
			return rc;
		}

		public Dictionary<String,String> wasEmailPasted(String tEmail, bool use_CACHE)
		{
			Dictionary<String, String> rc = new Dictionary<String, String>();
			try
			{
				String inStr = tEmail.ToLower().Trim();
				JToken json = null;
                Boolean isCached = accountCache.TryGetValue(inStr, out json);
                if (!use_CACHE || !isCached)
				{
                    Thread.Sleep(1500); // yep, it's necessary
					json = mWebUtil.wgetJSON(HIBP_URL + "/pasteaccount/" + inStr, tHeaders);
                    if (!isCached) accountCache.Add(inStr, json);
				}
				if ( json != null ) rc = parseHIBP(json);
			}
			catch (Exception ex)
			{
				if (mLogger != null) mLogger.logException(ex, "cst_HIBP::wasEmailPasted(" + tEmail+")");
			}
			return rc;
		}

		public Dictionary<String, String> wasEmailBreached(String tStr, bool use_CACHE)
		{
			Dictionary<String, String> rc = new Dictionary<String, String>();
			try
			{
				String inStr = tStr.ToLower().Trim();
                JToken json = null;
                Boolean isCached = emailCache.TryGetValue(inStr, out json);
                if (!use_CACHE || !isCached)
				{
                    Thread.Sleep(1500); // yep, it's necessary
                    json = mWebUtil.wgetJSON(HIBP_URL + "/breachedaccount/" + inStr, tHeaders);
                    if (!isCached) emailCache.Add(inStr, json);
				}
                if (json != null) rc = parseHIBP(json);
			}
			catch (Exception ex)
			{
                if (mLogger != null) mLogger.logException(ex, "cst_HIBP::wasEmailBreached(" + tStr + ")");
            }
            return rc;
		}

		public Dictionary<String, String> wasDomainBreached(String tStr, bool use_CACHE)
		{
			Dictionary<String, String> rc = new Dictionary<String, String>();
			try
			{
				String inStr = tStr.ToLower().Trim();
                JToken json = null;
                Boolean isCached = domainCache.TryGetValue(inStr, out json);
                if (!use_CACHE || !isCached)
				{
                    Thread.Sleep(1500); // yep, it's necessary
                    json = mWebUtil.wgetJSON(HIBP_URL + "/breaches?domain=" + inStr, tHeaders);
                    if (!isCached) domainCache.Add(inStr, json);
				}
                if (json != null) rc = parseHIBP(json);
			}
			catch (Exception ex)
			{
                if (mLogger != null) mLogger.logException(ex, "cst_HIBP::wasDomainBreached(" + tStr + ")");
            }
            return rc;
		}
	} // class
} // namespace
