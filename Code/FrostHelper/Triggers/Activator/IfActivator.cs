using FrostHelper.Helpers;
using static FrostHelper.Helpers.ConditionHelper;

namespace FrostHelper.Triggers.Activator;

/// <summary>
/// Only activates others if a flag is enabled
/// </summary>
[CustomEntity("FrostHelper/IfActivator")]
internal sealed class IfActivator : BaseActivator, IIfActivator {
    private readonly Condition _condition;

    public IfActivator(EntityData data, Vector2 offset) : base(data, offset) {
        _condition = data.GetCondition("condition", "");
        IsElse = data.Bool("isElse", false);

        Collidable = false;
    }

    public override void OnEnter(Player player) {
        base.OnEnter(player);

        if (_condition.Check())
            ActivateAll(player);
        else
            ActiveElseBlocks(player);
    }

    public bool IsElse { get; }
}