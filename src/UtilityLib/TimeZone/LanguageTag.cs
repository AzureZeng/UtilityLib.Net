using System.Text;

namespace AzureZeng.UtilityLib.TimeZone;

#nullable disable

public struct LanguageTag : IEquatable<LanguageTag> {
    public LanguageTag() {
    }

    public LanguageTag(string langId) {
        // NEED VERIFY!
        if (string.IsNullOrEmpty(langId)) throw new ArgumentNullException(nameof(langId));
        string[] parts = langId.Replace("_", "-").Split('-');
        Language = parts[0];
        for (int i = 1; i < parts.Length; i++) {
            var str = parts[i];
            if (Territory == null && str.Length == 4) Script = parts[i];
            else if (str.Length >= 2 && str.Length <= 3) Territory = parts[i];
            else if (str.Length > 4) {
                Variant = null;
            }
        }
    }

    public string Language { get; set; } = string.Empty;
    public string Script { get; set; } = null;
    public string Territory { get; set; } = null;
    public string Variant { get; set; } = null;
    
    public bool IsEmpty => string.IsNullOrEmpty(Language);

    public override string ToString() {
        return ToString('-');
    }

    public string ToString(char separator) {
        var stringBuilder = new StringBuilder(32);
        stringBuilder.Append(Language);
        if (!string.IsNullOrEmpty(Script)) {
            stringBuilder.Append(separator);
            stringBuilder.Append(Script);
        }
        if (!string.IsNullOrEmpty(Territory)) {
            stringBuilder.Append(separator);
            stringBuilder.Append(Territory);
        }
        if (!string.IsNullOrEmpty(Variant)) {
            stringBuilder.Append(separator);
            stringBuilder.Append(Variant);
        }
        return stringBuilder.ToString();
    }

    public bool Equals(LanguageTag other) {
        return string.Equals(Language, other.Language, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(Script, other.Script, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(Territory, other.Territory, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(Variant, other.Variant, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object obj) {
        return obj is LanguageTag other && Equals(other);
    }

    public override int GetHashCode() {
        var hashCode = new HashCode();
        hashCode.Add(Language, StringComparer.OrdinalIgnoreCase);
        hashCode.Add(Script, StringComparer.OrdinalIgnoreCase);
        hashCode.Add(Territory, StringComparer.OrdinalIgnoreCase);
        hashCode.Add(Variant, StringComparer.OrdinalIgnoreCase);
        return hashCode.ToHashCode();
    }

    public static bool operator ==(LanguageTag left, LanguageTag right) {
        return left.Equals(right);
    }

    public static bool operator !=(LanguageTag left, LanguageTag right) {
        return !left.Equals(right);
    }
}
