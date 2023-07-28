// Copyright (c) Azure Zeng. All rights reserved.
// Licensed under the Apache License 2.0. See LICENSE in the project root for license information.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace AzureZeng.MSBuildTasks {
    public class AssignPoFileItemMetadata : Task {
        [Required] public ITaskItem[] Files { get; set; }

        [Required] public string IntermediatePath { get; set; }

        [Output] public ITaskItem[] AssignedFiles { get; set; }

        private SHA256 _sha256;

        public override bool Execute() {
            using (_sha256 = SHA256.Create()) {
                try {
                    return ProcessTaskItems();
                } catch (Exception e) {
                    Log.LogErrorFromException(e);
                    return false;
                }
            }
        }

        private bool ProcessTaskItems() {
            AssignedFiles = new ITaskItem[Files.Length];
            for (var i = 0; i < Files.Length; i++) {
                var currentItem = Files[i];
                var outputItem = AssignedFiles[i] = new TaskItem(currentItem.ItemSpec);
                currentItem.CopyMetadataTo(outputItem);
                if (!string.Equals(currentItem.GetMetadata("_HasSetTargetPath"), "true",
                        StringComparison.OrdinalIgnoreCase)) {
                    outputItem.SetMetadata("TargetPath",
                        Path.ChangeExtension(outputItem.GetMetadata("TargetPath"), ".mo"));
                }
                outputItem.SetMetadata("IntermediatePath", Path.Combine(IntermediatePath,
                    $"{Path.GetFileNameWithoutExtension(currentItem.ItemSpec)}_{StrHash(currentItem.GetMetadata("FullPath"))}.mo"));
            }
            return true;
        }

        private string StrHash(string s) {
            return string.Join("",
                _sha256.ComputeHash(new UTF8Encoding().GetBytes(s)).Select(b => b.ToString("x")).ToArray());
        }
    }
}
