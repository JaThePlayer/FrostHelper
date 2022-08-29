namespace FrostHelper;

/// <summary>
/// Makes a flag permanent if it already was set previously while the player was inside the trigger
/// TODO: lonn plugin
/// </summary>
[CustomEntity("FrostHelper/TurnFlagPermanentTrigger")]
public class TurnFlagPermanentTrigger : Trigger {
    public string Flag;

    public string PermFlag => $"{Flag}_perm";

    public TurnFlagPermanentTrigger(EntityData data, Vector2 offset) : base(data, offset) {
        Flag = data.Attr("flag");
    }

    public override void Added(Scene scene) {
        base.Added(scene);

        if (SceneAs<Level>().Session.GetFlag(PermFlag)) {
            SceneAs<Level>().Session.SetFlag(Flag);
        }
    }

    public override void Awake(Scene scene) {
        base.Awake(scene);

        if (SceneAs<Level>().Session.GetFlag(PermFlag)) {
            SceneAs<Level>().Session.SetFlag(Flag);
        }
    }

    public override void OnStay(Player player) {
        base.OnStay(player);

        if (SceneAs<Level>().Session.GetFlag(Flag)) {
            SceneAs<Level>().Session.SetFlag(PermFlag);
        }
    }
}
