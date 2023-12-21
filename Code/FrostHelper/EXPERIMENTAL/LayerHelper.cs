namespace FrostHelper.EXPERIMENTAL;

internal static class LayerHelper {
    private static EntityData? _nextEntityData;
    private static Level? _nextLevel;
    private static bool _hooksLoaded;

    public static LayerTracker GetLayerTracker(this Scene scene) {
        return ControllerHelper<LayerTracker>.AddToSceneIfNeeded(scene);
    }

    public static List<Entity> GetEntitiesOnLayer(int layer) => GetLayerTracker(FrostModule.GetCurrentLevel()).GetEntitiesOnLayer(layer);

    //[OnLoad]
    public static void Load() {
        if (_hooksLoaded)
            return;

        _hooksLoaded = true;
        Everest.Events.Level.OnLoadEntity += Level_OnLoadEntity;
        On.Monocle.Entity.ctor_Vector2 += Entity_ctor_Vector2;
        On.Celeste.Player.OnTransition += Player_OnTransition;
    }

    //[OnUnload]
    public static void Unload() {
        if (!_hooksLoaded)
            return;

        _hooksLoaded = false;
        Everest.Events.Level.OnLoadEntity -= Level_OnLoadEntity;
        On.Monocle.Entity.ctor_Vector2 -= Entity_ctor_Vector2;
        On.Celeste.Player.OnTransition -= Player_OnTransition;
    }

    // transition listeners don't work lol
    private static void Player_OnTransition(On.Celeste.Player.orig_OnTransition orig, Player self) {
        foreach (LayerTracker item in self.Scene.Tracker.SafeGetEntities<LayerTracker>()) {
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
        LayerHelper.Load();
    }

    public void OnTransitionEnd() {
        foreach (var item in Layers) {
            //Console.WriteLine();
            //Console.WriteLine(item.Value.Count);
            item.Value.RemoveAll(e => e.Scene is null);
            //Console.WriteLine(item.Value.Count);
        }
    }

    public void Track(Entity entity, EntityData from) => Track(entity, from.Int("_editorLayer", 0));

    public void Track(Entity entity, int layer) {
        if (Layers.TryGetValue(layer, out var layerEntities)) {
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
}
