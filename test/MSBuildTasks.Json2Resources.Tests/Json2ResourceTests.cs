using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Resources;
using System.Text;

namespace AzureZeng.MSBuildTasks.Json2Resources.Tests;

[TestClass]
public class Json2ResourceTests {
    [TestMethod]
    public void LargeIntegersPreservedAsLong() {
        var res = ConvertAndRead("""{"BigId":9223372036854775807,"Over53":9007199254740993,"Count":1}""");
        Assert.AreEqual(9223372036854775807L, res["BigId"]);
        Assert.AreEqual(9007199254740993L, res["Over53"]);
        Assert.AreEqual(1L, res["Count"]);
    }

    [TestMethod]
    public void FractionalNumbersUseDecimal() {
        var res = ConvertAndRead("""{"Price":1.5,"Small":0.1}""");
        Assert.AreEqual(1.5m, res["Price"]);
        Assert.AreEqual(0.1m, res["Small"]);
        Assert.IsInstanceOfType(res["Price"], typeof(decimal));
    }

    [TestMethod]
    public void HugeNumberFallsBackToDouble() {
        var res = ConvertAndRead("""{"Huge":1e100}""");
        Assert.IsInstanceOfType(res["Huge"], typeof(double));
        Assert.AreEqual(1e100, res["Huge"]);
    }

    [TestMethod]
    public void BasicTypeMapping() {
        var res = ConvertAndRead("""{"Name":"hello","Flag":true,"Off":false,"Nil":null}""");
        Assert.AreEqual("hello", res["Name"]);
        Assert.IsTrue((bool)res["Flag"]);
        Assert.IsFalse((bool)res["Off"]);
        Assert.IsTrue(res.ContainsKey("Nil"));
        Assert.IsNull(res["Nil"]);
    }

    [TestMethod]
    public void NestedNamespaceFlattening() {
        var res = ConvertAndRead("""{"Sub":{"Key1":"1","SubKey2":{"Key2":"2"}}}""");
        Assert.AreEqual("1", res["Sub.Key1"]);
        Assert.AreEqual("2", res["Sub.SubKey2.Key2"]);
    }

    [TestMethod]
    public void DuplicateKeyDetection() {
        var ex = Assert.ThrowsExactly<ArgumentException>(() => ConvertAndRead("""{"a.b":1,"a":{"b":2}}"""));
        StringAssert.Contains(ex.Message, "Duplicate resource name: 'a.b'");
    }

    [TestMethod]
    public void DuplicateKeyDetectionIsCaseInsensitive() {
        Assert.ThrowsExactly<ArgumentException>(() => ConvertAndRead("""{"Key":1,"key":2}"""));
    }

    [TestMethod]
    public void RootMustBeObject() {
        Assert.ThrowsExactly<ArgumentException>(() => ConvertAndRead("[1,2,3]"));
        Assert.ThrowsExactly<ArgumentException>(() => ConvertAndRead("\"hello\""));
    }

    [TestMethod]
    public void ArrayValueThrowsClearError() {
        var ex = Assert.ThrowsExactly<ArgumentException>(() => ConvertAndRead("""{"arr":[1,2]}"""));
        StringAssert.Contains(ex.Message, "array");
    }

    [TestMethod]
    public void EmptyObjectProducesEmptyResource() {
        var res = ConvertAndRead("{}");
        Assert.AreEqual(0, res.Count);
    }

    [TestMethod]
    public void ExternalFileTextMode() {
        var dir = CreateTempDir();
        try {
            var jsonPath = Path.Combine(dir, "res.json");
            var outPath = Path.Combine(dir, "res.resources");
            File.WriteAllText(Path.Combine(dir, "text_res.txt"), "hello world");
            File.WriteAllText(jsonPath, """{"$file:SimpleExternalRes1":"text_res.txt"}""");
            Json2Resource.Convert(jsonPath, outPath, "$file:", dir);
            var res = ReadResourceFile(outPath);
            Assert.AreEqual("hello world", res["SimpleExternalRes1"]);
        } finally {
            Directory.Delete(dir, true);
        }
    }

