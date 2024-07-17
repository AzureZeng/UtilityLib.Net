using System.Text.Json;
using System.Text.Json.Serialization;

namespace AzureZeng.UtilityLib;

/// <summary>
/// Utility for converting <see cref="long"/> to <see cref="string"/> and vice versa in JSON serialization.
/// </summary>
public class LongToStringJsonConverter : JsonConverter<long> {
    /// <inheritdoc/>
    public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        if (reader.TokenType == JsonTokenType.Number) return reader.GetInt64();
        if (reader.TokenType == JsonTokenType.String) return long.Parse(reader.GetString()!);
        throw new JsonException(StringResources.GetString("Exceptions.InvalidStrToLongValueType"));
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options) {
        writer.WriteStringValue(value.ToString());
    }
}
