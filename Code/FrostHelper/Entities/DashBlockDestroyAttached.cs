namespace FrostHelper;

/// <summary>
/// A dash block that destoys attached entities when broken.
/// </summary>
[CustomEntity("FrostHelper/DashBlockDestroyAttached")]
[Tracked(false)]
[TrackedAs(typeof(DashBlock))]
public class DashBlockDestroyAttached : DashBlock {
    #region Hooks
    private static bool _hooksLoaded;

    public static void LoadHooksIfNeeded() {
        if (_hooksLoaded) {
            return;
        }

        _hooksLoaded = true;
        On.Celeste.DashBlock.RemoveAndFlagAsGone += DashBlock_RemoveAndFlagAsGone;
    }

    [OnUnload]
    public static void UnloadHooks() {
        _hooksLoaded = false;
        On.Celeste.DashBlock.RemoveAndFlagAsGone -= DashBlock_RemoveAndFlagAsGone;
    }
    
    /// <summary>
    /// Make our dash blocks get added to SoftDoNotLoad instead of DoNotLoad, to make it possible to remove attached entities.
    /// Also add a DestroyStaticMovers call.
    /// </summary>
    private static void DashBlock_RemoveAndFlagAsGone(On.Celeste.DashBlock.orig_RemoveAndFlagAsGone orig, DashBlock self) {
        orig(self);

        if (self is DashBlockDestroyAttached db) {
            self.SceneAs<Level>().Session.DoNotLoad.Remove(self.id);
            FrostModule.Session.SoftDoNotLoad.Add(self.id);

            db.DestroyMovers();
        }
    }
    #endregion

    public DashBlockDestroyAttached(EntityData data, Vector2 offset, EntityID id) : base(data, offset, id) {
        LoadHooksIfNeeded();
    }

    public override void Awake(Scene scene) {
        base.Awake(scene);

        if (permanent && FrostModule.Session.SoftDoNotLoad.Contains(id)) {
            DestroyMovers();
            RemoveSelf();
        }
    }

    public override void Removed(Scene scene) {
        base.Removed(scene);

        DestroyMovers();
    }

    public void DestroyMovers() {
        DestroyStaticMovers();
    }
}
