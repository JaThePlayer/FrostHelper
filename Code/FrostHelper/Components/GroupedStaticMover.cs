namespace FrostHelper;

[Tracked]
[TrackedAs(typeof(StaticMover))]
public class GroupedStaticMover : StaticMover {
    public int Group;
    public bool CanBeLeader = true;

    public GroupedStaticMover(int group, bool canBeLeader) {
        CanBeLeader = canBeLeader;
        Group = group;
        OnAttach = CanBeLeader ? (p) => AttachGroupableExt.TryGroupAttach(Entity, p) : null;
    }

    public GroupedStaticMover SetOnAttach(Action<Celeste.Platform> callback) {
        if (callback is not null) {
            OnAttach += callback;
        }

        return this;
    }
}

public static class AttachGroupableExt {
    public static void TryGroupAttach(Entity entity, Celeste.Platform platform) {
        var baseMover = entity.Get<GroupedStaticMover>();
        if (baseMover == null || !baseMover.CanBeLeader || entity == platform) {
            return;
        }


        var group = baseMover.Group;
        var staticMovers = platform.GetValue<List<StaticMover>>("staticMovers");

        foreach (GroupedStaticMover mover in entity.Scene.Tracker.GetComponents<GroupedStaticMover>()) {
            if (mover is not null && mover.Group == group && mover.Platform is null) {
                mover.OnAttach = null;
                staticMovers.Add(mover);
                mover.Platform = platform;
            }
        }
            
    }
}
