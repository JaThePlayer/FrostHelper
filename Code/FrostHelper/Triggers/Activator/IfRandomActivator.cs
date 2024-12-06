using FrostHelper.Helpers;

namespace FrostHelper.Triggers.Activator;

[CustomEntity("FrostHelper/IfRandomActivator")]
internal sealed class IfRandomActivator : BaseActivator, IIfActivator {
    private readonly SessionRandomGetter _sessionRandom;
    private readonly ConditionHelper.Condition _chance;
    
    public IfRandomActivator(EntityData data, Vector2 offset) : base(data, offset) {
        Collidable = false;
        
        _chance = data.GetCondition("chance", "50");
        
        _sessionRandom = new SessionRandomGetter(
            ConditionHelper.CreateOrDefault("0", ""),
            ConditionHelper.CreateOrDefault("100", ""),
            data.Enum("seedMode", SessionRandomGetter.SeedModes.SessionTime),
            data.Int("seed")
        );
        IsElse = data.Bool("isElse", false);
    }

    public override void OnEnter(Player player) {
        base.OnEnter(player);

        var session = player.level.Session;
        var roll = _sessionRandom.GetFloat(session);

        if (roll <= _chance.GetFloat(session)) {
            ActivateAll(player);
        } else {
            ActiveElseBlocks(player);
        }
    }

    public bool IsElse { get; }
}