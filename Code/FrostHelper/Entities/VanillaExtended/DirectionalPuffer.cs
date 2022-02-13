namespace FrostHelper;

[CustomEntity("CCH/DirectionalPuffer",
              "CCH/PinkPuffer",
              "FrostHelper/DirectionalPuffer")]
public class DirectionalPuffer : Puffer {
    [OnLoad]
    public static void Load() {
        IL.Celeste.Puffer.OnPlayer += IL_OnPlayer;
        IL.Celeste.Puffer.Render += IL_Render;
        On.Celeste.Puffer.ProximityExplodeCheck += ApplyDirectionalCheck;
        IL.Celeste.Puffer.Explode += Puffer_Explode;
        IL.Celeste.Puffer.GotoGone += Puffer_GotoGone;

        // for static puffers:
        IL.Celeste.Puffer.ctor_Vector2_bool += IL_Puffer_Constructor;
    }

    public static void Unload() {
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

    private ExplodeDirection direction;
    public bool Static;
    public int DashRecovery;
    public float RespawnTime;
    public bool NoRespawn;

    public DirectionalPuffer(EntityData data, Vector2 offset) : base(data, offset) {
        // replace the sprite with a custom one
        Remove(Get<Sprite>());
        var sprite = CustomSpriteHelper.CreateCustomSprite("pufferFish", data.Attr("directory", "objects/puffer/"));
        Puffer_sprite.SetValue(this, sprite);
        Add(sprite);
        sprite.Play("idle", false, false);
        sprite.SetColor(data.GetColor("color", "ffffff"));

        DashRecovery = data.Name == "CCH/PinkPuffer" ? 2 : data.Int("dashRecovery", 1);

        direction = data.Enum("explodeDirection", ExplodeDirection.Both);

        Static = data.Bool("static", false);
        NoRespawn = data.Bool("noRespawn", false);
        RespawnTime = data.Float("respawnTime", 2.5f);

        MakeStaticIfNeeded();
    }

    public static bool IsRightPuffer(Puffer p) {
        if (p is DirectionalPuffer puffer) {
            return puffer.direction == ExplodeDirection.Right;
        }

        return false;
    }

    public static bool IsLeftPuffer(Puffer p) {
        if (p is DirectionalPuffer puffer) {
            return puffer.direction == ExplodeDirection.Left;
        }

        return false;
    }

    public bool DirectionCheck(Player player) {
        return direction switch {
            ExplodeDirection.Left => player.Position.X > Position.X,
            ExplodeDirection.Right => player.Position.X < Position.X,
            ExplodeDirection.Both => false,
            ExplodeDirection.None => true,
            _ => throw new NotImplementedException(),
        };
    }

    private static int getRenderStartIndex(int orig, Puffer puffer) {
        if (puffer is DirectionalPuffer dirPuff) {
            return dirPuff.direction switch {
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
            return dirPuff.direction switch {
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
            puffer.SetValue("goneTimer", dirPuff.RespawnTime);
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
    }

    /// <summary>Only render part of the puffer's explosion radius indicator</summary>
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
    }

    internal static void Puffer_Explode(ILContext il) {
        ILCursor cursor = new(il);
        while (cursor.SeekVirtFunctionCall(typeof(Player), "ExplodeLaunch")) {
            cursor.Index++; // skipping over the Pop opcode

            cursor.Emit(OpCodes.Ldarg_0); // this
            cursor.Emit(OpCodes.Ldloc_1); // player
            cursor.EmitDelegate(Restore2DashesIfPinkPuffer);
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

    private static void Restore2DashesIfPinkPuffer(Puffer puffer, Player player) {
        if (puffer is DirectionalPuffer { DashRecovery: > 1 } p) {
            player.Dashes = p.DashRecovery;
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
