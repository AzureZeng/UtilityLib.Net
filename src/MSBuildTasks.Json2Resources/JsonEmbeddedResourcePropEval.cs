using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

// ReSharper disable InconsistentNaming

namespace AzureZeng.MSBuildTasks.Json2Resources;

/// <summary>
/// Evaluates the MSBuild metadata required to compile a set of JSON resource manifests:
/// the final <c>LogicalName</c> and <c>IntermediatePath</c> of every generated .resources file,
/// plus the flattened list of external files referenced through the file prefix keys.
/// </summary>
public class JsonEmbeddedResourcePropEval : Task {
    /// <summary>The <c>JsonResource</c> items to evaluate.</summary>
    [Required] public ITaskItem[] Inputs { get; set; }

    /// <summary>The project root namespace, prepended to every generated logical name.</summary>
    [Required] public string RootNamespace { get; set; }

    /// <summary>Directory where the generated .resources files are placed.</summary>
    [Required] public string IntermediateOutputPath { get; set; }

    /// <summary>Base directory for resolving relative external file paths.</summary>
    [Required] public string ContextPath { get; set; }

    /// <summary>The input items enriched with <c>LogicalName</c> and <c>IntermediatePath</c> metadata.</summary>
    [Output] public ITaskItem[] Outputs { get; set; }

    /// <summary>
    /// The deduplicated, normalized absolute paths of every external file referenced by the input manifests.
    /// Each path is a separate item so MSBuild can treat them as individual incremental-build inputs.
    /// </summary>
    [Output] public ITaskItem[] ExternalFiles { get; set; }

    public override bool Execute() {
        if (Inputs == null || Inputs.Length == 0) return true;
        try {
            // A SortedSet keeps the list deduplicated (a file referenced by several manifests counts once)
            // and deterministic (case-insensitive sort).
            var externalFileSet = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            Outputs = new ITaskItem[Inputs.Length];
            for (int i = 0; i < Inputs.Length; i++) {
                var input = Inputs[i];
                // Get item path relative to project
                var link = input.GetMetadata("Link");
                var itemRelativePath = string.IsNullOrEmpty(link) ? input.ItemSpec : link;

                // now translate it to logical name
                var ns = Dir2NS(Path.GetDirectoryName(itemRelativePath));
                var name = Path.GetFileNameWithoutExtension(itemRelativePath);
                var defaultLogicalName = DetermineLogicalName(ns, name);

                // determine LogicalName
                var baseLogicalName = input.GetMetadata("LogicalName");
                if (string.IsNullOrEmpty(baseLogicalName)) baseLogicalName = defaultLogicalName;
                var finalLogicalName = $"{baseLogicalName}.resources";

                // set metadata for output item
                var output = new TaskItem(input);
                output.SetMetadata("LogicalName", finalLogicalName);
                // The intermediate path uses the (possibly overridden) base name so two manifests whose
                // default names collide but override LogicalName differently never write to the same file.
                output.SetMetadata("IntermediatePath",
                    Path.Combine(IntermediateOutputPath, $"{baseLogicalName}.resources"));
                Outputs[i] = output;

                // Collect external file dependencies for incremental builds
                var filePrefix = input.GetMetadata("FilePrefix");
                if (string.IsNullOrEmpty(filePrefix)) filePrefix = "$file:";
                // Mirror the processor's context path resolution: per-item "FileDir" overrides ContextPath.
                var finalContextPath = ContextPath;
                if (string.Equals(input.GetMetadata("ContextPath"), "FileDir", StringComparison.OrdinalIgnoreCase))
                    finalContextPath = Path.GetDirectoryName(input.GetMetadata("FullPath"));
                CollectExternalFiles(input.GetMetadata("FullPath"), filePrefix, finalContextPath, externalFileSet);
            }

            // Flatten into one item per file. This must NOT be a single semicolon-joined item: MSBuild
            // item transforms do not split ';', so joined paths would be treated as one non-existent input.
            var externalFiles = new ITaskItem[externalFileSet.Count];
            int idx = 0;
            foreach (var f in externalFileSet) externalFiles[idx++] = new TaskItem(f);
            ExternalFiles = externalFiles;
        } catch (Exception e) {
            Log.LogError(e.ToString());
            return false;
        }

        return true;
    }

    /// <summary>
    /// Parses the manifest at <paramref name="jsonFilePath"/> and adds every external file it references to
    /// <paramref name="result"/>. Missing or malformed manifests are skipped here; the processor reports them.
    /// </summary>
    private static void CollectExternalFiles(string jsonFilePath, string fileItemPrefix, string contextPath,
        ISet<string> result) {
        if (string.IsNullOrEmpty(jsonFilePath) || !File.Exists(jsonFilePath)) return;
        try {
            var reader = new Utf8JsonReader(File.ReadAllBytes(jsonFilePath),
                new JsonReaderOptions() { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
            var element = JsonElement.ParseValue(ref reader);
            CollectExternalFiles(element, fileItemPrefix, contextPath, result);
        } catch (JsonException) {
            // The processor reports malformed JSON; skip dependency scan here.
        }
    }

    /// <summary>
    /// Walks the manifest recursively; external-file keys (whose name starts with
    /// <paramref name="fileItemPrefix"/>) contribute their resolved file path.
    /// </summary>
    private static void CollectExternalFiles(JsonElement element, string fileItemPrefix, string contextPath,
        ISet<string> result) {
        if (element.ValueKind != JsonValueKind.Object) return;
        foreach (var p in element.EnumerateObject()) {
            if (!string.IsNullOrEmpty(fileItemPrefix) && p.Name.StartsWith(fileItemPrefix)) {
                var fileName = string.Empty;
                if (p.Value.ValueKind == JsonValueKind.String) {
                    fileName = p.Value.GetString();
                } else if (p.Value.ValueKind == JsonValueKind.Object &&
                           p.Value.TryGetProperty("file", out var fileProp) &&
                           fileProp.ValueKind == JsonValueKind.String) {
                    fileName = fileProp.GetString();
                }
                if (!string.IsNullOrEmpty(fileName)) {
                    if (!Path.IsPathRooted(fileName)) fileName = Path.Combine(contextPath, fileName);
                    // Normalize (resolves ".." segments) so the same file referenced differently deduplicates.
                    result.Add(Path.GetFullPath(fileName));
                }
                continue;
            }
            if (p.Value.ValueKind == JsonValueKind.Object) {
                CollectExternalFiles(p.Value, fileItemPrefix, contextPath, result);
            }
        }
    }

    private string DetermineLogicalName(string ns, string name) {
        StringBuilder sb = new StringBuilder();
        if (!string.IsNullOrEmpty(RootNamespace)) {
            sb.Append(RootNamespace);
            sb.Append('.');
        }
        if (!string.IsNullOrEmpty(ns)) {
            sb.Append(ns);
            sb.Append('.');
        }
        sb.Append(name);
        return sb.ToString();
    }

    private static string Dir2NS(string path) {
        path = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        var sp = path.Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
        var pos = 0;
        for (int i = 0; i < sp.Length; i++) {
            var c = sp[i];
            if (c == ".") continue;
            if (c == "..") {
                pos--;
                if (pos < 0) throw new ArgumentException("Invalid path");
                continue;
            }
            sp[pos] = c;
            pos++;
        }
        var sb = new StringBuilder();
        for (int i = 0; i < pos; i++) {
            if (sb.Length > 0) sb.Append('.');
            sb.Append(sp[i]);
        }
        return sb.ToString();
    }
}
