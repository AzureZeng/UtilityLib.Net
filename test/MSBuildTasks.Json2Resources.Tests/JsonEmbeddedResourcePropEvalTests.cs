using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace AzureZeng.MSBuildTasks.Json2Resources.Tests;

[TestClass]
public class JsonEmbeddedResourcePropEvalTests {
    [TestMethod]
    public void IntermediatePathUsesDefaultLogicalName() {
        var task = new JsonEmbeddedResourcePropEval {
            Inputs = new ITaskItem[] { new TaskItem(Path.Combine("foo", "Test.json")) },
            RootNamespace = "My.Root",
            IntermediateOutputPath = "obj",
            ContextPath = ".",
        };
        Assert.IsTrue(task.Execute());
        Assert.AreEqual(Path.Combine("obj", "My.Root.foo.Test.resources"),
            task.Outputs[0].GetMetadata("IntermediatePath"));
    }

    [TestMethod]
    public void IntermediatePathUsesOverriddenLogicalName() {
        var item = new TaskItem(Path.Combine("foo", "Test.json"));
        item.SetMetadata("LogicalName", "Custom");
        var task = new JsonEmbeddedResourcePropEval {
            Inputs = new ITaskItem[] { item },
            RootNamespace = "My.Root",
            IntermediateOutputPath = "obj",
            ContextPath = ".",
        };
        Assert.IsTrue(task.Execute());
        Assert.AreEqual(Path.Combine("obj", "Custom.resources"),
            task.Outputs[0].GetMetadata("IntermediatePath"));
        Assert.AreEqual("Custom.resources", task.Outputs[0].GetMetadata("LogicalName"));
    }

    [TestMethod]
    public void ExternalFilesCollectedAndDeduplicated() {
        var dir = CreateTempDir();
        try {
            var jsonPath = Path.Combine(dir, "res.json");
            Directory.CreateDirectory(Path.Combine(dir, "sub"));
            File.WriteAllText(jsonPath, """
                {
                  "$file:T": "e1.txt",
                  "$file:B": { "file": "sub/e2.bin", "mode": "binary" },
                  "Nested": { "$file:N": "e1.txt" }
                }
                """);
            var task = new JsonEmbeddedResourcePropEval {
                Inputs = new ITaskItem[] { new TaskItem(jsonPath) },
                RootNamespace = "R",
                IntermediateOutputPath = "obj",
                ContextPath = dir,
            };
            Assert.IsTrue(task.Execute());
            var external = task.ExternalFiles.Select(f => f.ItemSpec).ToList();
            Assert.AreEqual(2, external.Count);
            CollectionAssert.Contains(external, Path.Combine(dir, "e1.txt"));
            CollectionAssert.Contains(external, Path.Combine(dir, "sub", "e2.bin"));
        } finally {
            Directory.Delete(dir, true);
        }
    }

    [TestMethod]
    public void ExternalFilesRespectsFileDirContextPath() {
        var dir = CreateTempDir();
        try {
            var sub = Path.Combine(dir, "res");
            Directory.CreateDirectory(sub);
            var jsonPath = Path.Combine(sub, "res.json");
            File.WriteAllText(jsonPath, """{"$file:T": "e1.txt"}""");
            var item = new TaskItem(jsonPath);
            item.SetMetadata("ContextPath", "FileDir");
            var task = new JsonEmbeddedResourcePropEval {
                Inputs = new ITaskItem[] { item },
                RootNamespace = "R",
                IntermediateOutputPath = "obj",
                ContextPath = dir,
            };
            Assert.IsTrue(task.Execute());
            Assert.AreEqual(Path.Combine(sub, "e1.txt"), task.ExternalFiles[0].ItemSpec);
        } finally {
            Directory.Delete(dir, true);
        }
    }

    [TestMethod]
    public void MissingJsonFileProducesNoExternalFiles() {
        var task = new JsonEmbeddedResourcePropEval {
            Inputs = new ITaskItem[] { new TaskItem(Path.Combine("missing", "Test.json")) },
            RootNamespace = "R",
            IntermediateOutputPath = "obj",
            ContextPath = ".",
        };
        Assert.IsTrue(task.Execute());
        Assert.AreEqual(0, task.ExternalFiles.Length);
    }

