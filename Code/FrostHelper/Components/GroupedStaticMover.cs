namespace FrostHelper;

[Tracked]
[TrackedAs(typeof(StaticMover))]
public class GroupedStaticMover : StaticMover {
    public int Group;

    public GroupedStaticMover(int group) {
        Group = group;
        OnAttach = (p) => AttachGroupableExt.TryGroupAttach(Entity, p);
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

        var group = baseMover.Group;
        var staticMovers = platform.GetValue<List<StaticMover>>("staticMovers");
        //foreach (Entity item in entity.Scene.Entities) {
        //    var mover = item.Get<GroupedStaticMover>();
        foreach (GroupedStaticMover mover in entity.Scene.Tracker.GetComponents<GroupedStaticMover>()) {
            if (mover is not null && mover.Group == group && mover.Platform is null) {
                mover.OnAttach = null;
                staticMovers.Add(mover);
                mover.Platform = platform;
            }
        }
            
    }
}
