// Copyright (c) Azure Zeng. All rights reserved.
// Licensed under the Apache License 2.0. See LICENSE in the project root for license information.

namespace AzureZeng.UtilityLib.TimeZone;

#nullable disable

public class MetaZoneMapping {
    public string TimeZoneId { get; set; }
    
    public IEnumerable<MetaZoneApplyRule> ApplyRules { get; set; }
}

public class MetaZoneApplyRule {
    public DateTimeOffset? FromUtc { get; set; }
    public DateTimeOffset? ToUtc { get; set; }
    public string MetaZoneId { get; set; }
}
