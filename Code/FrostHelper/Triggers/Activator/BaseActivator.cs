namespace FrostHelper.Triggers.Activator;

internal interface IIfActivator {
    public bool IsElse { get; }
}

public class BaseActivator : Trigger {
    public Vector2[] Nodes;
    public readonly bool OnlyOnce;
    public readonly float Delay;
    public bool ActivateAfterDeath;


    internal (List<Trigger> main, List<Trigger> elseBranch)? ToActivate;

    internal List<Trigger>?[]? ToActivatePerNode;
    internal List<Trigger>?[]? ToActivateElsePerNode;
    
    internal virtual bool NeedsNodeIndexes => false;

    // used by Random and Cycle modes to keep track of which trigger to call OnStay for.
    // For the 'all' setting, all triggers get activated instead (though this value is still used to keep track of whether anything got activated or not)
    private Trigger? lastActivatedTrigger;

    public ActivationModes ActivationMode;
    private int _cycleModeIdx;

    public enum ActivationModes {
        AllOrdered, // all triggers get activated at once, ordered by node id. Map editor default.
        CycleCorrect, // each time this activator gets triggered, the next trigger gets activated, wrapping over to the first one once all other ones have been triggered.
        Cycle, // CycleCorrect, but orders in the opposite direction, so the last node gets activated first...
        Random, // a random (seeded) trigger will be chosen.
        All, // all triggers get activated at once - deprecated
    }

    public BaseActivator(EntityData data, Vector2 offset) : base(data, offset) {
        Nodes = data.NodesOffset(offset);
        OnlyOnce = data.Bool("once");
        Delay = data.Float("delay", 0f);
        ActivationMode = data.Enum("activationMode", ActivationModes.All);
        ActivateAfterDeath = data.Bool("activateAfterDeath", false);
    }

