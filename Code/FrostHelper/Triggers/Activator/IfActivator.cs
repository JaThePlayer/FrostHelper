using FrostHelper.Helpers;
using static FrostHelper.Helpers.ConditionHelper;

namespace FrostHelper.Triggers.Activator;

/// <summary>
/// Only activates others if a flag is enabled
/// </summary>
[CustomEntity("FrostHelper/IfActivator")]
internal sealed class IfActivator : BaseActivator, IIfActivator {
    readonly Condition condition;

    public IfActivator(EntityData data, Vector2 offset) : base(data, offset) {
        condition = data.GetCondition("condition", "");
        IsElse = data.Bool("isElse", false);

        Collidable = false;
    }

    public override void OnEnter(Player player) {
        base.OnEnter(player);

        if (condition.Check())
            ActivateAll(player);
        else
            ActiveElseBlocks(player);
    }

    public bool IsElse { get; }
}