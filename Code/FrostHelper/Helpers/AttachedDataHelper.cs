using System.Runtime.CompilerServices;

namespace FrostHelper.Helpers;

internal static class AttachedDataHelper {
    private static class DataStore<T> where T : class {
        public static ConditionalWeakTable<object, T> Data = new();
    }

    /// <summary>
    /// Obtains (or creates) arbitrary data attached to the given object, in a way more efficient than DynamicData
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T GetOrCreateAttached<T>(this object obj) where T : class
        => DataStore<T>.Data.GetOrCreateValue(obj);

    /// <summary>
    /// Obtains arbitrary data attached to the given object, in a way more efficient than DynamicData
    /// Does not create it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T? TryGetAttached<T>(this object obj) where T : class {
        DataStore<T>.Data.TryGetValue(obj, out var ret);

        return ret;
    }

    /// <summary>
    /// Sets arbitrary data attached to the given object, in a way more efficient than DynamicData
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetAttached<T>(this object obj, T val) where T : class {
        var data = DataStore<T>.Data;

        data.Remove(obj);
        data.Add(obj, val);
    }

}