    [TestMethod]
    public void SameFileReferencedMultipleWays_DeduplicatedToSingleItem() {
        var dir = CreateTempDir();
        try {
            var jsonPath = Path.Combine(dir, "res.json");
            var abs = Path.Combine(dir, "e1.txt");
            File.WriteAllText(jsonPath, $$"""
                {
                  "$file:A": "e1.txt",
                  "$file:B": { "file": "e1.txt", "mode": "text" },
                  "Nested": { "$file:C": { "file": "sub/../e1.txt" } },
                  "$file:D": "{{abs}}"
                }
                """);
            var task = new JsonEmbeddedResourcePropEval {
                Inputs = new ITaskItem[] { new TaskItem(jsonPath) },
                RootNamespace = "R",
                IntermediateOutputPath = "obj",
                ContextPath = dir,
            };
            Assert.IsTrue(task.Execute());
            var external = task.ExternalFiles.Select(f => f.ItemSpec).ToArray();
            Assert.AreEqual(1, external.Length);
            Assert.AreEqual(Path.GetFullPath(abs), external[0]);
        } finally {
            Directory.Delete(dir, true);
        }
    }

    [TestMethod]
    public void DistinctFilesProduceDistinctItemsWithoutSemicolonMerging() {
        var dir = CreateTempDir();
        try {
            var jsonPath = Path.Combine(dir, "res.json");
            File.WriteAllText(jsonPath, """
                {
                  "$file:A": "a.txt",
                  "$file:B": { "file": "b.txt", "mode": "text" },
                  "Nested": { "$file:C": "c.txt" },
                  "$file:D": "a.txt"
                }
                """);
            var task = new JsonEmbeddedResourcePropEval {
                Inputs = new ITaskItem[] { new TaskItem(jsonPath) },
                RootNamespace = "R",
                IntermediateOutputPath = "obj",
                ContextPath = dir,
            };
            Assert.IsTrue(task.Execute());
            var external = task.ExternalFiles.Select(f => f.ItemSpec).ToArray();
            Assert.AreEqual(3, external.Length);
            Assert.IsFalse(external.Any(p => p.Contains(';')),
                "External file items must be flattened into separate items, not joined with ';'");
            CollectionAssert.AreEquivalent(
                new[] {
                    Path.Combine(dir, "a.txt"),
                    Path.Combine(dir, "b.txt"),
                    Path.Combine(dir, "c.txt"),
                },
                external);
        } finally {
            Directory.Delete(dir, true);
        }
    }

    [TestMethod]
    public void ExternalFilesFlattenedAcrossMultipleInputFiles() {
        var dir = CreateTempDir();
        try {
            var json1 = Path.Combine(dir, "a.json");
            var json2 = Path.Combine(dir, "b.json");
            File.WriteAllText(json1, """{"$file:One": "shared.txt", "$file:Two": "a_only.txt"}""");
            File.WriteAllText(json2, """{"$file:Three": "shared.txt", "$file:Four": "b_only.txt"}""");
            var task = new JsonEmbeddedResourcePropEval {
                Inputs = new ITaskItem[] { new TaskItem(json1), new TaskItem(json2) },
                RootNamespace = "R",
                IntermediateOutputPath = "obj",
                ContextPath = dir,
            };
            Assert.IsTrue(task.Execute());
            var external = task.ExternalFiles.Select(f => f.ItemSpec).ToArray();
            Assert.AreEqual(3, external.Length);
            CollectionAssert.AreEquivalent(
                new[] {
                    Path.Combine(dir, "shared.txt"),
                    Path.Combine(dir, "a_only.txt"),
                    Path.Combine(dir, "b_only.txt"),
                },
                external);
        } finally {
            Directory.Delete(dir, true);
        }
    }

    private static string CreateTempDir() {
        var dir = Path.Combine(Path.GetTempPath(), "j2r_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }
}
