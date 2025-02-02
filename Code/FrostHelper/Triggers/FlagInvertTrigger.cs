namespace FrostHelper.Triggers;

[CustomEntity("FrostHelper/FlagInvertTrigger")]
internal sealed class FlagInvertTrigger : Trigger {
    private readonly string Flag;
    
    public FlagInvertTrigger(EntityData data, Vector2 offset) : base(data, offset) {
        Flag = data.Attr("flag");
    }

    public override void OnEnter(Player player) {
        base.OnEnter(player);

        if (Scene is not Level level)
            return;
        
        var toChange = Flag;

        level.Session.SetFlag(toChange, !level.Session.GetFlag(toChange));
    }
}