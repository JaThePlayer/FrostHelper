namespace FrostHelper;

[CustomEntity("FrostHelper/FlagIfFlagTrigger")]
public class FlagIfFlagTrigger : Trigger {
    public string IfFlag;

    public string Flag;

    public FlagIfFlagTrigger(EntityData data, Vector2 offset) : base(data, offset) {
        Flag = data.Attr("flag");
        IfFlag = data.Attr("ifFlag");
    }

    public override void OnStay(Player player) {
        base.OnStay(player);

        if (SceneAs<Level>().Session.GetFlag(IfFlag)) {
            SceneAs<Level>().Session.SetFlag(Flag);
        }
    }
}
