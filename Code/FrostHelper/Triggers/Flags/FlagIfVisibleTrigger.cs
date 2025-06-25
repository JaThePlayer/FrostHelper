namespace FrostHelper;

[Tracked]
[CustomEntity("FrostHelper/FlagIfVisibleTrigger")]
public class FlagIfVisibleTrigger : Entity {
    public string Flag;
    public int W, H;

    public FlagIfVisibleTrigger[] ChildTriggers;

    private Level lvl;



    public FlagIfVisibleTrigger(EntityData data, Vector2 offset) : base(data.Position + offset) {
        Flag = data.Attr("flag");
        Collidable = false;
        Visible = false;
        W = data.Width;
        H = data.Height;
    }

    public bool InView() {
        Camera camera = lvl.Camera;
        return Utils.CreateRect(X, Y, W, H).Intersects(Utils.CreateRect(camera.X, camera.Y, 320f, 180f));
    }

    public override void Added(Scene scene) {
        base.Added(scene);
        lvl = (Scene as Level)!;
    }

    public override void Awake(Scene scene) {
        base.Awake(scene);

        if (!Active)
            return;

        ChildTriggers = scene.Tracker.SafeGetEntities<FlagIfVisibleTrigger>()
            .Cast<FlagIfVisibleTrigger>()
            .Where(t => t != this && t.Active && t.Flag == Flag)
            .ToArray();

        foreach (var trigger in ChildTriggers) {
            trigger.Active = false;
        }
    }

    public override void Update() {
        bool visible = InView();
        if (!visible)
            foreach (var item in ChildTriggers) {
                if (visible = item.InView())
                    break;
            }

        lvl.Session.SetFlag(Flag, visible);
    }
}
