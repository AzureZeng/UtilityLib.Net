// Copyright (c) Azure Zeng. All rights reserved.
// Licensed under the Apache License 2.0. See LICENSE in the project root for license information.

using System.Resources;
using System.Text.Json;

namespace AzureZeng.UtilityLib.Resources;

internal static class TimeZoneResources {
    public static ResourceManager ResourceManager { get; } = new ResourceManager(typeof(TimeZoneResources));
    
    public static JsonSerializerOptions? SerializerOptions { get; private set; }

    static TimeZoneResources() {
        SerializerOptions= new JsonSerializerOptions();
        SerializerOptions.MakeReadOnly();
    }
    
    public static string MetaZones => ResourceManager.GetString("MetaZones")!;
}
