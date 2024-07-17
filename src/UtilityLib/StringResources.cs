using System.Resources;

namespace AzureZeng.UtilityLib;

public static partial class StringResources {
    public static ResourceManager ResourceManager { get; } = new ResourceManager(typeof(StringResources));

    public static string? GetString(string key) {
        return ResourceManager.GetString(key);
    }
    public static string? GetString(string key, params object[] args) {
        var s = ResourceManager.GetString(key);
        if (s == null) return null;
        return string.Format(s, args);
    }
}
