﻿using System;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("✓ Outlook Email Safety Checks")]
[assembly: AssemblyDescription("https://github.com/CodeChex/OutlookSafetyChecks")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("John ✓ Checco (@CodeChex)")]
[assembly: AssemblyProduct("OutlookSafetyChex")]
[assembly: AssemblyCopyright("© Copyright 2018-2022, Checco Services")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(true)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("784d73ec-37da-4cb7-a011-e4265664dd22")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.04.*")]
[assembly: AssemblyFileVersion("1.22.01.18")]

[assembly: log4net.Config.XmlConfigurator(ConfigFile = "log4net.config")]
[assembly: NeutralResourcesLanguage("en")]
