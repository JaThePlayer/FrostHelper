using FrostHelper.Helpers;

namespace FrostHelper;

[CustomEntity("CCH/DirectionalPuffer",
              "CCH/PinkPuffer",
              "FrostHelper/DirectionalPuffer")]
internal sealed class DirectionalPuffer : Puffer {
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

    [OnUnload]
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

    private readonly ExplodeDirection _direction;
    private readonly bool _static;
    private readonly Recovery _recovery;
    private readonly float _respawnTime;
    private readonly bool _noRespawn;
    private readonly bool _killOnJump;
    private readonly bool _killOnLaunch;
    private readonly Color _eyeColor;
    private readonly Color _explosionRangeIndicatorColor;

    public DirectionalPuffer(EntityData data, Vector2 offset) : base(data, offset) {
        LoadIfNeeded();

        // replace the sprite with a custom one
        Remove(Get<Sprite>());
        sprite = CustomSpriteHelper.CreateCustomSprite("pufferFish", data.Attr("directory", "objects/puffer/"));
        
        Add(sprite);
        sprite.Play("idle", false, false);
        sprite.SetColor(data.GetColor("color", "ffffff"));

        // Backwards compat
        if (!data.Has("recovery")) {
            var dashRecovery = data.Name == "CCH/PinkPuffer" ? 2 : data.Int("dashRecovery", 1);
            if (dashRecovery <= 1) {
                dashRecovery = Recovery.RecoveryIsARefill;
            }
            _recovery = new Recovery(dashRecovery, Recovery.RecoveryIsARefill, Recovery.RecoveryIsIgnored);
        } else {
            _recovery = data.Parse("recovery", Recovery.DefaultRefill);
        }

        if (_recovery.ShowPostcardIfNeeded("Directional Puffer"))
            return;

        _direction = data.Enum("explodeDirection", ExplodeDirection.Both);

        _static = data.Bool("static", false);
        _noRespawn = data.Bool("noRespawn", false);
        _respawnTime = data.Float("respawnTime", 2.5f);
        _killOnJump = data.Bool("killOnJump", false);
        _killOnLaunch = data.Bool("killOnLaunch", false);
        _eyeColor = data.GetColor("eyeColor", "000000");
        _explosionRangeIndicatorColor = data.GetColor("explosionRangeIndicatorColor", "ffffff");


        MakeStaticIfNeeded();
    }

    private bool DirectionCheck(Player player) {
        _beforeExplodePlayerStats = _recovery.SavePlayerData(player);
        if (!_recovery.CanUse(_beforeExplodePlayerStats))
            return true;
        
        return _direction switch {
            ExplodeDirection.Left => player.Position.X > Position.X,
            ExplodeDirection.Right => player.Position.X < Position.X,
            ExplodeDirection.Both => false,
            ExplodeDirection.None => true,
            _ => throw new NotImplementedException(),
        };
    }

