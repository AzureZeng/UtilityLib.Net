// Copyright (c) Azure Zeng. All rights reserved.
// See LICENSE in the project root for license information.

namespace AzureZeng.UtilityLib.TimeZone;

public interface ILikelySubTagLookup {
    LanguageTag Fill(LanguageTag languageTag);

    public LanguageTag TryRemoveScript(LanguageTag languageId) {
        LanguageTag n = languageId;
        n.Script = string.Empty;
        var fill = Fill(n);
        if (languageId == fill) return n;
        return languageId;
    }
}
