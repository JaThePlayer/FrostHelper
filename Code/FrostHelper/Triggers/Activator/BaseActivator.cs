using System.Configuration;

namespace FrostHelper.Triggers.Activator;
internal class BaseActivator : Trigger {
    public Vector2[] Nodes;
    public readonly bool OnlyOnce;
    public readonly float Delay;

    internal List<Trigger>? ToActivate;

    public ActivationModes ActivationMode;
    private int _cycleModeIdx;

    public enum ActivationModes {
        All, // all triggers get activated at once
        Cycle, // each time this activator gets triggered, the next trigger gets activated, wrapping over to the first one once all other ones have been triggered.
        Random, // a random (seeded) trigger will be chosen.
    }

    public BaseActivator(EntityData data, Vector2 offset) : base(data, offset) {
        Nodes = data.NodesOffset(offset);
        OnlyOnce = data.Bool("once");
        Delay = data.Float("delay", 0f);
        ActivationMode = data.Enum("activationMode", ActivationModes.All);
    }

    public override void Awake(Scene scene) {
        base.Awake(scene);
        ToActivate = FastCollideAll<Trigger>();
    }

    public void ActivateAll(Player player) {
        if (Delay == 0) {
            InstantActivateAll(player);
        } else {
            Add(new Coroutine(DelayedActivateAll(player)));
        }

        if (OnlyOnce)
            RemoveSelf();
    }

    private IEnumerator DelayedActivateAll(Player player) {
        yield return Delay;
        InstantActivateAll(player);
    }

    public void InstantActivateAll(Player player) {
        ToActivate ??= FastCollideAll<Trigger>();

        if (ToActivate.Count == 0 || player is null || player.Scene is null)
            return;

        switch (ActivationMode) {
            case ActivationModes.All:
                foreach (var trigger in ToActivate) {
                    Activate(player, trigger);
                }
                break;
            case ActivationModes.Cycle:
                Activate(player, ToActivate[_cycleModeIdx]);
                _cycleModeIdx = (_cycleModeIdx + 1) % ToActivate.Count;
                break;
            case ActivationModes.Random:
                Activate(player, ToActivate[Calc.Random.Next(0, ToActivate.Count)]);
                break;
            default:
                break;
        }
    }

    private void Activate(Player player, Trigger trigger) {
        if (trigger.Scene is null)
            return;

        if (trigger.PlayerIsInside) {
            trigger.OnLeave(player);
        }
        trigger.OnEnter(player);
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
