// Copyright (c) Azure Zeng. All rights reserved.
// Licensed under the Apache License 2.0. See LICENSE in the project root for license information.

using AzureZeng.UtilityLib.TimeZone;
using System.Xml;

namespace AzureZeng.Utilities;

public class CldrDataParser {
    public Dictionary<string, string> Territories = new Dictionary<string, string>();
    public Dictionary<string, string> Languages = new Dictionary<string, string>();
    public Dictionary<string, TimeZoneLoc> TimeZones = new Dictionary<string, TimeZoneLoc>();
    
}
