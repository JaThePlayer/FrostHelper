using FrostHelper.Helpers;
using System.Reflection.Emit;
using OpCodes = System.Reflection.Emit.OpCodes;

namespace FrostHelper.SessionExpressions;

internal static class FieldAccessCommands {
    internal static readonly Dictionary<(Type, string), FieldAccessorCommand> Accessors = new() {
        [(typeof(Vector2), "len")] = new FieldAccessor<Vector2, float, Vector2LenAccessor>(),
        [(typeof(Vector2), "lenSq")] = new FieldAccessor<Vector2, float, Vector2LenSqAccessor>(),
        [(typeof(Vector2), "x")] = new FieldAccessor<Vector2, float, Vector2XAccessor>(),
        [(typeof(Vector2), "y")] = new FieldAccessor<Vector2, float, Vector2YAccessor>(),
        [(typeof(string), "len")] = new FieldAccessor<string, int, StringLenAccessor>(),
        [(typeof(Entity), "x")] = new PropertyInfoAccessor<Entity, float>(nameof(Entity.X)),
        [(typeof(Entity), "y")] = new PropertyInfoAccessor<Entity, float>(nameof(Entity.Y)),
        [(typeof(Entity), "pos")] = new FieldInfoAccessor<Entity, Vector2>(nameof(Entity.Position)),
        [(typeof(Entity), "sid")] = new FieldAccessor<Entity, string, EntitySidAccessor>(),
        
        [(typeof(EntityID), "roomName")] = new FieldInfoAccessor<EntityID, string>(nameof(EntityID.Level)),
        [(typeof(EntityID), "id")] = new FieldInfoAccessor<EntityID, int>(nameof(EntityID.ID)),
        
        [(typeof(IEnumerable), "count")] = new EnumerableCountAccessor(),
        
        [(typeof(Color), "r")] = new PropertyInfoStructAccessor<Color, byte, int>(nameof(Color.R)),
        [(typeof(Color), "g")] = new PropertyInfoStructAccessor<Color, byte, int>(nameof(Color.G)),
        [(typeof(Color), "b")] = new PropertyInfoStructAccessor<Color, byte, int>(nameof(Color.B)),
    };

    internal static ConditionHelper.Condition Create(string fieldName, ConditionHelper.Condition target, IExpressionContext ctx) {
        if (target.ReturnType is { } knownType && GetAccessor(knownType, fieldName, ctx) is { } accessor) {
            return new KnownFieldAccessor(target, accessor);
        }
        
        return new GeneralFieldAccessor(fieldName, target, ctx);
    }

    internal static FieldAccessorCommand? GetAccessor(Type? type, string fieldName, IExpressionContext ctx) {
        var currentType = type;
        while (currentType is not null) {
            if (Accessors.TryGetValue((currentType, fieldName), out var accessor)) {
                if (currentType != type) {
                    Accessors[(type!, fieldName)] = accessor;
                }
                return accessor;
            }

            foreach (var interfaceType in currentType.GetInterfaces()) {
                if (Accessors.TryGetValue((interfaceType, fieldName), out accessor)) {
                    if (currentType != type) {
                        Accessors[(type!, fieldName)] = accessor;
                    }
                    return accessor;
                }
            }

            currentType = currentType.BaseType;
        }

        return null;
    }

    private struct StringLenAccessor : IFieldAccessor<string, int> {
        public static int GetValue(string? obj) {
            return obj?.Length ?? 0;
        }
    }

    private struct Vector2LenAccessor : IFieldAccessor<Vector2, float> {
        public static float GetValue(Vector2 obj) {
            return obj.Length();
        }
    }

    private struct Vector2LenSqAccessor : IFieldAccessor<Vector2, float> {
        public static float GetValue(Vector2 obj) {
            return obj.LengthSquared();
        }
    }

    private struct Vector2XAccessor : IFieldAccessor<Vector2, float> {
        public static float GetValue(Vector2 obj) {
            return obj.X;
        }
    }

    private struct Vector2YAccessor : IFieldAccessor<Vector2, float> {
        public static float GetValue(Vector2 obj) {
            return obj.Y;
        }
    }

    private struct EntitySidAccessor : IFieldAccessor<Entity, string> {
        public static string GetValue(Entity? entity) {
            if (entity is null)
                return "?";
        
            var t = TypeHelper.TypeToEntityName(entity.GetType());
            if (t is { })
                return t;
            
            return "?";
        }
    }
}

