namespace FrostHelper.EXPERIMENTAL;

// ISSUE: entities dissapear during screen transitions

internal static class LayerHelper {
    internal static LayerTracker? _temporaryCurrentTracker;
    private static EntityData? _nextEntityData;
    private static Level? _nextLevel;

    public static LayerTracker GetLayerTracker(this Scene level) {
        /*
        const string layerTrackerFieldName = "fh.layerTracker";
        var data = DynamicData.For(level);
        if (data.TryGet<LayerTracker>(layerTrackerFieldName, out var tracker)) {
            return tracker;
        }
        tracker = new LayerTracker();
        data.Set(layerTrackerFieldName, tracker);
        return tracker;*/
        var tracker = _temporaryCurrentTracker ?? level.Tracker.GetEntity<LayerTracker>();
        if (tracker is null) {
            tracker = _temporaryCurrentTracker = new();
            level.Add(tracker);
        }

        return tracker;
    }

    public static List<Entity> GetEntitiesOnLayer(int layer) => GetLayerTracker(FrostModule.GetCurrentLevel()).GetEntitiesOnLayer(layer);

    /*
    public static int GetLayer(this Entity self) {
        var tracker = GetLayerTracker(self.Scene);

        return tracker.Get
    }*/

    [OnLoad]
    public static void Load() {
        Everest.Events.Level.OnLoadEntity += Level_OnLoadEntity;
        On.Monocle.Entity.ctor_Vector2 += Entity_ctor_Vector2;
        On.Celeste.Player.OnTransition += Player_OnTransition;
    }

    [OnUnload]
    public static void Unload() {
        Everest.Events.Level.OnLoadEntity -= Level_OnLoadEntity;
        On.Monocle.Entity.ctor_Vector2 -= Entity_ctor_Vector2;
        On.Celeste.Player.OnTransition -= Player_OnTransition;
    }

    // transition listeners don't work lol
    private static void Player_OnTransition(On.Celeste.Player.orig_OnTransition orig, Player self) {
        foreach (LayerTracker item in self.Scene.Tracker.GetEntities<LayerTracker>()) {
            item.OnTransitionEnd();
        }
        orig(self);
    }

    private static void Entity_ctor_Vector2(On.Monocle.Entity.orig_ctor_Vector2 orig, Entity self, Vector2 position) {
        orig(self, position);

        if (self is not LayerTracker && _nextEntityData is not null && _nextLevel is not null) {
            var layerTracker = _nextLevel.GetLayerTracker();

            layerTracker.Track(self, _nextEntityData);

            _nextEntityData = null;
            _nextLevel = null;
        }
    }

    private static bool Level_OnLoadEntity(Level level, LevelData levelData, Vector2 offset, EntityData entityData) {

        _nextEntityData = entityData;
        _nextLevel = level;
        return false;
    }


}

[Tracked]
public class LayerTracker : Entity {
    private Dictionary<int, List<Entity>> Layers = new() { [0] = new() };
    //private Dictionary<Entity, int> EntityToLayer

    public LayerTracker() {
        Tag |= Tags.Persistent;
        //Tag |= Tags.TransitionUpdate;
    }

    public void OnTransitionEnd() {
        foreach (var item in Layers) {
            //Console.WriteLine();
            //Console.WriteLine(item.Value.Count);
            item.Value.RemoveAll(e => e.Scene is null);
            //Console.WriteLine(item.Value.Count);
        }
    }

    public void Track(Entity entity, EntityData from) => Track(entity, from.Int("editorLayer", 0));

    public void Track(Entity entity, int layer) {
        if (Layers.TryGetValue(layer, out List<Entity> layerEntities)) {
            layerEntities.Add(entity);
        } else {
            Layers[layer] = new() { entity };
        }

        if (layer != 0)
            Layers[0].Add(entity);
    }

    public List<Entity> GetEntitiesOnLayer(int layer) {
        return Layers.TryGetValue(layer, out var entities) ? entities : new();
    }

    public override void Awake(Scene scene) {
        base.Awake(scene);

        LayerHelper._temporaryCurrentTracker = null;
    }
}
