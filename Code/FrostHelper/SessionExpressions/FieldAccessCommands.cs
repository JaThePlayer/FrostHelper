using FrostHelper.API;
using FrostHelper.Helpers;
using System.Reflection.Emit;
using OpCodes = System.Reflection.Emit.OpCodes;

namespace FrostHelper.SessionExpressions;

internal static class FieldAccessCommands {
    static FieldAccessCommands() {
        Accessors = new();
        
        RegisterField<Vector2, float, Vector2XAccessor>("x", "The position of the vector on the x-axis, e.g. $vec(3, 4).x -> 3");
        RegisterField<Vector2, float, Vector2YAccessor>("y", "The position of the vector on the y-axis, e.g. $vec(3, 4).y -> 4");
        RegisterField<Vector2, float, Vector2LenAccessor>("len", "The length of the vector, calculated as $sqrt(x*x + y*y)");
        RegisterField<Vector2, float, Vector2LenSqAccessor>("lenSq", "The squared length of the vector, calculated as x*x + y*y. Faster to calculate than .len.");
        
        RegisterField<string, int, StringLenAccessor>("len", "The length (in characters) of the string, e.g. \"hello\".len -> 5");
        
        RegisterProperty<Entity, float>("x", nameof(Entity.X), "The position of the entity on the x-axis.");
        RegisterProperty<Entity, float>("y", nameof(Entity.Y), "The position of the entity on the y-axis.");
        RegisterField<Entity, Vector2>("pos", nameof(Entity.Position), "The position of the entity.");
        RegisterField<Entity, string, EntitySidAccessor>("sid", "The SID (String ID, used in map editors) of the entity, e.g. FrostHelper/IceSpinner for Custom Spinners. Can be ? if the SID cannot be obtained for the given entity.");
        
        RegisterField<EntityID, string>("roomName", nameof(EntityID.Level), "The name of the room the ID belongs to.");
        RegisterField<EntityID, int>("id", nameof(EntityID.ID), "The numerical ID, unique per-room.");
        
        RegisterStructProperty<Color, byte, int>("r", nameof(Color.R), "The Red channel of this color, between 0 and 255 inclusive.");
        RegisterStructProperty<Color, byte, int>("g", nameof(Color.G), "The Green channel of this color, between 0 and 255 inclusive.");
        RegisterStructProperty<Color, byte, int>("b", nameof(Color.B), "The Blue channel of this color, between 0 and 255 inclusive.");
        RegisterStructProperty<Color, byte, int>("a", nameof(Color.A), "The Alpha channel of this color, between 0 and 255 inclusive.");
        
        Accessors[(typeof(IEnumerable), "count")] = new EnumerableCountAccessor();
    }
    
    internal static readonly Dictionary<(Type, string), FieldAccessorCommand> Accessors;

    private static void RegisterField<T, TField, TImpl>(string name, string description)
        where TImpl : IFieldAccessor<T, TField>
        => RegisterField<T, TField, TImpl>(name, [ RenderPart.Default(description) ]);
    
    private static void RegisterField<T, TField, TImpl>(string name, IReadOnlyList<RenderPart> description)
        where TImpl : IFieldAccessor<T, TField> {
        Accessors[(typeof(T), name)] = new FieldAccessor<T, TField, TImpl>(name, description);
    }
    
    private static void RegisterField<T, TField>(string name, string fieldNameCSharp, string description)
        => RegisterField<T, TField>(name, fieldNameCSharp, [ RenderPart.Default(description) ]);
    
    private static void RegisterField<T, TField>(string name, string fieldNameCSharp, IReadOnlyList<RenderPart> description) {
        Accessors[(typeof(T), name)] = new FieldInfoAccessor<T, TField>(name, fieldNameCSharp, description);
    }
    
    private static void RegisterProperty<T, TField>(string name, string propertyNameCSharp, string description)
        where T : class
        => RegisterProperty<T, TField>(name, propertyNameCSharp, [ RenderPart.Default(description) ]);
    
    private static void RegisterProperty<T, TField>(string name, string propertyNameCSharp, IReadOnlyList<RenderPart> description)
        where T : class {
        Accessors[(typeof(T), name)] = new PropertyInfoAccessor<T, TField>(name, propertyNameCSharp, description);
    }
    
