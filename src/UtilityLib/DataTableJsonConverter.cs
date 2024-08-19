// Copyright (c) Azure Zeng. All rights reserved.
// Licensed under the Apache License 2.0. See LICENSE in the project root for license information.

using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AzureZeng.UtilityLib;

public class DataTableJsonConverter : JsonConverter<DataTable> {
    public override DataTable? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, DataTable value, JsonSerializerOptions options) {
        throw new NotImplementedException();
    }
}