internal abstract class FieldAccessorCommand {
    public abstract object GetValue(object? obj);

    public virtual Type ReturnType => typeof(object);

    public abstract void Emit(ConditionCompilationCtx ctx, Type? knownInputType, Type targetType);
}

internal sealed class GeneralFieldAccessor(string fieldName, ConditionHelper.Condition target, IExpressionContext ctx) : ConditionHelper.Condition {
    private FieldAccessorCommand? _permanentlyKnownFieldAccessor;
    private readonly Dictionary<Type, FieldAccessorCommand?> _cache = [];
    private readonly ConditionHelper.Condition _target = target;
    
    private static readonly FieldInfo TargetField = typeof(GeneralFieldAccessor).GetField("_target", BindingFlags.NonPublic | BindingFlags.Instance)!;
    
    public override object Get(Session session, object? userdata) {
        var t = _target.Get(session, userdata);

        if (_cache.TryGetValue(t.GetType(), out var cached)) {
            return cached?.GetValue(t) ?? Zero;
        }

        if (FieldAccessCommands.GetAccessor(t.GetType(), fieldName, ctx) is { } accessor) {
            _cache[t.GetType()] = accessor;
            return accessor.GetValue(t);
        }
        
        _cache[t.GetType()] = null;
        NotificationHelper.Notify($"Failed to get field '{fieldName}' on type '{t.GetType().Name}'");

        return Zero;
    }

    internal override void Emit(ConditionCompilationCtx ctx, Type targetType) {
        var knownAccessor = GetPermanentlyKnownFieldAccessor();
        if (knownAccessor is null) {
            base.Emit(ctx, targetType);
            return;
        }

        var il = ctx.Il;
        LocalBuilder? tempLoc = null;
        il.EmitSwapOutCurrentCondition(ref tempLoc, ctx, _target, TargetField);
        
        _target.Emit(ctx, _target.ReturnType!);

        il.EmitRevertCurrentCondition(tempLoc, ctx);
        
        knownAccessor.Emit(ctx, _target.ReturnType, targetType);
    }

    internal override bool UsesCurrentConditionLocalInEmit
        => GetPermanentlyKnownFieldAccessor() is null || _target.UsesCurrentConditionLocalInEmit;

    private FieldAccessorCommand? GetPermanentlyKnownFieldAccessor() {
        if (_permanentlyKnownFieldAccessor is not null)
            return _permanentlyKnownFieldAccessor;
        
        var fieldType = _target.ReturnType;
        if (fieldType is null)
            return null;

        if (_cache.TryGetValue(fieldType, out var cachedAccessor)) {
            return _permanentlyKnownFieldAccessor = cachedAccessor;
        }

        if (FieldAccessCommands.GetAccessor(fieldType, fieldName, ctx) is { } accessor) {
            return _permanentlyKnownFieldAccessor = accessor;
        }

        return null;
    }

    protected internal override Type? ReturnType => GetPermanentlyKnownFieldAccessor()?.ReturnType;
}

internal sealed class KnownFieldAccessor(ConditionHelper.Condition target, FieldAccessorCommand accessor) : ConditionHelper.Condition {
    private ConditionHelper.Condition _target = target;
    
    private static readonly FieldInfo TargetField = typeof(KnownFieldAccessor).GetField("_target", BindingFlags.NonPublic | BindingFlags.Instance)!;
    
    public override object Get(Session session, object? userdata) {
        var t = _target.Get(session, userdata);

        return accessor.GetValue(t);
    }

    internal override void Emit(ConditionCompilationCtx ctx, Type targetType) {
        var il = ctx.Il;
        LocalBuilder? tempLoc = null;
        il.EmitSwapOutCurrentCondition(ref tempLoc, ctx, _target, TargetField);
        
        _target.Emit(ctx, target.ReturnType!);

        il.EmitRevertCurrentCondition(tempLoc, ctx);
        
        accessor.Emit(ctx, target.ReturnType, targetType);
    }

    internal override bool UsesCurrentConditionLocalInEmit => _target.UsesCurrentConditionLocalInEmit;

    protected internal override Type ReturnType => accessor.ReturnType;
}

internal interface IFieldAccessor<in T, out TField> {
    static abstract TField GetValue(T? obj);
}

internal class FieldAccessor<T, TField, TImpl> : FieldAccessorCommand where TImpl : IFieldAccessor<T, TField> {
    public override Type ReturnType => typeof(TField);

