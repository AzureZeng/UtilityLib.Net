using System;
using System.Collections.Generic;
using System.IO;
using System.Resources;
using System.Text.Json;

namespace AzureZeng.MSBuildTasks;

public static class Json2Resource {
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

    public static void Convert(string resFileName, string outputFileName, string fileItemPrefix = "$file:",
        string contextPath = "") {
        using (var resw = new ResourceWriter(outputFileName)) {
            var reader = new Utf8JsonReader(File.ReadAllBytes(resFileName));
            var element = JsonElement.ParseValue(ref reader);
            Json2ResourceInternal(resw, ref element, string.Empty, fileItemPrefix, contextPath);
        }
    }

    public static void Convert(ReadOnlySpan<byte> input, Stream target, string fileItemPrefix = "$file:",
        string contextPath = "") {
        var reader = new Utf8JsonReader(input);
        var element = JsonElement.ParseValue(ref reader);
        var resw = new ResourceWriter(target);
        Json2ResourceInternal(resw, ref element, string.Empty, fileItemPrefix, contextPath);
    }

    private static void Json2ResourceInternal(ResourceWriter resw, ref JsonElement element, string ns,
        string fileItemPrefix, string contextPath) {
        string finalNs = string.IsNullOrEmpty(ns) ? string.Empty : $"{ns}.";
        var streamList = new List<Stream>();
        try {
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
                        resw.AddResource(name, File.ReadAllText(fileName));
                    } else if (mode == "binary") {
                        resw.AddResource(name, File.ReadAllBytes(fileName));
                    } else if (mode == "stream") {
                        var fs = new FileStream(fileName, FileMode.Open);
                        streamList.Add(fs);
                        resw.AddResource(name, fs);
                    } else {
                        throw new ArgumentException(
                            $"Unsupported file mode type '{mode}', only accepts 'text', 'binary' or 'stream'.");
                    }
                    continue;
                }

                // Plain text resource
                switch (p.Value.ValueKind) {
                    case JsonValueKind.String:
                        resw.AddResource(name, p.Value.GetString());
                        break;
                    case JsonValueKind.Number:
                        resw.AddResource(name, p.Value.GetDouble());
                        break;
                    case JsonValueKind.False:
                        resw.AddResource(name, false);
                        break;
                    case JsonValueKind.True:
                        resw.AddResource(name, true);
                        break;
                    case JsonValueKind.Null:
                        resw.AddResource(name, (object)null);
                        break;
                    case JsonValueKind.Object:
                        var sub = p.Value;
                        Json2ResourceInternal(resw, ref sub, $"{finalNs}{p.Name}", fileItemPrefix, contextPath);
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported token type: {p.Value.ValueKind}");
                }
            }
        } finally {
            streamList.ForEach(ForceDispose);
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
