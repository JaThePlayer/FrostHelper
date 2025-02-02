using FrostHelper.Helpers;

namespace FrostHelper.SessionExpressions;

internal static class FieldAccessCommands {
    internal static readonly Dictionary<(Type, string), FieldAccessorCommand> Accessors = new() {
        [(typeof(Vector2), "len")] = new Vector2LenAccessor(),
        [(typeof(Vector2), "lenSq")] = new Vector2LenSqAccessor(),
        [(typeof(Vector2), "x")] = new Vector2XAccessor(),
        [(typeof(Vector2), "y")] = new Vector2YAccessor(),
        [(typeof(string), "len")] = new StringLenAccessor(),
        [(typeof(Entity), "x")] = new PropertyInfoAccessor<Entity, float>(nameof(Entity.X)),
        [(typeof(Entity), "y")] = new PropertyInfoAccessor<Entity, float>(nameof(Entity.Y)),
        [(typeof(Entity), "pos")] = new FieldInfoAccessor<Entity, Vector2>(nameof(Entity.Position)),
        [(typeof(Entity), "sid")] = new EntitySidAccessor(),
    };

    internal static ConditionHelper.Condition Create(string fieldName, ConditionHelper.Condition target, ExpressionContext ctx) {
        if (target.ReturnType is { } knownType && GetAccessor(knownType, fieldName, ctx) is { } accessor) {
            return new KnownFieldAccessor(target, accessor);
        }
        
        return new GeneralFieldAccessor(fieldName, target, ctx);
    }

    internal static FieldAccessorCommand? GetAccessor(Type? type, string fieldName, ExpressionContext ctx) {
        while (type is not null) {
            if (Accessors.TryGetValue((type, fieldName), out var accessor)) {
                return accessor;
            }

            type = type.BaseType;
        }

        return null;
    }

    private sealed class StringLenAccessor : FieldAccessor<string> {
        protected override object GetValue(string? obj) {
            return obj?.Length ?? 0;
        }

        public override Type ReturnType => typeof(int);
    }

    private sealed class Vector2LenAccessor : FieldAccessor<Vector2> {
        protected override object GetValue(Vector2 obj) {
            return obj.Length();
        }

        public override Type ReturnType => typeof(float);
    }

    private sealed class Vector2LenSqAccessor : FieldAccessor<Vector2> {
        protected override object GetValue(Vector2 obj) {
            return obj.LengthSquared();
        }

        public override Type ReturnType => typeof(float);
    }

    private sealed class Vector2XAccessor : FieldAccessor<Vector2> {
        protected override object GetValue(Vector2 obj) {
            return obj.X;
        }

        public override Type ReturnType => typeof(float);
    }

    private sealed class Vector2YAccessor : FieldAccessor<Vector2> {
        protected override object GetValue(Vector2 obj) {
            return obj.Y;
        }

        public override Type ReturnType => typeof(float);
    }

    private sealed class EntitySidAccessor : FieldAccessor<Entity> {
        protected override object GetValue(Entity? entity) {
            if (entity is null)
                return "?";
        
            var t = TypeHelper.TypeToEntityName(entity.GetType());
            if (t is { })
                return t;
            
            return "?";
        }
        
        public override Type ReturnType => typeof(string);
    }
}

internal abstract class FieldAccessorCommand {
    public abstract object GetValue(object? obj);

    public virtual Type ReturnType => typeof(object);
}

internal sealed class GeneralFieldAccessor(string fieldName, ConditionHelper.Condition target, ExpressionContext ctx) : ConditionHelper.Condition {
    public override object Get(Session session, object? userdata) {
        var t = target.Get(session, userdata);

        if (FieldAccessCommands.GetAccessor(t.GetType(), fieldName, ctx) is {} accessor)
            return accessor.GetValue(t);

        if (_loggedMissingFields.Add((t.GetType(), fieldName))) {
            NotificationHelper.Notify($"Failed to get field '{fieldName}' on type '{t.GetType().Name}'");
        }

        return 0;
    }
    
    private readonly HashSet<(Type, string)> _loggedMissingFields = [];
}

internal sealed class KnownFieldAccessor(ConditionHelper.Condition target, FieldAccessorCommand accessor) : ConditionHelper.Condition {
    public override object Get(Session session, object? userdata) {
        var t = target.Get(session, userdata);

        return accessor.GetValue(t);
    }

    protected internal override Type ReturnType => accessor.ReturnType;
}

internal abstract class FieldAccessor<T> : FieldAccessorCommand {
    protected abstract object GetValue(T? obj);
    
    public override object GetValue(object? obj) {
        return GetValue((T)obj!);
    }
}

internal sealed class FieldInfoAccessor<T, TField>(string fieldName) : FieldAccessorCommand {
    private readonly FieldInfo _fieldInfo = typeof(T).GetField(fieldName)!;
    private readonly Func<T, TField> _getter = typeof(T).GetField(fieldName)!.CreateFastGetter<T, TField>();
    
    public override object GetValue(object? obj) {
        return _getter((T)obj!)!;
    }

    public override Type ReturnType => _fieldInfo.FieldType;
}

internal sealed class PropertyInfoAccessor<T, TField>(string fieldName) : FieldAccessorCommand {
    private readonly PropertyInfo _fieldInfo = typeof(T).GetProperty(fieldName)!;
    private readonly Func<T, TField> _getter = typeof(T).GetProperty(fieldName)!.GetGetMethod()!.CreateDelegate<Func<T, TField>>();
    
    public override object GetValue(object? obj) {
        return _getter((T)obj!)!;
    }

    public override Type ReturnType => _fieldInfo.PropertyType;
}
