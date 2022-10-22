using FMOD;
using System.Collections.Generic;

namespace FrostHelper;

internal static class LinqExt {
    /// <summary>
    /// Finds the first element in the <paramref name="source"/> enumerable that is of the type <typeparamref name="TTarget"/> for which <paramref name="condition"/> returns true
    /// </summary>
    public static TTarget? FirstOfTypeOrDefault<TSource, TTarget>(this IEnumerable<TSource> source, Func<TTarget, bool> condition)
        where TTarget : TSource {
        foreach (var item in source) {
            if (item is TTarget target && condition(target)) {
                return target;
            }
        }

        return default;
    }

    public static void Foreach<T>(this IEnumerable<T> source, Action<T> action) {
        foreach (var item in source) {
            action(item);
        }
    }

    /// <summary>
    /// A more efficient implementation of Max, as the .net framework implementation is literally just source.Select(selector).Max()...
    /// </summary>
    public static int Max<T>(this IEnumerable<T> source, Func<T, int> selector) {
        int value = 0;

        switch (source) {
            case T[] arr:
                foreach (var item in arr) {
                    var x = selector(item);
                    if (x > value) {
                        value = x;
                    }
                }
                return value;
            case List<T> list:
                foreach (var item in list) {
                    var x = selector(item);
                    if (x > value) {
                        value = x;
                    }
                }
                return value;
            default:
                foreach (var item in source) {
                    var x = selector(item);
                    if (x > value) {
                        value = x;
                    }
                }
                return value;
        }
    }
}
