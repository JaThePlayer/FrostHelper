namespace FrostHelper.Triggers.Activator;

[Tracked]
[CustomEntity("FrostHelper/OnJumpActivator")]
internal sealed class OnJumpActivator : BaseActivator {
    #region Hooks

    private static bool _hooksLoaded;
    
    [HookPreload]
    private static void LoadIfNeeded() {
        if (_hooksLoaded)
            return;
        _hooksLoaded = true;

        FrostModule.RegisterILHook(EasierILHook.CreatePrefixHook(typeof(Player), nameof(Player.Jump), OnJump, JumpTypes.Normal));
        FrostModule.RegisterILHook(EasierILHook.CreatePrefixHook(typeof(Player), nameof(Player.ClimbJump), OnJump, JumpTypes.ClimbJump));
        FrostModule.RegisterILHook(EasierILHook.CreatePrefixHook(typeof(Player), nameof(Player.SuperJump), OnJump, JumpTypes.SuperJump));
        FrostModule.RegisterILHook(EasierILHook.CreatePrefixHook(typeof(Player), nameof(Player.HiccupJump), OnJump, JumpTypes.HiccupJump));
        FrostModule.RegisterILHook(EasierILHook.CreatePrefixHook(typeof(Player), nameof(Player.WallJump), OnJump, JumpTypes.WallJump));
        FrostModule.RegisterILHook(EasierILHook.CreatePrefixHook(typeof(Player), nameof(Player.SuperWallJump), OnJump, JumpTypes.SuperWallJump));
    }

    private static void OnJump(Player self, JumpTypes type) {
        foreach (OnJumpActivator activator in self.Scene.Tracker.SafeGetEntities<OnJumpActivator>()) {
            if (activator._ignoreNextJump) {
                activator._ignoreNextJump = false;
                continue;
            }
            
            if (activator._jumpType.HasFlag(type)) {
                activator.ActivateAll(self);
            }
            
            // ClimbJump calls Jump, we want to ignore that Jump call
            if (type == JumpTypes.ClimbJump)
                activator._ignoreNextJump = true;
        }
    }

    #endregion

    [Flags]
    private enum JumpTypes {
        None = 0,
        Normal = 1,
        ClimbJump = 2,
        // 4 is used as a const in jump methods, just in case we will not use it to not mess up badly coded ilhooks in other mods
        SuperJump = 8,
        HiccupJump = 16,
        WallJump = 32,
        SuperWallJump = 64,
    }
    
    private readonly JumpTypes _jumpType;

    private bool _ignoreNextJump;
    
    public OnJumpActivator(EntityData data, Vector2 offset) : base(data, offset) {
        LoadIfNeeded();
        
        _jumpType = data.FlagEnumFromMultipleBools(
            (JumpTypes.Normal, "normalJump"),
            (JumpTypes.ClimbJump, "climbJump"),
            (JumpTypes.SuperJump, "superJump"),
            (JumpTypes.HiccupJump, "hiccupJump"),
            (JumpTypes.WallJump, "wallJump"),
            (JumpTypes.SuperWallJump, "superWallJump")
        );
    }
}