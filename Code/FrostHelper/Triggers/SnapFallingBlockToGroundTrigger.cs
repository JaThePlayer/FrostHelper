namespace FrostHelper;

[CustomEntity("FrostHelper/SnapFallingBlockToGround")]
public class SnapFallingBlockToGroundTrigger : Trigger {
    public readonly string Flag;

    public SnapFallingBlockToGroundTrigger(EntityData data, Vector2 offset) : base(data, offset) {
        Flag = data.Attr("flag");

        Tag = Tags.TransitionUpdate;
    }

    public override void Update() {
        base.Update();

        Level level = (Scene as Level)!;

        if (level.Session.GetFlag(Flag)) {
            foreach (FallingBlock item in CollideAll<FallingBlock>()) {
                while (true) {
                    const int speed = 1;
                    if (item.MoveVCollideSolids(speed, true, null)) {
                        break;
                    }
                    if (item.Top > (level.Bounds.Bottom + 16) || (item.Top > level.Bounds.Bottom - 1 && item.CollideCheck<Solid>(item.Position + new Vector2(0f, 1f)))) {
                        break;
                    }
                }

                item.Get<Coroutine>()?.RemoveSelf();
            }
        }

        RemoveSelf();
    }
}
