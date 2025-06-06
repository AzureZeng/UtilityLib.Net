// Copyright (c) Azure Zeng. All rights reserved.
// Licensed under the Apache License 2.0. See LICENSE in the project root for license information.

namespace AzureZeng.UtilityLib;

public static class UtilFunctions {
    public static void ForceDispose(IDisposable? disposable) {
        try {
            disposable?.Dispose();
        } catch (Exception _) {
            // ignore
        }
    }
 
    
}
