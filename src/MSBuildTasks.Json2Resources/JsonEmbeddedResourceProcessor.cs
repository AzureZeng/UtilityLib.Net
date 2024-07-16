// Copyright (c) Azure Zeng. All rights reserved.
// Licensed under the Apache License 2.0. See LICENSE in the project root for license information.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;

namespace AzureZeng.MSBuildTasks;

public class JsonEmbeddedResourceProcessor : Task {
    [Required] public ITaskItem[] Inputs { get; set; }

    [Required] public string ContextPath { get; set; }

    public override bool Execute() {
        try {
            foreach (var item in Inputs) {
                var filePrefix = item.GetMetadata("FilePrefix");
                if (string.IsNullOrEmpty(filePrefix)) filePrefix = "$file:";
                Json2Resource.Convert(item.GetMetadata("FullPath"), item.GetMetadata("IntermediatePath"),
                    filePrefix, ContextPath);
            }
        } catch (Exception e) {
            Log.LogError($"Error while generating resources: {e}");
            return false;
        }

        return true;
    }
}
