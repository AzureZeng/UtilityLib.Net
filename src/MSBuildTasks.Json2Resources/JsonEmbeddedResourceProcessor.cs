// Copyright (c) Azure Zeng. All rights reserved.
// Licensed under the Apache License 2.0. See LICENSE in the project root for license information.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.IO;

namespace AzureZeng.MSBuildTasks;

public class JsonEmbeddedResourceProcessor : Task {
    [Required] public ITaskItem[] Inputs { get; set; }

    [Required] public string ContextPath { get; set; }

    public override bool Execute() {
        try {
            foreach (var item in Inputs) {
                var filePrefix = item.GetMetadata("FilePrefix");
                if (string.IsNullOrEmpty(filePrefix)) filePrefix = "$file:";

                // Set compile context path to the directory of JsonResource file
                // when the value of ContextPath property is 'FileDir' 
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
