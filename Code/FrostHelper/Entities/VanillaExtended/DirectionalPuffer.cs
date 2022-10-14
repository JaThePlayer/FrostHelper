namespace FrostHelper;

[CustomEntity("CCH/DirectionalPuffer",
              "CCH/PinkPuffer",
              "FrostHelper/DirectionalPuffer")]
public class DirectionalPuffer : Puffer {
    private static bool _hooksLoaded;

    [HookPreload]
    public static void LoadIfNeeded() {
        if (_hooksLoaded)
            return;
        _hooksLoaded = true;

        IL.Celeste.Puffer.OnPlayer += IL_OnPlayer;
        IL.Celeste.Puffer.Render += IL_Render;
        On.Celeste.Puffer.ProximityExplodeCheck += ApplyDirectionalCheck;
        IL.Celeste.Puffer.Explode += Puffer_Explode;
        IL.Celeste.Puffer.GotoGone += Puffer_GotoGone;

        // for static puffers:
        IL.Celeste.Puffer.ctor_Vector2_bool += IL_Puffer_Constructor;
    }

    public static void Unload() {
        if (!_hooksLoaded)
            return;
        _hooksLoaded = false;

        IL.Celeste.Puffer.OnPlayer -= IL_OnPlayer;
        IL.Celeste.Puffer.Render -= IL_Render;
        On.Celeste.Puffer.ProximityExplodeCheck -= ApplyDirectionalCheck;
        IL.Celeste.Puffer.Explode -= Puffer_Explode;
        IL.Celeste.Puffer.GotoGone -= Puffer_GotoGone;

        // for static puffers:
        IL.Celeste.Puffer.ctor_Vector2_bool -= IL_Puffer_Constructor;
    }

    public enum ExplodeDirection {
        Left,
        Right,
        Both,
        None,
    }

    public ExplodeDirection Direction;
    public bool Static;
    public int DashRecovery;
    public new float RespawnTime;
    public bool NoRespawn;
    public bool KillOnJump;
    public bool KillOnLaunch;
    public Color EyeColor;
    public Color ExplosionRangeIndicatorColor;

    public DirectionalPuffer(EntityData data, Vector2 offset) : base(data, offset) {
        LoadIfNeeded();

        // replace the sprite with a custom one
        Remove(Get<Sprite>());
        var sprite = CustomSpriteHelper.CreateCustomSprite("pufferFish", data.Attr("directory", "objects/puffer/"));
        Puffer_sprite.SetValue(this, sprite);
        Add(sprite);
        sprite.Play("idle", false, false);
        sprite.SetColor(data.GetColor("color", "ffffff"));

        DashRecovery = data.Name == "CCH/PinkPuffer" ? 2 : data.Int("dashRecovery", 1);

        Direction = data.Enum("explodeDirection", ExplodeDirection.Both);

        Static = data.Bool("static", false);
        NoRespawn = data.Bool("noRespawn", false);
        RespawnTime = data.Float("respawnTime", 2.5f);
        KillOnJump = data.Bool("killOnJump", false);
        KillOnLaunch = data.Bool("killOnLaunch", false);
        EyeColor = data.GetColor("eyeColor", "000000");
        ExplosionRangeIndicatorColor = data.GetColor("explosionRangeIndicatorColor", "ffffff");


        MakeStaticIfNeeded();
    }

    public static bool IsRightPuffer(Puffer p) {
        if (p is DirectionalPuffer puffer) {
            return puffer.Direction == ExplodeDirection.Right;
        }

        return false;
    }

    public static bool IsLeftPuffer(Puffer p) {
        if (p is DirectionalPuffer puffer) {
            return puffer.Direction == ExplodeDirection.Left;
        }

        return false;
    }

    public bool DirectionCheck(Player player) {
        return Direction switch {
            ExplodeDirection.Left => player.Position.X > Position.X,
            ExplodeDirection.Right => player.Position.X < Position.X,
            ExplodeDirection.Both => false,
            ExplodeDirection.None => true,
            _ => throw new NotImplementedException(),
        };
    }

