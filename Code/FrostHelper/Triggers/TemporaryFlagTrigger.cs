namespace FrostHelper;

[CustomEntity("FrostHelper/TemporaryFlagTrigger")]
public class TemporaryFlagTrigger : Trigger {
    public readonly string Flag;

    public TemporaryFlagTrigger(EntityData data, Vector2 offset) : base(data, offset) {
        Flag = data.Attr("flag");

        // reset the flag ASAP
        FrostModule.GetCurrentLevel()?.Session.SetFlag(Flag, false);
    }

    public override void OnEnter(Player player) {
        base.OnEnter(player);

        FrostModule.GetCurrentLevel().Session.SetFlag(Flag, true);
    }
}
