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
    public static T GetAttached<T>(this object obj) where T : class
        => DataStore<T>.Data.GetOrCreateValue(obj);
}
