// Copyright (c) Azure Zeng. All rights reserved.
// Licensed under the Apache License 2.0. See LICENSE in the project root for license information.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.IO;

namespace AzureZeng.MSBuildTasks.Json2Resources;

/// <summary>
/// Compiles every evaluated <c>JsonResource</c> item into its .resources file
/// by delegating to <see cref="Json2Resource.Convert"/>.
/// </summary>
public class JsonEmbeddedResourceProcessor : Task {
    /// <summary>The <c>JsonResource</c> items (enriched with <c>IntermediatePath</c> metadata) to compile.</summary>
    [Required] public ITaskItem[] Inputs { get; set; }

    /// <summary>Base directory for resolving relative external file paths.</summary>
    [Required] public string ContextPath { get; set; }

    public override bool Execute() {
        try {
            foreach (var item in Inputs) {
                var filePrefix = item.GetMetadata("FilePrefix");
                if (string.IsNullOrEmpty(filePrefix)) filePrefix = "$file:";

                // Set compile context path to the directory of the JsonResource file
                // when the value of ContextPath property is 'FileDir'.
                var finalContextPath = ContextPath;
                if (string.Equals(item.GetMetadata("ContextPath"), "FileDir", StringComparison.OrdinalIgnoreCase))
                    finalContextPath = Path.GetDirectoryName(item.GetMetadata("FullPath"));
                Json2Resource.Convert(item.GetMetadata("FullPath"), item.GetMetadata("IntermediatePath"),
                    filePrefix, finalContextPath);
            }
        } catch (Exception e) {
            Log.LogError($"Error while generating resources: {e}");
            return false;
        }

        return true;
    }
}