    private static int GetRenderStartIndex(int orig, Puffer puffer) {
        if (puffer is DirectionalPuffer dirPuff) {
            return dirPuff._direction switch {
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

    private static int GetRenderEndIndex(int orig, Puffer puffer) {
        if (puffer is DirectionalPuffer dirPuff) {
            return dirPuff._direction switch {
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

    private static void SetRespawnTime(Puffer puffer) {
        if (puffer is DirectionalPuffer dirPuff) {
            puffer.goneTimer = dirPuff._respawnTime;
        }
    }

    private static void RemoveSelfIfNoRespawn(Puffer puffer) {
        if (puffer is DirectionalPuffer { _noRespawn: true }) {
            puffer.RemoveSelf();
        }
    }

    /// <summary>Implement the RespawnTime and NoRespawn properties</summary>
    private static void Puffer_GotoGone(ILContext il) {
        ILCursor cursor = new(il);

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitCall(RemoveSelfIfNoRespawn);


        while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchStfld<Puffer>("goneTimer"))) {
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitCall(SetRespawnTime);
        }
    }

    /// <summary>Make sure that you can't get boosted in the opposite direction by moving into the puffer</summary>
    private static void IL_OnPlayer(ILContext il) {
        ILCursor cursor = new(il);

        // find the branch that skips over Puffer.Explode
        if (cursor.TryGotoNext(MoveType.After, instr => instr.OpCode.Code == Code.Ble_Un_S)) {
            ILLabel label = (cursor.Prev.Operand as ILLabel)!;

            // emit new branch
            cursor.Emit(OpCodes.Ldarg_0); // this
            cursor.Emit(OpCodes.Ldarg_1); // player
            cursor.EmitDelegate(ShouldSkipPufferExplode);
            cursor.Emit(OpCodes.Brtrue, label.Target!);
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
            cursor.Emit(OpCodes.Brfalse, returnLabel.Target!);

            // restore the stack
            cursor.Emit(OpCodes.Ldarg_1); // player
            cursor.Emit(OpCodes.Ldloc, fromYLocal); // this.Top
        }
    }

    private static bool ShouldSkipPufferExplode(Puffer p, Player player) {
        return (p is DirectionalPuffer dirPuff) && dirPuff.DirectionCheck(player);
    }

    private static bool HandleCustomBounceEvents(Player player, Puffer self) {
        if (self is DirectionalPuffer { _killOnJump: true } && !player.Dead) {
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
    private static void IL_Render(ILContext il) {
        ILCursor cursor = new(il);

        // change min i
        while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcI4(0) &&
                                                           instr.Next.MatchStloc(6))) {
            cursor.Emit(OpCodes.Ldarg_0); // this
            cursor.EmitDelegate(GetRenderStartIndex);
        }

        // change max i
        while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcI4(28))) {
            cursor.Emit(OpCodes.Ldarg_0); // this
            cursor.EmitDelegate(GetRenderEndIndex);
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

    private static Color GetEyeColor(Color prev, Puffer self) {
        return self is DirectionalPuffer dirPuf ? dirPuf._eyeColor : prev;
    }

    private static Color GetExplosionRangeIndicatorColor(Color prev, Puffer self) {
        return self is DirectionalPuffer dirPuf ? dirPuf._explosionRangeIndicatorColor : prev;
    }

    private static void Puffer_Explode(ILContext il) {
        ILCursor cursor = new(il);
        
        if (cursor.SeekVirtFunctionCall(typeof(Player), "ExplodeLaunch")) {
            cursor.Index++; // skipping over the Pop opcode

            cursor.Emit(OpCodes.Ldarg_0); // this
            cursor.Emit(OpCodes.Ldloc_1); // player
            cursor.EmitCall(HandleCustomExplodeEvents);
        }
    }

    /// <summary>Make the directional puffer not explode or become alerted if the player is behind the directional puffer</summary>
    private static bool ApplyDirectionalCheck(On.Celeste.Puffer.orig_ProximityExplodeCheck orig, Puffer self) {
        if (self is DirectionalPuffer dirPuffer) {
            Player player = self.Scene.Tracker.GetNearestEntity<Player>(self.Position);

            if (player is not null && dirPuffer.DirectionCheck(player)) {
                return false;
            }
        }
        return orig(self);
    }

    private Recovery.SavedPlayerData _beforeExplodePlayerStats;
    
    private static void HandleCustomExplodeEvents(Puffer puffer, Player player) {
        if (puffer is not DirectionalPuffer dirPuffer) 
            return;

        if (dirPuffer._killOnLaunch && !player.Dead) {
            player.Die(Calc.AngleToVector(Calc.Angle(dirPuffer.Position, player.Position), 1f));
            return;
        }

        dirPuffer._recovery.Recover(player, dirPuffer._beforeExplodePlayerStats);
    }

    // based on Maddie's Helping Hand's Static Puffers
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
        if (idleSine.Entity is DirectionalPuffer { _static: true }) {
            // unrandomize the initial pufferfish position.
            idleSine.Reset();
        }
    }

    private void MakeStaticIfNeeded() {
        if (!_static) {
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
