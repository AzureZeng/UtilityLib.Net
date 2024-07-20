// Copyright (c) Azure Zeng. All rights reserved.
// Licensed under the Apache License 2.0. See LICENSE in the project root for license information.

namespace AzureZeng.UtilityLib;

# nullable disable
/// <summary>
/// Some useful object scope extension functions.
/// </summary>
public static class ScopeFunctions {
    public static T Apply<T>(this T obj, Action<T> action) {
        if (action == null) return obj;
        action(obj);
        return obj;
    }

    public static TOut Let<TIn, TOut>(TIn input, Func<TIn, TOut> func) {
        if (func == null) throw new ArgumentNullException(nameof(func));
        return func(input);
    }
}
