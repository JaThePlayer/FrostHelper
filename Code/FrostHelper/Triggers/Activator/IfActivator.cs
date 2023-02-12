using FrostHelper.Helpers;
using static FrostHelper.Helpers.ConditionHelper;

namespace FrostHelper.Triggers.Activator;

/// <summary>
/// Only activates others if a flag is enabled
/// </summary>
[CustomEntity("FrostHelper/IfActivator")]
internal sealed class IfActivator : BaseActivator {
    Condition condition;

    public IfActivator(EntityData data, Vector2 offset) : base(data, offset) {
        condition = data.GetCondition("condition", "");

        Collidable = false;
    }

    public override void OnEnter(Player player) {
        base.OnEnter(player);

        if (condition.Check())
            ActivateAll(player);
    }
}