    public override object GetValue(object? obj) {
        return TImpl.GetValue((T)obj!)!;
    }
    
    public override void Emit(ConditionCompilationCtx ctx, Type? knownInputType, Type targetType) {
        ctx.Il.Emit(OpCodes.Call, typeof(TImpl).GetMethod(nameof(TImpl.GetValue), BindingFlags.Public | BindingFlags.Static)!);
        ctx.Il.EmitConvertToInSessionExpression(typeof(TField), targetType);
    }
}

internal sealed class FieldInfoAccessor<T, TField>(string fieldName) : FieldAccessorCommand {
    private readonly FieldInfo _fieldInfo = typeof(T).GetField(fieldName)!;
    private readonly Func<T, TField> _getter = typeof(T).GetField(fieldName)!.CreateFastGetter<T, TField>();
    
    public override object GetValue(object? obj) {
        return _getter((T)obj!)!;
    }

    public override Type ReturnType => _fieldInfo.FieldType;
    
    public override void Emit(ConditionCompilationCtx ctx, Type? knownInputType, Type targetType) {
        ctx.Il.Emit(OpCodes.Ldfld, _fieldInfo);
        ctx.Il.EmitConvertToInSessionExpression(_fieldInfo.FieldType, targetType);
    }
}

internal sealed class PropertyInfoAccessor<T, TField>(string fieldName) : FieldAccessorCommand where T : class {
    private readonly PropertyInfo _propertyInfo = typeof(T).GetProperty(fieldName)!;
    private readonly Func<T, TField> _getter = typeof(T).GetProperty(fieldName)!.GetGetMethod()!.CreateDelegate<Func<T, TField>>();
    
    public override object GetValue(object? obj) {
        return _getter((T)obj!)!;
    }

    public override Type ReturnType => _propertyInfo.PropertyType;
    
    public override void Emit(ConditionCompilationCtx ctx, Type? knownInputType, Type targetType) {
        ctx.Il.Emit(OpCodes.Callvirt, _propertyInfo.GetMethod!);
        ctx.Il.EmitConvertToInSessionExpression(_propertyInfo.PropertyType, targetType);
    }
}

internal sealed class PropertyInfoStructAccessor<T, TField, TRetAs>(string fieldName) : FieldAccessorCommand where T : struct {
    delegate TField RefFunc(ref T value);
    
    private readonly PropertyInfo _propertyInfo = typeof(T).GetProperty(fieldName)!;
    private readonly RefFunc _getter = typeof(T).GetProperty(fieldName)!.GetGetMethod()!.CreateDelegate<RefFunc>();
    
    public override object GetValue(object? obj) {
        T val = (T) obj!;
        return ConditionHelper.Condition.Coerce<TRetAs>(_getter(ref val)!)!;
    }

    public override Type ReturnType => typeof(TRetAs);
    
    public override void Emit(ConditionCompilationCtx ctx, Type? knownInputType, Type targetType) {
        ctx.Il.Emit(OpCodes.Callvirt, _propertyInfo.GetMethod!);
        ctx.Il.EmitConvertToInSessionExpression(_propertyInfo.PropertyType, targetType);
    }
}

internal sealed class EnumerableCountAccessor : FieldAccessorCommand {
    public override object GetValue(object? obj) {
        return ((IEnumerable?) obj)?.Cast<object>().Count() ?? 0;
    }

    public override Type ReturnType => typeof(int);

    private static readonly MethodInfo Linq_Enumerable_Cast_T = typeof(Enumerable).GetMethod(nameof(Enumerable.Cast))!;
    private static readonly MethodInfo Linq_Enumerable_Count_T = typeof(Enumerable).GetMethod(nameof(Enumerable.Count), [ typeof(IEnumerable<>).MakeGenericType(Type.MakeGenericMethodParameter(0)) ])!;

    public override void Emit(ConditionCompilationCtx ctx, Type? knownInputType, Type targetType) {
        if (knownInputType?.GetProperty("Count") is { GetMethod: { } getCount } countProperty && countProperty.PropertyType == typeof(int)) {
            ctx.Il.Emit(OpCodes.Call, getCount);
        } else {
            ctx.Il.Emit(OpCodes.Call, Linq_Enumerable_Cast_T.MakeGenericMethod(typeof(object)));
            ctx.Il.Emit(OpCodes.Call, Linq_Enumerable_Count_T.MakeGenericMethod(typeof(object)));
        }

        ctx.EmitConvertTo(typeof(int), targetType);
    }
}
