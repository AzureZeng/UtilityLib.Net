using AzureZeng.UtilityLib.TimeZone;
using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using System.Xml.XPath;

namespace AzureZeng.Utilities;

public static class Program {
    public static void Main(string[] args) {
        var mappings = ReadMetaZoneMappingFile("/Volumes/DataVol/temp/cldr-47-full/common/supplemental/metaZones.xml");
        File.WriteAllText("/Volumes/DataVol/temp/json.json",
            JsonSerializer.Serialize(mappings,
                new JsonSerializerOptions {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }));
    }

    public static IEnumerable<MetaZoneMapping> ReadMetaZoneMappingFile(string path) {
        XDocument document = XDocument.Load(path);
        return document.XPathSelectElements("/supplementalData/metaZones/metazoneInfo/timezone").Select(x => {
            var metaZoneMapping = new MetaZoneMapping { TimeZoneId = x.Attribute("type")?.Value ?? string.Empty };
            metaZoneMapping.ApplyRules = x.Elements("usesMetazone").Select(y => {
                var from = y.Attribute("from")?.Value;
                var to = y.Attribute("to")?.Value;
                var mzone = y.Attribute("mzone")?.Value;

                return new MetaZoneApplyRule() {
                    MetaZoneId = mzone,
                    FromUtc = string.IsNullOrEmpty(from)
                        ? null
                        : DateTimeOffset.Parse(from, null, DateTimeStyles.AssumeUniversal),
                    ToUtc = string.IsNullOrEmpty(to)
                        ? null
                        : DateTimeOffset.Parse(to, null, DateTimeStyles.AssumeUniversal)
                };
            }).ToImmutableArray();
            return metaZoneMapping;
        }).ToArray();
    }
}
