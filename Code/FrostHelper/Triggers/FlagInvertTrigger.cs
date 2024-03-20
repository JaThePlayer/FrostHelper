namespace FrostHelper.Triggers;

[CustomEntity("FrostHelper/FlagInvertTrigger")]
internal sealed class FlagInvertTrigger : Trigger {
    private readonly string Flag;
    
    public FlagInvertTrigger(EntityData data, Vector2 offset) : base(data, offset) {
        Flag = data.Attr("flag");
    }

    public override void OnEnter(Player player) {
        base.OnEnter(player);

        if (player.Scene is not Level level)
            return;
        
        var flags = level.Session.Flags;
        var toChange = Flag;

        if (!flags.Add(toChange))
            flags.Remove(toChange);
    }
}