    private static int getRenderStartIndex(int orig, Puffer puffer) {
        if (puffer is DirectionalPuffer dirPuff) {
            return dirPuff.Direction switch {
                ExplodeDirection.Left => 14,
                ExplodeDirection.Right => orig,
                ExplodeDirection.Both => orig,
                ExplodeDirection.None => int.MaxValue,
                _ => throw new NotImplementedException(),
            };
        } else {
            return orig;
        }
    }

    private static int getRenderEndIndex(int orig, Puffer puffer) {
        if (puffer is DirectionalPuffer dirPuff) {
            return dirPuff.Direction switch {
                ExplodeDirection.Left => orig,
                ExplodeDirection.Right => 14,
                ExplodeDirection.Both => orig,
                ExplodeDirection.None => int.MinValue,
                _ => throw new NotImplementedException(),
            };
        } else {
            return orig;
        }
    }

    private static void setRespawnTime(Puffer puffer) {
        if (puffer is DirectionalPuffer dirPuff) {
            puffer.goneTimer = dirPuff.RespawnTime;
        }
    }

    private static void removeSelfIfNoRespawn(Puffer puffer) {
        if (puffer is DirectionalPuffer { NoRespawn: true }) {
            puffer.RemoveSelf();
        }
    }

    /// <summary>Implement the RespawnTime and NoRespawn properties</summary>
    private static void Puffer_GotoGone(ILContext il) {
        ILCursor cursor = new(il);

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitCall(removeSelfIfNoRespawn);


        while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchStfld<Puffer>("goneTimer"))) {
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitCall(setRespawnTime);
        }
    }

    /// <summary>Make sure that you can't get boosted in the opposite direction by moving into the puffer</summary>
    internal static void IL_OnPlayer(ILContext il) {
        ILCursor cursor = new(il);

        // find the branch that skips over Puffer.Explode
        if (cursor.TryGotoNext(MoveType.After, instr => instr.OpCode.Code == Code.Ble_Un_S)) {
            ILLabel label = (cursor.Prev.Operand as ILLabel)!;

            // emit new branch
            cursor.Emit(OpCodes.Ldarg_0); // this
            cursor.Emit(OpCodes.Ldarg_1); // player
            cursor.EmitDelegate<Func<Puffer, Player, bool>>((p, player) => (p is DirectionalPuffer dirPuff) && dirPuff.DirectionCheck(player));
            cursor.Emit(OpCodes.Brtrue, label.Target);
        }

        // add a label to the end of the function so that it's easier to branch to it later
        cursor.Index = cursor.Instrs.Count - 1;
        var returnLabel = cursor.DefineLabel();
        cursor.MarkLabel(returnLabel);
        cursor.Index = 0;

        if (cursor.SeekVirtFunctionCall(typeof(Player), "Bounce")) {
            cursor.Index--;

            VariableDefinition fromYLocal = new VariableDefinition(il.Import(typeof(float)));
            il.Body.Variables.Add(fromYLocal);

            cursor.Emit(OpCodes.Stloc, fromYLocal); // store this.Top for later

            cursor.Emit(OpCodes.Ldarg_0); // this
            cursor.EmitCall(HandleCustomBounceEvents);

            // if the func returned false, early return
            cursor.Emit(OpCodes.Brfalse, returnLabel.Target);

            // restore the stack
            cursor.Emit(OpCodes.Ldarg_1); // player
            cursor.Emit(OpCodes.Ldloc, fromYLocal); // this.Top
        }
    }

    internal static bool HandleCustomBounceEvents(Player player, Puffer self) {
        if (self is DirectionalPuffer { KillOnJump: true } && !player.Dead) {
            player.Die(-Vector2.UnitY);
            self.inflateWiggler.Start();
            self.bounceWiggler.Start();
            return false;
        }

        return true;
    }

    /// <summary>
    /// - Only render part of the puffer's explosion radius indicator
    /// - Implement the eye color property
    /// </summary>
    internal static void IL_Render(ILContext il) {
        ILCursor cursor = new(il);

        // change min i
        while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcI4(0) &&
                                                           instr.Next.MatchStloc(6))) {
            cursor.Emit(OpCodes.Ldarg_0); // this
            cursor.EmitDelegate(getRenderStartIndex);
        }

        // change max i
        while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcI4(28))) {
            cursor.Emit(OpCodes.Ldarg_0); // this
            cursor.EmitDelegate(getRenderEndIndex);
        }

        cursor.Index = 0;

        // implement explosion range indicator changing
        while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCall<Color>("get_White"))) {
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitCall(GetExplosionRangeIndicatorColor);
        }

        // implement eye color changing
        if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCall<Color>("get_Black"))) {
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitCall(GetEyeColor);
        }
    }

    public static Color GetEyeColor(Color prev, Puffer self) {
        return self is DirectionalPuffer dirPuf ? dirPuf.EyeColor : prev;
    }

    public static Color GetExplosionRangeIndicatorColor(Color prev, Puffer self) {
        return self is DirectionalPuffer dirPuf ? dirPuf.ExplosionRangeIndicatorColor : prev;
    }

    internal static void Puffer_Explode(ILContext il) {
        ILCursor cursor = new(il);
        while (cursor.SeekVirtFunctionCall(typeof(Player), "ExplodeLaunch")) {
            cursor.Index++; // skipping over the Pop opcode

            cursor.Emit(OpCodes.Ldarg_0); // this
            cursor.Emit(OpCodes.Ldloc_1); // player
            cursor.EmitCall(HandleCustomExplodeEvents);
        }
    }

    /// <summary>Make the directional puffer not explode or become alerted if the player is behind the directional puffer</summary>
    internal static bool ApplyDirectionalCheck(On.Celeste.Puffer.orig_ProximityExplodeCheck orig, Puffer self) {
        if (self is DirectionalPuffer dirPuffer) {
            Player player = self.Scene.Tracker.GetNearestEntity<Player>(self.Position);

            if (player is not null && dirPuffer.DirectionCheck(player)) {
                return false;
            }
        }
        return orig(self);
    }

    private static void HandleCustomExplodeEvents(Puffer puffer, Player player) {
        if (puffer is not DirectionalPuffer dirPuffer) 
            return;

        if (dirPuffer.KillOnLaunch && !player.Dead) {
            player.Die(Calc.AngleToVector(Calc.Angle(dirPuffer.Position, player.Position), 1f));
            return;
        }

        if (dirPuffer.DashRecovery > 1) {
            player.Dashes = dirPuffer.DashRecovery;
        }
    }

    private static FieldInfo Puffer_sprite = typeof(Puffer).GetField("sprite", BindingFlags.NonPublic | BindingFlags.Instance);


    // based on Max's Helping Hand's Static Puffers
    #region StaticPuffer
    private static void IL_Puffer_Constructor(ILContext il) {
        ILCursor cursor = new(il);

        while (cursor.SeekVirtFunctionCall(typeof(SineWave), "Randomize")) {
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.LoadField<Puffer>("idleSine");
            cursor.EmitCall<DirectionalPuffer>(nameof(ResetSineIfStatic));
        }
    }

    public static void ResetSineIfStatic(SineWave idleSine) {
        if (idleSine.Entity is DirectionalPuffer { Static: true }) {
            // unrandomize the initial pufferfish position.
            idleSine.Reset();
        }
    }

    public void MakeStaticIfNeeded() {
        if (!Static) {
            return;
        }

        // remove the sine wave component so that it isn't updated.
        Get<SineWave>()?.RemoveSelf();

        // give the puffer a different depth compared to the player to eliminate frame-precise inconsistencies.
        Depth = -1;

        // offset the horizontal position by a tiny bit.
        // Vanilla puffers have a non-integer position (due to the randomized offset), making it impossible to be boosted downwards,
        // so we want to do the same.
        Position.X += 0.0001f;
    }
    #endregion
}
