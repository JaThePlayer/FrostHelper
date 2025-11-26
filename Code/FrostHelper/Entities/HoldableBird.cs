namespace FrostHelper.Entities;

[CustomEntity("FrostHelper/HoldableBird")]
internal sealed class HoldableBird : Glider {
    public int UsesLeft { get; set; }
    
    public int MaxUses { get; }
    
    public float MinXSpeed { get; }
    public float XSpeedMult { get; }
    
    
    public HoldableBird(EntityData e, Vector2 offset) : base(e, offset) {
        MaxUses = e.Int("maxUses", 1);
        MinXSpeed = e.Float("minXSpeed", FlingBird.FlingSpeed.X); // 380f
        XSpeedMult = e.Float("xSpeedMult", 1.2f);

        Remove(sprite);
        sprite = GFX.SpriteBank.Create(e.Attr("xmlName", "glider"));
        Add(sprite);
        
        RefreshUses();
    }

    public override void Update() {
        base.Update();

        var heldPlayer = Hold.Holder;
        var held = Hold.IsHeld && heldPlayer is { };

        if (heldPlayer is { }) {
            sprite.FlipX = heldPlayer.Facing switch {
                Facings.Left => true,
                _ => false,
            };
        }
        
        // Recover the uses BEFORE the launch code itself, to make sure launching from the ground doesn't recover uses.
        if ((!held && OnGround()) || (heldPlayer?.OnGround(1) ?? false)) {
            RefreshUses();
        }
        
        if (UsesLeft > 0 && held && heldPlayer?.StateMachine.State == Player.StNormal) {
            if (AbilityButtonCheck()) {
                LaunchPlayer(heldPlayer);
            }
        }

        if (CollideFirst<BirdRefill>() is { } refill) {
            refill.OnBirdCollide(this);
        }

        sprite.Color = UsesLeft switch {
            1 => Color.White,
            2 => Color.DeepPink,
            _ => Color.DimGray,
        };
    }

    public override void Render() {
        sprite.Rotation = 0f;
        sprite.Scale = Vector2.One;
        Components.Render();
        
        if (bubble)
        {
            for (int i = 0; i < 24; i++)
            {
                Draw.Point(Position + PlatformAdd(i), PlatformColor(i));
            }
        }
    }

    public bool AbilityButtonCheck() {
        if (Input.Dash.Pressed) {
            Input.Dash.ConsumePress();
            return true;
        }
        
        if (Input.CrouchDash.Pressed) {
            Input.CrouchDash.ConsumePress();
            return true;
        }

        return false;
    }

    public void LaunchPlayer(Player player) {
        UsesLeft--;
        
        Audio.Play("event:/new_content/game/10_farewell/bird_throw", Center);
        
        // Copy-paste from Player.FinishFlingBird
        var dir = Input.GetAimVector(player.Facing);
        dir.Y = 1f;
        dir.X = float.Sign(dir.X);
        
        
        player.AutoJump = true;
        player.forceMoveX = (int)dir.X;
        player.forceMoveXTimer = 0.2f;
        
        
        //player.Speed = FlingBird.FlingSpeed * dir;
        player.Speed.Y = FlingBird.FlingSpeed.Y;

        var xSpeed = player.Speed.X * XSpeedMult;
        if (float.Abs(xSpeed) < MinXSpeed)
            xSpeed = MinXSpeed;
        player.Speed.X = float.Abs(xSpeed) * dir.X;
        
        
        player.varJumpTimer = 0.2f;
        player.varJumpSpeed = player.Speed.Y;
        player.launched = true;
    }

    public void RefreshUses() {
        UsesLeft = MaxUses;
    }
}

[CustomEntity("FrostHelper/HoldableBirdRefill")]
[Tracked]
internal sealed class BirdRefill : Refill {
    #region Hooks
    [OnLoad]
    public static void LoadHooks() {
        IL.Celeste.Refill.ctor_EntityData_Vector2 += RefillOnctor_EntityData_Vector2;
        IL.Celeste.Refill.ctor_Vector2_bool_bool += RefillOnctor_Vector2_bool_bool;
    }

    [OnUnload] 
    public static void UnloadHooks() {
        IL.Celeste.Refill.ctor_EntityData_Vector2 -= RefillOnctor_EntityData_Vector2;
        IL.Celeste.Refill.ctor_Vector2_bool_bool -= RefillOnctor_Vector2_bool_bool;
    }
    
    // Capture the EntityData passed to this ctor, so that it can be used in the il hook for the other ctor
    private static void RefillOnctor_EntityData_Vector2(ILContext il) {
        var cursor = new ILCursor(il);
        
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldarg_1);
        cursor.EmitCall(CaptureEntityData);
    }

    // Allow re-spriting the refill
    private static void RefillOnctor_Vector2_bool_bool(ILContext il) {
        var cursor = new ILCursor(il);

        if (cursor.SeekLoadString("objects/refill/")) {
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitCall(GetDirectory);
        }
    }

    private static string GetDirectory(string orig, Refill refill) {
        if (refill is BirdRefill birdRefill) {
            var dir = birdRefill._capturedEntityData.Attr("directory", "objects/refill/");
            if (!dir.EndsWith("/", StringComparison.Ordinal))
                dir += "/";
            
            return dir;
        }

        return orig;
    }
    
    private static void CaptureEntityData(Refill refill, EntityData entityData) {
        if (refill is BirdRefill birdRefill) {
            birdRefill._capturedEntityData = entityData;
        }
    }
    #endregion

    //Used by ctor IL hooks
    private EntityData _capturedEntityData;

    private readonly string useSfx;

    private readonly float respawnTime;
    
    public BirdRefill(EntityData data, Vector2 offset) : base(data, offset) {
        Get<PlayerCollider>().OnCollide = OnPlayerCollide;

        useSfx = data.Attr("useSfx", "event:/game/general/diamond_touch");
        respawnTime = data.Float("respawnTime", 2.5f);
    }

    private void OnPlayerCollide(Player player) {
        if (player.Holding?.Entity is HoldableBird bird) {
            OnBirdCollide(bird);
        }
    }

    internal void OnBirdCollide(HoldableBird bird) {
        if (bird.UsesLeft < bird.MaxUses) {
            bird.RefreshUses();
            
            Audio.Play(useSfx, Position);
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
            Collidable = false;
            Add(new Coroutine(NewRefillRoutine(bird)));
            respawnTimer = respawnTime;
        }
    }
    
    private IEnumerator NewRefillRoutine(HoldableBird bird)
    {
        // Copy-paste from RefillRoutine, changed to use the bird instead of player
        
        Celeste.Celeste.Freeze(0.05f);
        yield return null;
        
        level.Shake(0.3f);
        sprite.Visible = flash.Visible = false;
        if (!oneUse)
        {
            outline.Visible = true;
        }
        Depth = 8999;
        yield return 0.05f;
        float num = bird.Speed.Angle();
        level.ParticlesFG.Emit(p_shatter, 5, Position, Vector2.One * 4f, num - 1.57079637f);
        level.ParticlesFG.Emit(p_shatter, 5, Position, Vector2.One * 4f, num + 1.57079637f);
        SlashFx.Burst(Position, num);
        if (oneUse)
        {
            RemoveSelf();
        }
    }
}