namespace FrostHelper.Components;

/// <summary>
/// Allows registering a callback when any entity gets added to the scene.
/// </summary>
[Tracked]
internal sealed class AnyEntityAddedHook : Component {
    public readonly Action<Entity, Scene> OnEntityAdded;
    
    /// <summary>
    /// If true, the callback should also be called on all entities added to the scene before this component got added.
    /// </summary>
    public bool CatchUpToPreviouslyAdded { get; init; }
    
    private bool _awoken;

    public AnyEntityAddedHook(Action<Entity, Scene> onEntityAdded) : base(true, false) {
        OnEntityAdded = onEntityAdded;
        LoadHooks();
    }

    #region Hooks

    private static bool _hooksLoaded;
    
    public static void LoadHooks() {
        if (_hooksLoaded)
            return;
        _hooksLoaded = true;
        
        IL.Monocle.Entity.Added += EntityOnAdded;
    }
    
    [OnUnload]
    public static void UnloadHooks() {
        if (!_hooksLoaded)
            return;
        _hooksLoaded = false;
        
        IL.Monocle.Entity.Added -= EntityOnAdded;
    }

    private static void EntityOnAdded(ILContext il) {
        var cursor = new ILCursor(il);
        
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldarg_1);
        cursor.EmitCall(BeforeEntityAdded);
    }
    
    private static void BeforeEntityAdded(Entity entity, Scene scene) {
        foreach (AnyEntityAddedHook hook in scene.Tracker.SafeGetComponents<AnyEntityAddedHook>()) {
            hook.OnEntityAdded(entity, scene);
        }
    }

    public override void EntityAdded(Scene scene) {
        base.EntityAdded(scene);

        if (CatchUpToPreviouslyAdded) {
            foreach (var previouslyAdded in scene.Entities.entities) {
                OnEntityAdded(previouslyAdded, scene);
            }
        }
    }

    public override void Update() {
        if (_awoken)
            RemoveSelf();
    }

    #endregion
}
