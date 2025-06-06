// Copyright (c) Azure Zeng. All rights reserved.
// Licensed under the Apache License 2.0. See LICENSE in the project root for license information.

namespace AzureZeng.UtilityLib.TimeZone;

public class TimeZoneLoc {
    public string TimeZoneId { get; set; } = string.Empty;
    public ITimeZoneTranslation? Long { get; set; }
    public ITimeZoneTranslation? Short { get; set; }
}

public interface ITimeZoneTranslation {
    public string Daylight { get; }
    public string Standard { get; }
    public string Generic { get; }
}

public class MetaZone {
    public string MetaZoneId { get; set; } = null!;
    public ITimeZoneTranslation Long { get; set; } = null!;
    public ITimeZoneTranslation Short { get; set; } = null!;
}

public class TimeZoneTranslation : ITimeZoneTranslation {
    public string Daylight { get; set; } = null!;
    public string Standard { get; set; } = null!;
    public string Generic { get; set; } = null!;
}
