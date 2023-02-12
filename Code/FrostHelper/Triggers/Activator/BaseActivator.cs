namespace FrostHelper.Triggers.Activator;

internal class BaseActivator : Trigger {
    public Vector2[] Nodes;
    public readonly bool OnlyOnce;
    public readonly float Delay;

    internal List<Trigger>? ToActivate;

    // used by Random and Cycle modes to keep track of which trigger to call OnStay for.
    // For the 'all' setting, all triggers get activated instead (though this value is still used to keep track of whether anything got activated or not)
    private Trigger? lastActivatedTrigger;

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

    /// <summary>
    /// Calls the OnStay method on all just activated triggers.
    /// </summary>
    /// <param name="player"></param>
    public void CallOnStay(Player? player = null) {
        return; // see comment in OnEntityEnterActivator.Update

        if (lastActivatedTrigger is { } tr) {
            if (tr.Scene != Scene) {
                lastActivatedTrigger = null;
                return;
            }

            player ??= Scene.Tracker.GetEntity<Player>();
            if (player is null)
                return;

            if (ActivationMode == ActivationModes.All) {
                foreach (var trigger in ToActivate!) {
                    trigger.OnStay(player);
                }
            } else {
                tr.OnStay(player);
            }
        }
    }

    /// <summary>
    /// Calls the OnLeave method on all just activated triggers.
    /// Then, forgets which triggers were activated
    /// </summary>
    /// <param name="player"></param>
    public void CallOnLeave(Player? player = null) {
        return; // see comment in OnEntityEnterActivator.Update

        if (lastActivatedTrigger is { } tr) {
            if (tr.Scene != Scene) {
                lastActivatedTrigger = null;
                return;
            }

            player ??= Scene.Tracker.GetEntity<Player>();
            if (player is null)
                return;

            if (ActivationMode == ActivationModes.All) {
                foreach (var trigger in ToActivate!) {
                    trigger.OnLeave(player);
                }
            } else {
                tr.OnLeave(player);
            }
        }

        lastActivatedTrigger = null;
    }

    private IEnumerator DelayedActivateAll(Player player) {
        yield return Delay;
        InstantActivateAll(player);
    }

    public void InstantActivateAll(Player player) {
        // There's a chance for an activator to get triggered *before* Awake.
        ToActivate ??= FastCollideAll<Trigger>();

        if (ToActivate.Count == 0 || player is null || player.Scene is null)
            return;

        CallOnLeave(player);
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
        // Some triggers only do their thing in OnStay, so let's call that as well
#warning TODO: Add setting to call this each frame...
        trigger.OnStay(player);

        lastActivatedTrigger = trigger;
    }

    /// <summary>
    /// Struct containing a <typeparamref name="T"/> and an int index. Used because value tuples don't exist in framework :(
    /// </summary>
    /// <typeparam name="T"></typeparam>
    private struct Indexed<T> {
        public T Value;
        public int Index;
    }

    private List<T> FastCollideAll<T>() where T : Trigger {
        // When we're cycling, the order of triggered triggers should be decided by node order, so that it's manipulatable easily.
        // In all other modes, order is not important, so we don't sort at all for better performance
        // Since the index is unnecessary on non-cycle modes, we'll create a List<T> directly, instead of going through Indexed<T> to then re-allocate it into List<T>.
        // TODO: maybe rewrite into storing the indexes separately, in a stack-allocated buffer??
        List<T>? into = null;
        List<Indexed<T>>? intoWithIndexes = null;
        if (ActivationMode == ActivationModes.Cycle) {
            intoWithIndexes = new();
        } else {
            into = new();
        }
        var nodes = Nodes;

        foreach (T entity in Scene.Tracker.GetEntities<T>()) {
            var ePos = entity.Position;
            var eCol = (Hitbox) entity.Collider;

            var eRight = ePos.X + eCol.Width;
            var eBottom = ePos.Y + eCol.Height;

            for (int i = 0; i < nodes.Length; i++) {
                Vector2 node = nodes[i];
                if (node.X < eRight
                 && node.X > ePos.X
                 && node.Y < eBottom
                 && node.Y > ePos.Y) {
                    if (into is { })
                        into.Add(entity);
                    else
                        intoWithIndexes!.Add(new() {
                            Value = entity,
                            Index = i,
                        });
                    break;
                }
            }
        }

        // If we have kept track of indexes, then we need to sort
        if (intoWithIndexes is { }) {
            intoWithIndexes.Sort((p1, p2) => p2.Index - p1.Index);

            // Now, convert our list to just a List<T>.
            // Done manually for performance and less allocations.
            into = new(intoWithIndexes.Count);
            foreach (var item in intoWithIndexes) {
                into.Add(item.Value);
            }
        }


        return into!;
    }
}
