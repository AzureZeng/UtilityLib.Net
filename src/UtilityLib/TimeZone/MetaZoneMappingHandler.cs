// Copyright (c) Azure Zeng. All rights reserved.
// Licensed under the Apache License 2.0. See LICENSE in the project root for license information.

using AzureZeng.UtilityLib.Resources;
using System.Text.Json;

namespace AzureZeng.UtilityLib.TimeZone;

public static class MetaZoneMappingHandler {
    private static readonly Lazy<Dictionary<string, MetaZoneMapping>> _mappings = new(CreateMappings);

    private static Dictionary<string, MetaZoneMapping> CreateMappings() {
        var zones = JsonSerializer.Deserialize<List<MetaZoneMapping>>(TimeZoneResources.MetaZones,
            TimeZoneResources.SerializerOptions)!;
        var metaZoneMappings = new Dictionary<string, MetaZoneMapping>();
        foreach (var zone in zones) {
            metaZoneMappings.Add(zone.TimeZoneId.ToLower(), zone);
        }
        return metaZoneMappings;
    }

    public static string GetMetaZone(string timeZoneId) {
        ArgumentException.ThrowIfNullOrEmpty(timeZoneId);
        if (!_mappings.Value.TryGetValue(timeZoneId.ToLower(), out var zone)) return string.Empty;
        var time = DateTimeOffset.UtcNow;
        foreach (var applyRule in zone.ApplyRules) {
            if ((applyRule.FromUtc == null || time >= applyRule.FromUtc) &&
                (applyRule.ToUtc == null || time < applyRule.ToUtc)) {
                return applyRule.MetaZoneId;
            }
        }
        return string.Empty;
    }
}