    public override void Awake(Scene scene) {
        base.Awake(scene);
        ToActivate ??= FastCollideAll();
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

    public void ActiveElseBlocks(Player player) {
        if (Delay == 0) {
            InstantActivateAll(player, activateElseBranch: true);
        } else {
            Add(new Coroutine(DelayedActivateAll(player, activateElseBranch: true)));
        }

        if (OnlyOnce)
            RemoveSelf();
    }

    public void ActivateAtNode(Player player, int nodeIdx) {
        ToActivate ??= FastCollideAll();

        if (ToActivatePerNode is null)
            throw new Exception(
                $"ActivateAtNode called for an Activator [{GetType()}] which doesn't keep track of nodes. This is a Frost Helper bug!");

        var toActivate = ToActivatePerNode.ElementAtOrDefault(nodeIdx);

        if (toActivate is {Count: > 0 }) {
            if (Delay == 0) {
                InstantActivateAll(player, toActivate);
            } else {
                Add(new Coroutine(DelayedActivateAll(player, toActivate)));
            }
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

#pragma warning disable CS0162 // Unreachable code detected
        if (lastActivatedTrigger is { } tr) {
            if (tr.Scene != Scene) {
                lastActivatedTrigger = null;
                return;
            }

            player ??= Scene.Tracker.GetEntity<Player>();
            if (player is null)
                return;

            if (ActivationMode == ActivationModes.All) {
                foreach (var trigger in ToActivate.Value.main) {
                    trigger.OnStay(player);
                }
            } else {
                tr.OnStay(player);
            }
        }
#pragma warning restore CS0162 // Unreachable code detected
    }

    /// <summary>
    /// Calls the OnLeave method on all just activated triggers.
    /// Then, forgets which triggers were activated
    /// </summary>
    /// <param name="player"></param>
    public void CallOnLeave(Player? player = null) {
        return; // see comment in OnEntityEnterActivator.Update

#pragma warning disable CS0162 // Unreachable code detected
        if (lastActivatedTrigger is { } tr) {
            if (tr.Scene != Scene) {
                lastActivatedTrigger = null;
                return;
            }

            player ??= Scene.Tracker.GetEntity<Player>();
            if (player is null)
                return;

            if (ActivationMode == ActivationModes.All) {
                foreach (var trigger in ToActivate.Value.main) {
                    trigger.OnLeave(player);
                }
            } else {
                tr.OnLeave(player);
            }
        }

        lastActivatedTrigger = null;
#pragma warning restore CS0162 // Unreachable code detected
    }

    private IEnumerator DelayedActivateAll(Player player, bool activateElseBranch = false) {
        yield return Delay;
        InstantActivateAll(player, activateElseBranch);
    }
    
    private IEnumerator DelayedActivateAll(Player player, List<Trigger> toActivate) {
        ToActivate ??= FastCollideAll();
        yield return Delay;
        InstantActivateAll(player, toActivate);
    }

    public void InstantActivateAll(Player player, bool activateElseBranch = false) {
        // There's a chance for an activator to get triggered *before* Awake.
        ToActivate ??= FastCollideAll();

        InstantActivateAll(player, activateElseBranch ? ToActivate.Value.elseBranch : ToActivate.Value.main);
    }

    internal void InstantActivateAll(Player player, List<Trigger> toActivate) {
        if (toActivate.Count == 0 || ((player?.Scene is null) && !ActivateAfterDeath))
            return;
        CallOnLeave(player);
        switch (ActivationMode) {
            case ActivationModes.All:
                foreach (var trigger in toActivate) {
                    Activate(player!, trigger);
                }
                break;
            case ActivationModes.AllOrdered:
                foreach (var trigger in toActivate) {
                    Activate(player!, trigger);
                }
                break;
            case ActivationModes.Cycle or ActivationModes.CycleCorrect:
                Activate(player!, toActivate[_cycleModeIdx]);
                _cycleModeIdx = (_cycleModeIdx + 1) % toActivate.Count;
                break;
            case ActivationModes.Random:
                Activate(player!, toActivate[Calc.Random.Next(0, toActivate.Count)]);
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
    internal struct Indexed<T> {
        public T Value;
        public int Index;
    }

    internal (List<Trigger> main, List<Trigger> elseBranch) FastCollideAll() {
        static void Add(Trigger entity, int i, List<Trigger>? into, List<Trigger>? intoElse, List<Indexed<Trigger>>? intoWithIndexes, List<Indexed<Trigger>>? intoElseWithIndexes) {
            if (entity is IIfActivator { IsElse: true }) {
                if (intoElse is { })
                    intoElse.Add(entity);
                else
                    intoElseWithIndexes!.Add(new() {
                        Value = entity,
                        Index = i,
                    });
            } else {
                if (into is { })
                    into.Add(entity);
                else
                    intoWithIndexes!.Add(new() {
                        Value = entity,
                        Index = i,
                    });
            }
        }
        
        // When we're cycling, the order of triggered triggers should be decided by node order, so that it's manipulatable easily.
        // In all other modes, order is not important, so we don't sort at all for better performance
        // Since the index is unnecessary on non-cycle modes, we'll create a List<T> directly, instead of going through Indexed<T> to then re-allocate it into List<T>.
        // TODO: maybe rewrite into storing the indexes separately, in a stack-allocated buffer??
        List<Trigger>? into = null;
        List<Trigger>? intoElse = null;
        
        List<Indexed<Trigger>>? intoWithIndexes = null;
        List<Indexed<Trigger>>? intoElseWithIndexes = null;
        if (ActivationMode is ActivationModes.Cycle or ActivationModes.CycleCorrect or ActivationModes.AllOrdered || NeedsNodeIndexes) {
            intoWithIndexes = [];
            intoElseWithIndexes = [];
        } else {
            into = [];
            intoElse = [];
        }
        var nodes = Nodes;
        var maxNodeId = 0;

        foreach (Trigger entity in Scene.Tracker.GetEntities<Trigger>()) {
            var ePos = entity.Position;

            switch (entity.Collider)
            {
                case Hitbox eCol:
                {
                    // Fast path for the 99.99% of triggers that use Hitbox colliders
                    var eRight = ePos.X + eCol.Width;
                    var eBottom = ePos.Y + eCol.Height;

                    for (int i = 0; i < nodes.Length; i++) {
                        Vector2 node = nodes[i];
                        if (node.X < eRight
                            && node.X > ePos.X
                            && node.Y < eBottom
                            && node.Y > ePos.Y) {
                            maxNodeId = int.Max(maxNodeId, i);
                            Add(entity, i, into, intoElse, intoWithIndexes, intoElseWithIndexes);
                            break;
                        }
                    }

                    break;
                }
                case {} otherCollider: {
                    for (int i = 0; i < nodes.Length; i++) {
                        Vector2 node = nodes[i];
                        if (otherCollider.Collide(node)) {
                            maxNodeId = int.Max(maxNodeId, i);
                            Add(entity, i, into, intoElse, intoWithIndexes, intoElseWithIndexes);
                            break;
                        }
                    }

                    break;
                }
            }
        }

        // If we have kept track of indexes, then we need to sort
        if (intoWithIndexes is { }) {
            if (ActivationMode is ActivationModes.Cycle) {
                // backwards compat: old 'Cycle' mode sorted in the wrong order:
                intoWithIndexes.Sort((p1, p2) => p2.Index - p1.Index);
                intoElseWithIndexes!.Sort((p1, p2) => p2.Index - p1.Index);
            } else {
                intoWithIndexes.Sort((p1, p2) => p1.Index - p2.Index);
                intoElseWithIndexes!.Sort((p1, p2) => p1.Index - p2.Index);
            }
            

            if (NeedsNodeIndexes) {
                ToActivatePerNode = new List<Trigger>?[maxNodeId + 1];
                ToActivateElsePerNode = new List<Trigger>?[maxNodeId + 1];
                foreach (var item in intoWithIndexes) {
                    ToActivatePerNode[item.Index] ??= new(1);
                    ToActivatePerNode[item.Index]!.Add(item.Value);
                }
                foreach (var item in intoElseWithIndexes) {
                    ToActivateElsePerNode[item.Index] ??= new(1);
                    ToActivateElsePerNode[item.Index]!.Add(item.Value);
                }
            }

            // Now, convert our list to just a List<T>.
            // Done manually for performance and less allocations.
            into = new(intoWithIndexes.Count);
            intoElse = new(intoElseWithIndexes.Count);
            foreach (var item in intoWithIndexes) {
                into.Add(item.Value);
            }
            foreach (var item in intoElseWithIndexes) {
                intoElse.Add(item.Value);
            }
        }

        return (into!, intoElse!);
    }
}
