// Copyright (c) Azure Zeng. All rights reserved.
// Licensed under the Apache License 2.0. See LICENSE in the project root for license information.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace AzureZeng.MSBuildTasks {
    public class CompileIntermediateMoFile : Task {
        [Required] public ITaskItem[] PoSrcFiles { get; set; }

        public override bool Execute() {
            foreach (var f in PoSrcFiles) {
                try {
                    if (!CompileFile(f.GetMetadata("FullPath"), f.GetMetadata("IntermediatePath"))) return false;
                } catch (Exception e) {
                    Log.LogErrorFromException(e);
                    return false;
                }
            }
            return true;
        }

        private bool CompileFile(string source, string target) {
            string startArgs = string.Format($"\"{source}\" -o \"{target}\"");
            var process = Process.Start(new ProcessStartInfo("msgfmt", startArgs) { RedirectStandardError = true });
            if (process == null) {
                Log.LogError("Cannot start msgfmt");
                return true;
            }
            var err = process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0) {
                Log.LogError($"Error when compiling MO file: {err}");
                return false;
            }
            return true;
        }
    }
}
