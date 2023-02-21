using FrostHelper.DecalRegistry;
using FrostHelper.Entities.Boosters;
using System.Runtime.CompilerServices;
using static FrostHelper.Helpers.BackdropHelper;

namespace FrostHelper.Helpers;

internal static class AttachedDataHelper {
    private static class DataStore<T> where T : class {
        public static ConditionalWeakTable<object, object> Data = new();
        internal static ConditionalWeakTable<object, object>.CreateValueCallback Factory = (o) => Activator.CreateInstance<T>();
    }

    static Dictionary<Type, List<ConditionalWeakTable<object, object>>?> ObjectToPossibleDatas = new();

    static AttachedDataHelper() {
        RegisterData<Decal, RainbowDecalMarker>();
        RegisterData<Player, GenericCustomBooster>();

        // make sure that backdrops are registered before parallax, so that parallax can copy values from Backdrops
        RegisterData<Backdrop, OrigPositionData>();
        RegisterData<Parallax, DontUpdateInvisibleStylegroundsController.ParallaxWrapInfo>();
    }
    /// <summary>
    /// Registers that a given data type will get attached to the given object type.
    /// Exists for SpeedrunTool support.
    /// </summary>
    private static void RegisterData<TObj, TData>()
        where TData : class
        where TObj : class {
        RegisterData<TData>(typeof(TObj));
    }

    private static void RegisterData<TData>(Type objType) where TData : class {
        if (!ObjectToPossibleDatas.TryGetValue(objType, out var datas)) {
            datas = ObjectToPossibleDatas[objType] = new();
        }

        datas!.Add(DataStore<TData>.Data);

        // also add anything from base types
        var type = objType;
        while (type.BaseType is { } baseType) {
            if (ObjectToPossibleDatas.TryGetValue(baseType, out var baseDatas)) {
                datas.AddRange(baseDatas);
            }

            type = baseType;
        }
    }

    private static List<ConditionalWeakTable<object, object>>? GetPossibleDatas(Type startingType) {
        List<ConditionalWeakTable<object, object>>? datas;
        var type = startingType;
        while (!ObjectToPossibleDatas.TryGetValue(type, out datas)) {
            if (type.BaseType is not { } baseType) {
                ObjectToPossibleDatas[startingType] = null;
                return null;
            }
            type = baseType;
        }

        return datas;
    }

    /// <summary>
    /// Returns an array containing all data attached to the object, might contain null values. Returns null if there's no attached data.
    /// Exists for SpeedrunTool support, do not use otherwise.
    /// </summary>
    public static object?[]? GetAllData(object obj) {
        var datas = GetPossibleDatas(obj.GetType());
        if (datas is null) {
            return null;
        }

        var len = datas.Count;
        object[]? dataValues = null;
        for (int i = 0; i < len; i++) {
            datas[i].TryGetValue(obj, out var data);

            if (data is { }) {
                dataValues ??= new object[len];
                dataValues[i] = data;
            }
        }

        return dataValues;
    }

    /// <summary>
    /// Sets all attached data for this object. 
    /// The <paramref name="dataValues"/> array has to have elements in the same order as they're returned by <see cref="GetAllData(object)"/> for an object of this type.
    /// Exists for SpeedrunTool support, do not use otherwise.
    /// </summary>
    public static void SetAllData(object obj, object?[]? dataValues) {
        if (dataValues is null) {
            return;
        }

        var datas = GetPossibleDatas(obj.GetType());
        if (datas is null) {
            return;
        }

        var len = dataValues.Length;
        for (int i = 0; i < len; i++) {
            var data = datas[i];

            // remove even if dataValues[i] is null, in case we set the data to something else after GetAllData was called
            data.Remove(obj);

            if (dataValues[i] is { } value)
                data.Add(obj, value);
        }
    }


    /// <summary>
    /// Obtains (or creates) arbitrary data attached to the given object, in a way more efficient than DynamicData
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T GetOrCreateAttached<T>(this object obj) where T : class
        => (T) DataStore<T>.Data.GetValue(obj, DataStore<T>.Factory);

    /// <summary>
    /// Obtains arbitrary data attached to the given object, in a way more efficient than DynamicData
    /// Does not create it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T? TryGetAttached<T>(this object obj) where T : class {
        DataStore<T>.Data.TryGetValue(obj, out var ret);

        return (T) ret;
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
