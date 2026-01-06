using System.Runtime.CompilerServices;

namespace FrostHelper.Helpers;

public interface IStaticFunc<in TIn, out TOut> {
    public static abstract TOut Invoke(TIn arg);
}

public interface IFunc<in TIn, out TOut> {
    TOut Invoke(TIn arg);
}

struct ConstTrueFilter<T> : IFunc<T, bool>, IStaticFunc<T, bool> {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Invoke(T arg) {
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool IStaticFunc<T, bool>.Invoke(T arg) {
        return true;
    }
}