    [TestMethod]
    public void ExternalFileBinaryMode() {
        var dir = CreateTempDir();
        try {
            var jsonPath = Path.Combine(dir, "res.json");
            var outPath = Path.Combine(dir, "res.resources");
            byte[] data = { 1, 2, 3, 4, 5, 255, 0 };
            File.WriteAllBytes(Path.Combine(dir, "bin_data.bin"), data);
            File.WriteAllText(jsonPath, """{"$file:Bin":"bin_data.bin"}""");
            Json2Resource.Convert(jsonPath, outPath, "$file:", dir);
            var res = ReadResourceFile(outPath);
            CollectionAssert.AreEqual(data, (byte[])res["Bin"]);
        } finally {
            Directory.Delete(dir, true);
        }
    }

    [TestMethod]
    public void ExternalFileAdvancedObjectMode() {
        var dir = CreateTempDir();
        try {
            var jsonPath = Path.Combine(dir, "res.json");
            var outPath = Path.Combine(dir, "res.resources");
            File.WriteAllText(Path.Combine(dir, "adv.txt"), "adv text");
            File.WriteAllText(jsonPath, """{"$file:Adv":{"file":"adv.txt","mode":"text"}}""");
            Json2Resource.Convert(jsonPath, outPath, "$file:", dir);
            var res = ReadResourceFile(outPath);
            Assert.AreEqual("adv text", res["Adv"]);
        } finally {
            Directory.Delete(dir, true);
        }
    }

    [TestMethod]
    public void ExternalFileStreamMode() {
        var dir = CreateTempDir();
        try {
            var jsonPath = Path.Combine(dir, "res.json");
            var outPath = Path.Combine(dir, "res.resources");
            byte[] data = { 10, 20, 30, 40 };
            File.WriteAllBytes(Path.Combine(dir, "stream.bin"), data);
            File.WriteAllText(jsonPath, """{"$file:Stm":{"file":"stream.bin","mode":"stream"}}""");
            Json2Resource.Convert(jsonPath, outPath, "$file:", dir);
            var res = ReadResourceFile(outPath);
            var stream = (Stream)res["Stm"];
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            CollectionAssert.AreEqual(data, ms.ToArray());
        } finally {
            Directory.Delete(dir, true);
        }
    }

    [TestMethod]
    public void FilePrefixOverrideAndContextPath() {
        var dir = CreateTempDir();
        try {
            File.WriteAllText(Path.Combine(dir, "p.txt"), "prefixed");
            using var ms = new MemoryStream();
            Json2Resource.Convert(Encoding.UTF8.GetBytes("""{"@@file:P":"p.txt"}"""), ms, "@@file:", dir);
            var res = ReadResource(ms);
            Assert.AreEqual("prefixed", res["P"]);
        } finally {
            Directory.Delete(dir, true);
        }
    }

    private static Dictionary<object, object> ConvertAndRead(string json, string fileItemPrefix = "$file:",
        string contextPath = "") {
        using var ms = new MemoryStream();
        Json2Resource.Convert(Encoding.UTF8.GetBytes(json), ms, fileItemPrefix, contextPath);
        return ReadResource(ms);
    }

    private static Dictionary<object, object> ReadResource(Stream stream) {
        stream.Position = 0;
        var result = new Dictionary<object, object>();
        using (var reader = new ResourceReader(stream)) {
            foreach (DictionaryEntry entry in reader) {
                result[entry.Key] = entry.Value;
            }
        }
        return result;
    }

    private static Dictionary<object, object> ReadResourceFile(string path) {
        using var fs = File.OpenRead(path);
        return ReadResource(fs);
    }

    private static string CreateTempDir() {
        var dir = Path.Combine(Path.GetTempPath(), "j2r_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }
}