    private static void RegisterStructProperty<T, TField, TRetAs>(string name, string propertyNameCSharp, string description)
        where T : struct
        => RegisterStructProperty<T, TField, TRetAs>(name, propertyNameCSharp, [ RenderPart.Default(description) ]);
    
    private static void RegisterStructProperty<T, TField, TRetAs>(string name, string propertyNameCSharp, IReadOnlyList<RenderPart> description)
        where T : struct {
        Accessors[(typeof(T), name)] = new PropertyInfoStructAccessor<T, TField, TRetAs>(name, propertyNameCSharp, description);
    }

    internal static ConditionHelper.Condition Create(string fieldName, ConditionHelper.Condition target, IExpressionContext ctx) {
        if (target.ReturnType is { } knownType && GetAccessor(knownType, fieldName, ctx) is { } accessor) {
            return new KnownFieldAccessor(target, accessor) {
                SourceText = $".{fieldName}"
            };
        }
        
        return new GeneralFieldAccessor(fieldName, target, ctx) {
            SourceText = $".{fieldName}"
        };
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
    public CommandDescriptor Descriptor { get; set; }
    
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

internal sealed class KnownFieldAccessor : ConditionHelper.Condition {
    private ConditionHelper.Condition _target;
    private readonly FieldAccessorCommand _accessor;

    public KnownFieldAccessor(ConditionHelper.Condition target, FieldAccessorCommand accessor) {
        _accessor = accessor;
        _target = target;
        Descriptor = _accessor.Descriptor;
    }
    
    public ConditionHelper.Condition Target => _target;

    private static readonly FieldInfo TargetField = typeof(KnownFieldAccessor).GetField(nameof(_target), BindingFlags.NonPublic | BindingFlags.Instance)!;
    
    public override object Get(Session session, object? userdata) {
        var t = _target.Get(session, userdata);

        return _accessor.GetValue(t);
    }

    internal override void Emit(ConditionCompilationCtx ctx, Type targetType) {
        var il = ctx.Il;
        LocalBuilder? tempLoc = null;
        il.EmitSwapOutCurrentCondition(ref tempLoc, ctx, _target, TargetField);
        
        _target.Emit(ctx, _target.ReturnType!);

        il.EmitRevertCurrentCondition(tempLoc, ctx);
        
        _accessor.Emit(ctx, _target.ReturnType, targetType);
    }

    internal override bool UsesCurrentConditionLocalInEmit => _target.UsesCurrentConditionLocalInEmit;

    protected internal override Type ReturnType => _accessor.ReturnType;
}

internal interface IFieldAccessor<in T, out TField> {
    static abstract TField GetValue(T? obj);
}

internal class FieldAccessor<T, TField, TImpl> : FieldAccessorCommand where TImpl : IFieldAccessor<T, TField> {
    public FieldAccessor(string name, IReadOnlyList<RenderPart> description) {
        Descriptor = new CommandDescriptor {
            Name = name,
            DeclaringType = TypeDescriptor.For(typeof(T)),
            ReturnType = TypeDescriptor.For(typeof(TField)),
            Description = description,
        };
    }
    
    public override Type ReturnType => typeof(TField);

    public override object GetValue(object? obj) {
        return TImpl.GetValue((T)obj!)!;
    }
    
    public override void Emit(ConditionCompilationCtx ctx, Type? knownInputType, Type targetType) {
        ctx.Il.Emit(OpCodes.Call, typeof(TImpl).GetMethod(nameof(TImpl.GetValue), BindingFlags.Public | BindingFlags.Static)!);
        ctx.Il.EmitConvertToInSessionExpression(typeof(TField), targetType);
    }
}

internal sealed class FieldInfoAccessor<T, TField> : FieldAccessorCommand {
    private readonly FieldInfo _fieldInfo;
    private readonly Func<T, TField> _getter;

    public FieldInfoAccessor(string sessionExpressionName, string fieldName, IReadOnlyList<RenderPart> description) {
        _fieldInfo = typeof(T).GetField(fieldName)!;
        _getter = typeof(T).GetField(fieldName)!.CreateFastGetter<T, TField>();
        
        Descriptor = new CommandDescriptor {
            Name = sessionExpressionName,
            DeclaringType = TypeDescriptor.For(typeof(T)),
            ReturnType = TypeDescriptor.For(typeof(TField)),
            Description = description,
        };
    }

    public override object GetValue(object? obj) {
        return _getter((T)obj!)!;
    }

    public override Type ReturnType => _fieldInfo.FieldType;
    
    public override void Emit(ConditionCompilationCtx ctx, Type? knownInputType, Type targetType) {
        ctx.Il.Emit(OpCodes.Ldfld, _fieldInfo);
        ctx.Il.EmitConvertToInSessionExpression(_fieldInfo.FieldType, targetType);
    }
}

internal sealed class PropertyInfoAccessor<T, TField> : FieldAccessorCommand where T : class {
    private readonly PropertyInfo _propertyInfo;
    private readonly Func<T, TField> _getter;

    public PropertyInfoAccessor(string sessionExpressionName, string fieldName, IReadOnlyList<RenderPart> description) {
        _propertyInfo = typeof(T).GetProperty(fieldName)!;
        _getter = typeof(T).GetProperty(fieldName)!.GetGetMethod()!.CreateDelegate<Func<T, TField>>();

        Descriptor = new CommandDescriptor {
            Name = sessionExpressionName,
            DeclaringType = TypeDescriptor.For(typeof(T)),
            ReturnType = TypeDescriptor.For(typeof(TField)),
            Description = description,
        };
    }

    public override object GetValue(object? obj) {
        return _getter((T)obj!)!;
    }

    public override Type ReturnType => _propertyInfo.PropertyType;
    
    public override void Emit(ConditionCompilationCtx ctx, Type? knownInputType, Type targetType) {
        ctx.Il.Emit(OpCodes.Callvirt, _propertyInfo.GetMethod!);
        ctx.Il.EmitConvertToInSessionExpression(_propertyInfo.PropertyType, targetType);
    }
}

internal sealed class PropertyInfoStructAccessor<T, TField, TRetAs> : FieldAccessorCommand where T : struct {
    delegate TField RefFunc(ref T value);
    
    private readonly PropertyInfo _propertyInfo;
    private readonly RefFunc _getter;

    public PropertyInfoStructAccessor(string sessionExpressionName, string fieldName, IReadOnlyList<RenderPart> description) {
        _propertyInfo = typeof(T).GetProperty(fieldName)!;
        _getter = typeof(T).GetProperty(fieldName)!.GetGetMethod()!.CreateDelegate<RefFunc>();
        
        Descriptor = new CommandDescriptor {
            Name = sessionExpressionName,
            DeclaringType = TypeDescriptor.For(typeof(T)),
            ReturnType = TypeDescriptor.For(typeof(TRetAs)),
            Description = description,
        };
    }

    public override object GetValue(object? obj) {
        T val = (T) obj!;
        return ConditionHelper.Condition.Coerce<TRetAs>(_getter(ref val)!)!;
    }

    public override Type ReturnType => typeof(TRetAs);
    
    public override void Emit(ConditionCompilationCtx ctx, Type? knownInputType, Type targetType) {
        var loc = ctx.Il.DeclareLocal(typeof(T));
        ctx.Il.Emit(OpCodes.Stloc, loc);
        ctx.Il.Emit(OpCodes.Ldloca, loc);
        ctx.Il.Emit(OpCodes.Call, _propertyInfo.GetMethod!);
        ctx.Il.EmitConvertToInSessionExpression(_propertyInfo.PropertyType, targetType);
    }
}

internal sealed class EnumerableCountAccessor : FieldAccessorCommand {
    public EnumerableCountAccessor() {
        Descriptor = new CommandDescriptor {
            Name = "count",
            DeclaringType = TypeDescriptor.For(typeof(IEnumerable)),
            ReturnType = TypeDescriptor.For(typeof(int)),
            Description = [
                RenderPart.Default("Returns the amount of elements in this "),
                RenderPart.Type(TypeDescriptor.For(typeof(IEnumerable))),
                RenderPart.Default(".")
            ],
        };
    }
    
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
