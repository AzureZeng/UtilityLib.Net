// Copyright (c) Azure Zeng. All rights reserved.
// Licensed under the Apache License 2.0. See LICENSE in the project root for license information.

using AzureZeng.UtilityLib.TimeZone;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Xml.Linq;
using System.Xml.XPath;

namespace AzureZeng.Utilities;

public class LikelySubtagFileReader : ILikelySubTagLookup {
    private Dictionary<string, string> _database = new();

    public async Task LoadFromCldrAsync(string fileName) {
        await using var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        var xdoc = await XDocument.LoadAsync(fs, LoadOptions.None, CancellationToken.None);
        _database = xdoc.XPathSelectElements("/supplementalData/likelySubtags/likelySubtag")
            .ToDictionary(e => e.Attribute("from")?.Value.ToLower() ?? throw new FormatException("Invalid file format"),
                e => e.Attribute("to")?.Value ?? throw new FormatException("Invalid file format"));
    }

    public Task CompileToJsonAsync(string fileName) {
        return File.WriteAllTextAsync(fileName,
            JsonSerializer.Serialize(_database,
                new JsonSerializerOptions() {
                    WriteIndented = true, Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
                }), new UTF8Encoding(false));
    }

    public LanguageTag Fill(LanguageTag languageTag) {
        Func<string?>[] lookups = [
            () => {
                if (string.IsNullOrEmpty(languageTag.Territory) || string.IsNullOrEmpty(languageTag.Script) ||
                    string.IsNullOrEmpty(languageTag.Variant)) return null;
                var und = languageTag;
                und.Language = "und";
                if (_database.TryGetValue(languageTag.ToString('_').ToLower(), out var ret)) return ret;
                return _database.TryGetValue(und.ToString('_').ToLower(), out ret) ? ret : null;
            },
            () => {
                if (string.IsNullOrEmpty(languageTag.Script)) return null;
                var lookupA = languageTag;
                lookupA.Territory = null;
                lookupA.Variant = null;
                var und = lookupA;
                und.Language = "und";
                if (_database.TryGetValue(lookupA.ToString('_').ToLower(), out var ret)) return ret;
                return _database.TryGetValue(und.ToString('_').ToLower(), out ret) ? ret : null;
            },
            () => {
                if (string.IsNullOrEmpty(languageTag.Territory)) return null;
                var lookupA = languageTag;
                lookupA.Script = null;
                lookupA.Variant = null;
                var und = lookupA;
                und.Language = "und";
                if (_database.TryGetValue(lookupA.ToString('_').ToLower(), out var ret)) return ret;
                return _database.TryGetValue(und.ToString('_').ToLower(), out ret) ? ret : null;
            },
            () => _database.GetValueOrDefault(languageTag.Language)
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
