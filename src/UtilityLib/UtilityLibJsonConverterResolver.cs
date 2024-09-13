// Copyright (c) Azure Zeng. All rights reserved.
// Licensed under the Apache License 2.0. See LICENSE in the project root for license information.

using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace AzureZeng.UtilityLib;

public class UtilityLibJsonConverterResolver : IJsonTypeInfoResolver {
    public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options) {
        if (type == typeof(DataTable)) {
            var conv = options.Converters.FirstOrDefault(x => x.Type == typeof(DataTable));
            return JsonMetadataServices.CreateValueInfo<DataTable>(options, conv ?? new JsonDataTableRenderer());
        }
        return null;
    }
}
