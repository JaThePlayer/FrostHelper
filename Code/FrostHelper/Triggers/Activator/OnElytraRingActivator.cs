namespace FrostHelper.Triggers.Activator;

[CustomEntity("FrostHelper/OnElytraRingActivator")]
[Tracked]
internal sealed class OnElytraRingActivator : BaseActivator {
    #region Hooks

    private static bool _hooksLoaded = false;

    internal static void LoadIfNeeded() {
        if (_hooksLoaded) return;
        _hooksLoaded = true;

        var boostRingType = TypeHelper.EntityNameToType("CommunalHelper/ElytraBoostRing");
        if (boostRingType.BaseType is not { } ringType) {
            throw new Exception($"Couldn't find base type of 'CommunalHelper/ElytraBoostRing' (c#: {boostRingType})");
        }
        
        // public virtual void OnPlayerTraversal(Player player, int sign, bool shake = true)
        var onPlayerTraversalMethod = ringType.GetMethod("OnPlayerTraversal") ?? throw new Exception($"Couldn't find {ringType}.OnPlayerTraversal");

        FrostModule.RegisterILHook(new ILHook(onPlayerTraversalMethod, OnPlayerTraversalHook));
    }

    private static void OnPlayerTraversalHook(ILContext il) {
        var cursor = new ILCursor(il);
        
        cursor.Emit(OpCodes.Ldarg_0); // this ring
        cursor.Emit(OpCodes.Ldarg_1); // player
        cursor.EmitCall(OnPlayerTraversalPrefix);
    }
    
    private static void OnPlayerTraversalPrefix(object ring, Player player) {
        foreach (OnElytraRingActivator activator in player.Scene.Tracker.SafeGetEntities<OnElytraRingActivator>()) {
            activator.OnElytraRing(ring, player);
        }
    }
    #endregion
    
    internal void OnElytraRing(object ring, Player player) {
        if (_types.Count == 0 || _types.Contains(ring.GetType()))
            ActivateAll(player);
    }

    private readonly HashSet<Type> _types;
    
    public OnElytraRingActivator(EntityData data, Vector2 offset) : base(data, offset) {
        LoadIfNeeded();

        _types = FrostModule.GetTypesAsHashSet(data.Attr("types", ""));
        
        Active = false;
        Collidable = false;
    }
}