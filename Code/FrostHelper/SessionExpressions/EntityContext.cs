using FrostHelper.Helpers;

namespace FrostHelper.SessionExpressions;

internal static class EntityContext {
    public static ExpressionContext Default { get; } = new(
        new() {
            
        }, 
        new() {
            
        }
    );
}

internal abstract class EntityCondition : ConditionHelper.Condition {
    protected abstract object GetValue(Session session, Entity entity);

    public override object Get(Session session, object? userdata) {
        if (userdata is Entity entity) {
            return GetValue(session, entity);
        }
        return Zero;
    }
}

internal abstract class EntityCondition<T> : ConditionHelper.Condition 
where T : Entity {
    protected abstract object GetValue(Session session, T entity);

    public override object Get(Session session, object? userdata) {
        if (userdata is T entity) {
            return GetValue(session, entity);
        }

        return Zero;
    }
}