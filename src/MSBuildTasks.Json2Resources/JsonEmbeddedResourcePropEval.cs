using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.IO;
using System.Text;

// ReSharper disable InconsistentNaming

namespace AzureZeng.MSBuildTasks;

public class JsonEmbeddedResourcePropEval : Task {
    [Required] public ITaskItem[] Inputs { get; set; }

    [Required] public string RootNamespace { get; set; }

    [Required] public string IntermediateOutputPath { get; set; }

    [Output] public ITaskItem[] Outputs { get; set; }

    public override bool Execute() {
        if (Inputs == null || Inputs.Length == 0) return true;
        try {
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
                var logicalName = input.GetMetadata("LogicalName");
                if (string.IsNullOrEmpty(logicalName)) logicalName = defaultLogicalName;

                // set metadata for output item
                var output = new TaskItem(input);
                output.SetMetadata("LogicalName", logicalName);
                output.SetMetadata("IntermediatePath",
                    Path.Combine(IntermediateOutputPath, logicalName));
                Outputs[i] = output;
            }
        } catch (Exception e) {
            Log.LogError(e.ToString());
            return false;
        }

        return true;
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
        sb.Append(".resources");
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
