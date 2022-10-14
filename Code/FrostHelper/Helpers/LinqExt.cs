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
}
