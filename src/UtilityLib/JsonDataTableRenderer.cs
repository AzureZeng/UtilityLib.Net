// Copyright (c) Azure Zeng. All rights reserved.
// Licensed under the Apache License 2.0. See LICENSE in the project root for license information.

using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AzureZeng.UtilityLib;

public class JsonDataTableRenderer : JsonConverter<DataTable> {
    public override DataTable? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        throw new JsonException("Only the serialization operation of DataTable is supported.");
    }

    public override void Write(Utf8JsonWriter writer, DataTable value, JsonSerializerOptions options) {
        writer.WriteStartArray();
        foreach (DataRow r in value.Rows) {
            writer.WriteStartObject();
            for (int i = 0; i < value.Columns.Count; i++) {
                var c = value.Columns[i];
                writer.WritePropertyName(c.ColumnName);
                var val = r[i];
                if (val is null or DBNull) {
                    writer.WriteNullValue();
                } else
                    JsonSerializer.Serialize(writer, val, val.GetType(), options);
            }
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }
}
