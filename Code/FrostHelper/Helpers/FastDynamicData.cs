using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FrostHelper.Helpers;

internal static class FastDynamicData {
    public static T? GetDynamicDataField<T>(this object obj, string fieldName) {
        return DynamicData.For(obj).Data.TryGetValue(fieldName, out var ret) ? (T?) ret : default;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T? GetDynamicDataAttached<T>(this object obj) where T : class, IAttachable {
        return DynamicData.For(obj).Data.TryGetValue(typeof(T).Name /*T.DynamicDataName*/, out var ret) ? (T?) ret : default;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T GetOrCreateDynamicDataAttached<T>(this object obj) where T : class, IAttachable, new() {
        ref var sl = ref CollectionsMarshal.GetValueRefOrAddDefault(DynamicData.For(obj).Data, typeof(T).Name /*T.DynamicDataName*/, out var exists);
        sl ??= new T();
        
        return (T)sl;
    }
    
    public static void SetDynamicDataField(this object obj, string fieldName, object? value) {
        DynamicData.For(obj).Data[fieldName] = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetDynamicDataAttached<T>(this object obj, T? value) where T : class, IAttachable {
        DynamicData.For(obj).Data[typeof(T).Name /*T.DynamicDataName*/] = value;
    }
    /*
    // ConditionalWeakTable<object, _Data_>
    private static readonly object? DataMap;

    private static Func<object, object, object> _g;
    private static Func<object, Dictionary<string, object?>> _getDataFrom__Data__;

    private static object G<T>(object obj, object tblO) where T : class {
        var tbl = (ConditionalWeakTable<object, T>) tblO;
        lock (tbl) {
            if (!tbl.TryGetValue(obj, out var data)) {
                data = (T)Activator.CreateInstance(typeof(T), [obj.GetType()])!;
                tbl.Add(obj, data);
            }

            return data;
        }
    }

    private static Dictionary<string, object?>? GetData(object obj) {
        if (DataMap is {})
            return _getDataFrom__Data__(_g(obj, DataMap));
        return null;
    }

    public static T? GetDynamicDataField<T>(this object obj, string fieldName) {
        if (GetData(obj) is { } d)
            return (T?)d.GetValueOrDefault(fieldName);

        return DynamicData.For(obj).Get<T>(fieldName);
    }

    public static T? GetDynamicDataAttached<T>(this object obj) where T : class, IAttachable {
        var fieldName = T.DynamicDataName;
        if (GetData(obj) is { } d)
            return (T?)d.GetValueOrDefault(fieldName);

        return DynamicData.For(obj).Get<T>(fieldName);
    }

    public static T GetOrCreateDynamicDataAttached<T>(this object obj) where T : class, IAttachable, new() {
        var fieldName = T.DynamicDataName;
        if (GetData(obj) is { } d) {
            if (d.TryGetValue(fieldName, out var t))
                return (T)t!;

            t = new T();
            d[fieldName] = t;
            return (T)t;
        }

        var dData = DynamicData.For(obj);
        if (dData.Get<T>(fieldName) is { } t2) {
            return t2;
        }

        t2 = new T();
        dData.Set(fieldName, t2);
        return t2;
    }

    public static void SetDynamicDataField(this object obj, string fieldName, object? value) {
        if (GetData(obj) is { } d) {
            d[fieldName] = value;
        } else {
            DynamicData.For(obj).Set(fieldName, value);
        }
    }

    public static void SetDynamicDataAttached<T>(this object obj, T? value) where T : class, IAttachable {
        var fieldName = T.DynamicDataName;
        if (GetData(obj) is { } d) {
            d[fieldName] = value;
        } else {
            DynamicData.For(obj).Set(fieldName, value);
        }
    }

    static FastDynamicData() {
        DataMap = typeof(DynamicData)
            .GetField("_DataMap", BindingFlags.Static | BindingFlags.NonPublic)?
            .GetValue(null);

        if (DataMap is null)
            return;

        var dataType = DataMap.GetType().GenericTypeArguments[1];

        var method = new DynamicMethodDefinition("FrostHelper.FastDynamicData.$<GetData>", typeof(Dictionary<string, object?>), [typeof(object)]);
        var il = method.GetILProcessor();

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, dataType.GetField("Data", BindingFlags.Instance | BindingFlags.Public)!);
        il.Emit(OpCodes.Ret);

        _getDataFrom__Data__ = method.Generate().CreateDelegate<Func<object, Dictionary<string, object?>>>();// ()Delegate.CreateDelegate(typeof(Func<object, Dictionary<string, object?>>), );

        _g = typeof(FastDynamicData).GetMethod(nameof(G), BindingFlags.Static | BindingFlags.NonPublic)!
            .MakeGenericMethod([dataType])
            .CreateDelegate<Func<object, object, object>>();
    }
    */
}

internal interface IAttachable {
    // Causes crashes in eevee helper due to it using dynamiddata on an interface?!?!
    // Bring this back once MonoMod is updated to fix the crash:
    // public static abstract string DynamicDataName { get; }
}