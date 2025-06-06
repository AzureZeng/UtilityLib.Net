// Copyright (c) Azure Zeng. All rights reserved.
// Licensed under the Apache License 2.0. See LICENSE in the project root for license information.

using AzureZeng.UtilityLib.TimeZone;
using System.Resources;

namespace AzureZeng.UtilityLib.Resources;

internal class LikelySubTagResource : ILikelySubTagLookup{
    private static readonly ResourceManager ResourceManager = new ResourceManager(typeof(LikelySubTagResource));

    public static ILikelySubTagLookup Instance { get; } = new LikelySubTagResource();
    
    public LanguageTag Fill(LanguageTag languageTag) {
                Func<string?>[] lookups = [
            () => {
                if (string.IsNullOrEmpty(languageTag.Territory) || string.IsNullOrEmpty(languageTag.Script) ||
                    string.IsNullOrEmpty(languageTag.Variant)) return null;
                var und = languageTag;
                und.Language = "und";
                string? ret = ResourceManager.GetString(languageTag.ToString('_').ToLower());
                if (!string.IsNullOrEmpty(ret)) return ret;
                ret = ResourceManager.GetString(und.ToString('_').ToLower());
                return string.IsNullOrEmpty(ret) ? null : ret;
            },
            () => {
                if (string.IsNullOrEmpty(languageTag.Script)) return null;
                var lookupA = languageTag;
                lookupA.Territory = null;
                lookupA.Variant = null;
                var und = lookupA;
                und.Language = "und";
                string? ret = ResourceManager.GetString(lookupA.ToString('_').ToLower());
                if (!string.IsNullOrEmpty(ret)) return ret;
                ret = ResourceManager.GetString(und.ToString('_').ToLower());
                return string.IsNullOrEmpty(ret) ? null : ret;
            },
            () => {
                if (string.IsNullOrEmpty(languageTag.Territory)) return null;
                var lookupA = languageTag;
                lookupA.Script = null;
                lookupA.Variant = null;
                var und = lookupA;
                und.Language = "und";
                string? ret = ResourceManager.GetString(lookupA.ToString('_').ToLower());
                if (!string.IsNullOrEmpty(ret)) return ret;
                ret = ResourceManager.GetString(und.ToString('_').ToLower());
                return string.IsNullOrEmpty(ret) ? null : ret;
            },
            () => ResourceManager.GetString(languageTag.Language)
        ];

        string? lookup = null;
        foreach (var f in lookups) {
            lookup = f();
            if (!string.IsNullOrEmpty(lookup)) break;
        }

        if (lookup == null) return new LanguageTag() { Language = "" };
        var l = new LanguageTag(lookup);
        if (!string.IsNullOrEmpty(languageTag.Territory)) l.Territory = languageTag.Territory;
        if (!string.IsNullOrEmpty(languageTag.Script)) l.Script = languageTag.Script;
        if (!string.IsNullOrEmpty(languageTag.Variant)) l.Variant = languageTag.Variant;
        return l;
    }
}
