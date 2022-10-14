namespace FrostHelper;

internal static class ChangeDashSpeedOnce {
    public static float? NextDashSpeed;
    public static float? NextSuperJumpSpeed;

    public static void ChangeNextDashSpeed(float speed) {
        LoadIfNeeded();
        NextDashSpeed = speed;
    }

    public static void ChangeNextSuperJumpSpeed(float speed) {
        LoadIfNeeded();
        NextSuperJumpSpeed = speed;
    }

    private static bool _hooksLoaded;

    [HookPreload]
    public static void LoadIfNeeded() {
        if (_hooksLoaded)
            return;
        _hooksLoaded = true;

        FrostModule.RegisterILHook(EasierILHook.HookCoroutine("Celeste.Player", "DashCoroutine", DashCoroutinePatch));
        FrostModule.RegisterILHook(EasierILHook.Hook<Player>("DashEnd", DashEndPatch));
        FrostModule.RegisterILHook(EasierILHook.Hook<Player>("SuperJump", SuperJumpPatch));
        FrostModule.RegisterILHook(EasierILHook.Hook<Player>("orig_Die", DiePatch));
    }

    // Reset the dash speed when the player dies
    public static void DiePatch(ILContext context) {
        var cursor = new ILCursor(context);

        /*
        foreach (var item in cursor.Instrs) {
            try {
                Console.WriteLine(item.ToString());
            } catch { }
        }*/

        if (cursor.SeekVirtFunctionCall<Player>("Stop")) // this func is called right after the !Dead check
            cursor.EmitCall(Reset);
    }

    public static void DashEndPatch(ILContext context) {
        var cursor = new ILCursor(context);
        cursor.EmitCall(Reset);
    }

    public static void DashCoroutinePatch(ILContext context) {
        var cursor = new ILCursor(context);

        while (cursor.SeekLoadFloat(240f)) {
            cursor.EmitCall(GetDashSpeed);
        }
    }

    public static void SuperJumpPatch(ILContext context) {
        var cursor = new ILCursor(context);

        while (cursor.SeekLoadFloat(260f)) {
            cursor.EmitCall(GetSuperJumpSpeed);
        }
    }

    public static void Reset() {
        NextDashSpeed = null;
        NextSuperJumpSpeed = null;
    }

    public static float GetDashSpeed(float orig) {
        if (NextDashSpeed is not null) {
            orig = NextDashSpeed.Value;
            NextDashSpeed = null;

            return orig;
        }

        return orig;
    }

    public static float GetSuperJumpSpeed(float orig) {
        if (NextSuperJumpSpeed is not null) {
            orig = NextSuperJumpSpeed.Value;
            NextSuperJumpSpeed = null;

            return orig;
        }

        return orig;
    }
}
