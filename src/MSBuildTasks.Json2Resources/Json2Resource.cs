using System;
using System.Collections.Generic;
using System.IO;
using System.Resources;
using System.Text.Json;

namespace AzureZeng.MSBuildTasks.Json2Resources;

/// <summary>
/// Converts a JSON resource manifest into a standard .NET binary resource (.resources) file.
/// Nested JSON objects are flattened into dotted resource keys, and properties whose names start with a
/// configurable prefix (default "$file:") load their value from an external file instead.
/// </summary>
public static class Json2Resource {
    // File extensions treated as text when the external-file load mode is "auto".
    // Any other file is stored as a byte array.
    private static readonly ISet<string> TextFileExtensions = new HashSet<string> {
        ".txt",
        ".log",
        ".ini",
        ".xml",
        ".json",
        ".jsonc",
        ".cs",
        ".vb",
        ".css",
        ".htm",
        ".html",
        ".js",
        ".ts",
        ".c",
        ".cpp",
        ".h",
        ".go",
        ".rs",
        ".py",
        ".rtf",
        ".odt",
        ".sql",
        ".sh"
    };

    /// <summary>
    /// Reads a JSON resource manifest from a file and writes the converted .resources file.
    /// </summary>
    /// <param name="resFileName">Path of the JSON resource manifest to read.</param>
    /// <param name="outputFileName">Path of the .resources file to write.</param>
    /// <param name="fileItemPrefix">Prefix that marks a key as loading from an external file.</param>
    /// <param name="contextPath">Base directory for resolving relative external file paths.</param>
    public static void Convert(string resFileName, string outputFileName, string fileItemPrefix = "$file:",
        string contextPath = "") {
        using var resw = new ResourceWriter(outputFileName);
        // Allow trailing commas and skip comments so the manifest is convenient to author by hand.
        var reader = new Utf8JsonReader(File.ReadAllBytes(resFileName),
            new JsonReaderOptions() { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
        var element = JsonElement.ParseValue(ref reader);
        EnsureRootObject(element, resFileName);
        // Tracks every resource name written so far to reject duplicates with a clear error.
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        // Streams of "stream"-mode external resources, kept open until Generate() reads them.
        var streamList = new List<Stream>();
        try {
            Json2ResourceInternal(resw, ref element, string.Empty, fileItemPrefix, contextPath, names, streamList);
            resw.Generate();
        } finally {
            // Always close opened streams, even when an exception is thrown mid-conversion.
            streamList.ForEach(ForceDispose);
        }
    }

    /// <summary>
    /// Converts an in-memory JSON resource manifest and writes the .resources data to <paramref name="target"/>.
    /// </summary>
    /// <remarks>
    /// The <paramref name="target"/> stream is left open. Because <see cref="ResourceWriter"/> closes the stream
    /// it is constructed with, the .resources data is first written to an internal buffer and then copied out.
    /// </remarks>
    /// <param name="input">UTF-8 encoded JSON resource manifest.</param>
    /// <param name="target">Stream the .resources data is written to.</param>
    /// <param name="fileItemPrefix">Prefix that marks a key as loading from an external file.</param>
    /// <param name="contextPath">Base directory for resolving relative external file paths.</param>
    public static void Convert(ReadOnlySpan<byte> input, Stream target, string fileItemPrefix = "$file:",
        string contextPath = "") {
        using var buffer = new MemoryStream();
        var reader = new Utf8JsonReader(input);
        var element = JsonElement.ParseValue(ref reader);
        EnsureRootObject(element);
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var streamList = new List<Stream>();
        try {
            using (var resw = new ResourceWriter(buffer)) {
                Json2ResourceInternal(resw, ref element, string.Empty, fileItemPrefix, contextPath, names,
                    streamList);
                resw.Generate();
            }
        } finally {
            streamList.ForEach(ForceDispose);
        }
        var bytes = buffer.ToArray();
        target.Write(bytes, 0, bytes.Length);
        target.Flush();
    }

    /// <summary>
    /// Validates that the JSON root is an object; a manifest with an array or primitive root is rejected.
    /// </summary>
    private static void EnsureRootObject(JsonElement element, string source = "") {
        if (element.ValueKind != JsonValueKind.Object)
            throw new ArgumentException(
                $"The root element of the JSON resource{(string.IsNullOrEmpty(source) ? "" : $" '{source}'")} " +
                $"must be a JSON object, but the actual type is '{element.ValueKind}'.");
    }

    /// <summary>
    /// Adds a resource after checking that the name has not been used before.
    /// </summary>
    /// <remarks>
    /// Names are compared case-insensitively to match <see cref="ResourceWriter"/> / <see cref="ResourceManager"/>
    /// lookup semantics, so keys such as "Foo" and "foo" are treated as duplicates.
    /// </remarks>
    private static void AddResource(ResourceWriter resw, ISet<string> names, string name, object value) {
        if (!names.Add(name))
            throw new ArgumentException($"Duplicate resource name: '{name}'.");
        resw.AddResource(name, value);
    }

    /// <summary>
    /// Recursively converts the JSON object <paramref name="element"/> into resources, prefixing nested keys with
    /// <paramref name="ns"/> (e.g. a nested "b" inside "a" becomes the resource key "a.b").
    /// </summary>
    private static void Json2ResourceInternal(ResourceWriter resw, ref JsonElement element, string ns,
        string fileItemPrefix, string contextPath, ISet<string> names, List<Stream> streamList) {
        string finalNs = string.IsNullOrEmpty(ns) ? string.Empty : $"{ns}.";

        foreach (var p in element.EnumerateObject()) {
            string name = $"{finalNs}{p.Name}";
            // Read item data from file
            if (!string.IsNullOrEmpty(fileItemPrefix) && p.Name.StartsWith(fileItemPrefix)) {
                var resName = p.Name.Substring(fileItemPrefix.Length);
                if (string.IsNullOrEmpty(resName))
                    throw new ArgumentException($"Invalid external file key: {p.Name}");
                name = $"{finalNs}{resName}";
                var mode = string.Empty;
                var fileName = string.Empty;
                // Resolve external resource info
                switch (p.Value.ValueKind) {
                    case JsonValueKind.String:
                        fileName = p.Value.GetString();
                        break;
                    case JsonValueKind.Object: {
                        if (p.Value.TryGetProperty("file", out var fileProp) &&
                            fileProp.ValueKind == JsonValueKind.String) {
                            fileName = fileProp.GetString();
                        }
                        if (p.Value.TryGetProperty("mode", out var modeProp) &&
                            modeProp.ValueKind == JsonValueKind.String) {
                            mode = modeProp.GetString()?.ToLower();
                        }
                        break;
                    }
                    default:
                        throw new ArgumentException(
                            $"Unsupported value for external file resource: {p.Value.ValueKind}");
                }
                // Validate and write resource
                if (string.IsNullOrEmpty(fileName))
                    throw new ArgumentException($"No file name specified for property '{name}'");
                if (!Path.IsPathRooted(fileName)) fileName = Path.Combine(contextPath, fileName);
                if (string.IsNullOrEmpty(mode) || mode == "auto") {
                    mode = TextFileExtensions.Contains(Path.GetExtension(fileName).ToLower()) ? "text" : "binary";
                }
                if (mode == "txt" || mode == "text") {
                    AddResource(resw, names, name, File.ReadAllText(fileName));
                } else if (mode == "binary") {
                    AddResource(resw, names, name, File.ReadAllBytes(fileName));
                } else if (mode == "stream") {
                    var fs = new FileStream(fileName, FileMode.Open);
                    streamList.Add(fs);
                    AddResource(resw, names, name, fs);
                } else {
                    throw new ArgumentException(
                        $"Unsupported file mode type '{mode}', only accepts 'text', 'binary' or 'stream'.");
                }
                continue;
            }

            // Plain value resource
            switch (p.Value.ValueKind) {
                case JsonValueKind.String:
                    AddResource(resw, names, name, p.Value.GetString());
                    break;
                case JsonValueKind.Number:
                    // Store with the most precise .NET type available to avoid precision loss:
                    // integral values -> long (exact), otherwise decimal (exact), then double as a fallback
                    // for values beyond the decimal range (e.g. 1e100).
                    var num = p.Value;
                    if (num.TryGetInt64(out var l))
                        AddResource(resw, names, name, l);
                    else if (num.TryGetDecimal(out var d))
                        AddResource(resw, names, name, d);
                    else
                        AddResource(resw, names, name, num.GetDouble());
                    break;
                case JsonValueKind.False:
                    AddResource(resw, names, name, false);
                    break;
                case JsonValueKind.True:
                    AddResource(resw, names, name, true);
                    break;
                case JsonValueKind.Null:
                    AddResource(resw, names, name, (object)null);
                    break;
                case JsonValueKind.Object:
                    // Recurse into the nested object, extending the key namespace.
                    var sub = p.Value;
                    Json2ResourceInternal(resw, ref sub, $"{finalNs}{p.Name}", fileItemPrefix, contextPath, names,
                        streamList);
                    break;
                case JsonValueKind.Array:
                    throw new ArgumentException(
                        $"JSON array value is not supported for resource key '{name}'. " +
                        "Only object, string, number, boolean and null values are supported.");
                default:
                    throw new InvalidOperationException($"Unsupported token type: {p.Value.ValueKind}");
            }
        }
    }

    private static void ForceDispose(IDisposable disposable) {
        try {
            disposable?.Dispose();
        } catch (Exception) {
            // ignored
        }
    }
}
