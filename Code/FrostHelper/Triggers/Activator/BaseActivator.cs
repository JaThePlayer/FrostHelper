namespace FrostHelper.Triggers.Activator;
internal class BaseActivator : Trigger {
    public Vector2[] Nodes;
    public readonly bool OnlyOnce;
    public readonly float Delay;

    internal List<Trigger>? ToActivate;

    public BaseActivator(EntityData data, Vector2 offset) : base(data, offset) {
        Nodes = data.NodesOffset(offset);
        OnlyOnce = data.Bool("once");
        Delay = data.Float("delay", 0f);
    }

    public override void Awake(Scene scene) {
        base.Awake(scene);
        ToActivate = FastCollideAll<Trigger>();
    }

    public void ActivateAll(Player player) {
        if (Delay == 0) {
            DoActivateAll(player);
        } else {
            Add(new Coroutine(DelayedActivateAll(player)));
        }

        if (OnlyOnce)
            RemoveSelf();
    }

    private IEnumerator DelayedActivateAll(Player player) {
        yield return Delay;
        DoActivateAll(player);
    }

    private void DoActivateAll(Player player) {
        ToActivate ??= FastCollideAll<Trigger>();

        foreach (var trigger in ToActivate) {
            if (trigger.Scene is not { })
                continue;

            if (trigger.PlayerIsInside) {
                trigger.OnLeave(player);
            }
            trigger.OnEnter(player);
        }
    }

    private List<T> FastCollideAll<T>() where T : Trigger {
        var into = new List<T>();
        var nodes = Nodes;

        foreach (T entity in Scene.Tracker.GetEntities<T>()) {
            var ePos = entity.Position;
            var eCol = (Hitbox)entity.Collider;

            var eRight = ePos.X + eCol.Width;
            var eBottom = ePos.Y + eCol.Height;

            foreach (var node in nodes) {
                if (node.X < eRight
                 && node.X > ePos.X
                 && node.Y < eBottom
                 && node.Y > ePos.Y) {
                    into.Add(entity);
                    break;
                }
            }

        }
        return into;
    }
}
