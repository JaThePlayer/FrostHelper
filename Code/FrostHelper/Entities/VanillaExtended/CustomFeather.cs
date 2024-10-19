namespace FrostHelper;

[Tracked(false)]
[CustomEntity("FrostHelper/CustomFeather")]
public class CustomFeather : Entity {
    #region Hooks
    private static bool _hooksLoaded;

    [OnLoad] // TODO: hooks in this class are also used in red custom boosters, split!
    public static void LoadIfNeeded() {
        if (_hooksLoaded)
            return;
        _hooksLoaded = true;

        IL.Celeste.Player.UpdateHair += modFeatherState;
        IL.Celeste.Player.UpdateSprite += modFeatherState;
        IL.Celeste.Player.OnCollideH += modFeatherState;
        IL.Celeste.Player.OnCollideV += modFeatherState;
        IL.Celeste.Player.Render += modFeatherState;
        IL.Celeste.Player.BeforeDownTransition += modFeatherState;
        IL.Celeste.Player.BeforeUpTransition += modFeatherState;
        IL.Celeste.Player.HiccupJump += modFeatherState;

        FrostModule.RegisterILHook(new ILHook(typeof(Player).GetMethod("orig_Update", BindingFlags.Instance | BindingFlags.Public)!, modFeatherState));
        FrostModule.RegisterILHook(new ILHook(typeof(Player).GetMethod("orig_UpdateSprite", BindingFlags.Instance | BindingFlags.NonPublic)!, modFeatherState));

        On.Celeste.PlayerSprite.Render += PlayerSprite_Render;
    }

    private static void PlayerSprite_Render(On.Celeste.PlayerSprite.orig_Render orig, PlayerSprite self) {
        if (self.EntityAs<Player>() is { } player && self.Color == Color.White && player.StateMachine.State == CustomFeatherState) {
            var origC = self.Color;
            self.SetColor(player.Get<PlayerHair>()?.Color ?? self.Color);
            orig(self);
            self.Color = origC;
        } else {
            orig(self);
        }
    }

    [OnUnload]
    public static void UnloadHooks() {
        if (!_hooksLoaded)
            return;
        _hooksLoaded = false;

        IL.Celeste.Player.UpdateHair -= modFeatherState;
        IL.Celeste.Player.UpdateSprite -= modFeatherState;
        IL.Celeste.Player.OnCollideH -= modFeatherState;
        IL.Celeste.Player.OnCollideV -= modFeatherState;
        IL.Celeste.Player.Render -= modFeatherState;
        IL.Celeste.Player.BeforeDownTransition -= modFeatherState;
        IL.Celeste.Player.BeforeUpTransition -= modFeatherState;
        IL.Celeste.Player.HiccupJump -= modFeatherState;

        On.Celeste.PlayerSprite.Render -= PlayerSprite_Render;
    }

    static void modFeatherState(ILContext il) {
        ILCursor cursor = new ILCursor(il);
        while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcI4(Player.StStarFly) && instr.Previous.MatchCallvirt<StateMachine>("get_State"))) {
            cursor.Emit(OpCodes.Ldarg_0); // this
            cursor.EmitCall(FrostModule.GetFeatherState);
        }
        cursor.Index = 0;
        #warning Move to GenericCustomBooster.cs
        while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcI4(Player.StRedDash) && instr.Previous.MatchCallvirt<StateMachine>("get_State"))) {
            cursor.Emit(OpCodes.Ldarg_0); // this
            cursor.EmitCall(FrostModule.GetRedDashState);
        }
    }
    #endregion

    #region CustomState
    public static int CustomFeatherState = int.MaxValue;

    public static void CustomFeatherBegin(Entity e) {
        LoadIfNeeded();

        Player player = (e as Player)!;

        var data = DynamicData.For(player);
        var feather = data.Get<CustomFeather>("fh.customFeather");
        if (feather is null)
            return;
        
        player.Sprite.Play("startStarFly", false, false);
        data.Set("starFlyTransforming", true);
        data.Set("starFlyTimer", feather.FlyTime);
        data.Set("starFlySpeedLerp", 0f);
        data.Set("jumpGraceTimer", 0f);
        BloomPoint? starFlyBloom = player.starFlyBloom;
        if (starFlyBloom == null) {
            player.Add(starFlyBloom = new BloomPoint(new Vector2(0f, -6f), 0f, 16f));
        }
        starFlyBloom.Visible = true;
        starFlyBloom.Alpha = 0f;
        data.Set("starFlyBloom", starFlyBloom);
        player.Collider = data.Get<Hitbox>("starFlyHitbox");
        data.Set("hurtbox", data.Get("starFlyHurtbox"));
        var starFlyLoopSfx = data.Get<SoundSource>("starFlyLoopSfx");
        var starFlyWarningSfx = data.Get<SoundSource>("starFlyWarningSfx");
        if (starFlyLoopSfx == null) {
            player.Add(starFlyLoopSfx = new SoundSource());
            starFlyLoopSfx.DisposeOnTransition = false;
            player.Add(starFlyWarningSfx = new SoundSource());
            starFlyWarningSfx.DisposeOnTransition = false;
        }
        starFlyLoopSfx.Play("event:/game/06_reflection/feather_state_loop", "feather_speed", 1f);
        starFlyWarningSfx?.Stop(true);
        data.Set("starFlyLoopSfx", starFlyLoopSfx);
        data.Set("starFlyWarningSfx", starFlyWarningSfx);
    }
    public static void CustomFeatherEnd(Entity e) {
        Player player = (e as Player)!;
        var feather = DynamicData.For(player).Get<CustomFeather>("fh.customFeather");
        if (feather is null)
            return;

        player.Play("event:/game/06_reflection/feather_state_end", null, 0f);
        player.starFlyWarningSfx.Stop(true);
        player.starFlyLoopSfx.Stop(true);
        player.Hair.DrawPlayerSpriteOutline = false;
        player.Sprite.Color = Color.White;
        player.SceneAs<Level>().Displacement.AddBurst(player.Center, 0.25f, 8f, 32f, 1f, null, null);
        player.starFlyBloom.Visible = false;
        player.Sprite.HairCount = player.startHairCount;
        player.StarFlyReturnToNormalHitbox();

        if (player.StateMachine.State != 2) {
            player.SceneAs<Level>().Particles.Emit(feather.P_Boost, 12, player.Center, Vector2.One * 4f, (-player.Speed).Angle());
        }

        // reset to vanilla value
        player.starFlyColor = ColorHelper.GetColor("ffd65c");
    }
    public static IEnumerator CustomFeatherCoroutine(Entity e) {
        Player player = (e as Player)!;
        var feather = DynamicData.For(player).Get<CustomFeather>("fh.customFeather");
        if (feather is null)
            yield break;

        while (player.Sprite.CurrentAnimationID == "startStarFly") {
            yield return null;
        }
        while (player.Speed != Vector2.Zero) {
            yield return null;
        }
        yield return 0.1f;
        player.Sprite.Color = feather.FlyColor;
        player.Sprite.HairCount = 7;
        player.Hair.DrawPlayerSpriteOutline = true;
        player.SceneAs<Level>().Displacement.AddBurst(player.Center, 0.25f, 8f, 32f, 1f, null, null);
        player.starFlyTransforming = false;
        player.starFlyTimer = feather.FlyTime;

        if (feather.RefillDashes)
            player.RefillDash();

        if (feather.RefillStamina)
            player.RefillStamina();

        Vector2 dir = Input.Feather.Value;
        if (dir == Vector2.Zero) {
            dir = Vector2.UnitX * (float) player.Facing;
        }

        player.Speed = dir * 250f;
        player.starFlyLastDir = dir;
        player.SceneAs<Level>().Particles.Emit(feather.P_Boost, 12, player.Center, Vector2.One * 4f, feather.FlyColor, (-dir).Angle());
        Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
        player.SceneAs<Level>().DirectionalShake(player.starFlyLastDir, 0.3f);
        while (player.starFlyTimer > 0.5f) {
            player.Hair.Color = player.Sprite.Color = feather.FlyColor;
            yield return null;
        }
        player.starFlyWarningSfx.Play("event:/game/06_reflection/feather_state_warning", null, 0f);
        yield break;
    }

    public static int StarFlyUpdate(Entity e) {
        Player player = (e as Player)!;
        Level level = player.SceneAs<Level>();
        BloomPoint bloomPoint = player.starFlyBloom;
        var feather = DynamicData.For(player).Get<CustomFeather>("fh.customFeather");
        if (feather is null)
            return Player.StNormal;

        float StarFlyTime = feather.FlyTime;
        bloomPoint.Alpha = Calc.Approach(bloomPoint.Alpha, 0.7f, Engine.DeltaTime * StarFlyTime);
        player.starFlyBloom = bloomPoint;
        Input.Rumble(RumbleStrength.Climb, RumbleLength.Short);

        if (player.starFlyTransforming) {
            player.Speed = Calc.Approach(player.Speed, Vector2.Zero, 1000f * Engine.DeltaTime);
        } else {
            Vector2 aimValue = Input.Feather.Value;
            bool notAiming = false;
            if (aimValue == Vector2.Zero) {
                notAiming = true;
                aimValue = player.starFlyLastDir;
            }
            Vector2 lastSpeed = player.Speed.SafeNormalize(Vector2.Zero);

            if (lastSpeed == Vector2.Zero) {
                lastSpeed = aimValue;
            } else {
                lastSpeed = lastSpeed.RotateTowards(aimValue.Angle(), 5.58505344f * Engine.DeltaTime);
            }
            player.starFlyLastDir = lastSpeed;
            float target;
            if (notAiming) {
                player.starFlySpeedLerp = 0f;
                target = feather.NeutralSpeed; // was 91f
            } else {
                if (lastSpeed != Vector2.Zero && Vector2.Dot(lastSpeed, aimValue) >= 0.45f) {
                    player.starFlySpeedLerp = Calc.Approach(player.starFlySpeedLerp, 1f, Engine.DeltaTime / 1f);
                    target = MathHelper.Lerp(feather.LowSpeed, feather.MaxSpeed, player.starFlySpeedLerp);
                } else {
                    player.starFlySpeedLerp = 0f;
                    target = 140f;
                }
            }

            SoundSource ss = player.starFlyLoopSfx;
            ss.Param("feather_speed", notAiming ? 0 : 1);

            float speed = player.Speed.Length();
            speed = Calc.Approach(speed, target, 1000f * Engine.DeltaTime);
            player.Speed = lastSpeed * speed;

            if (level.OnInterval(0.02f)) {
                level.Particles.Emit(feather.P_Flying, 1, player.Center, Vector2.One * 2f, feather.FlyColor, (-player.Speed).Angle());
            }

            if (Input.Jump.Pressed) {
                if (player.OnGround(3)) {
                    player.Jump(true, true);
                    return Player.StNormal;
                }

                if (player.WallJumpCheck(-1)) {
                    player.WallJump(1);
                    return Player.StNormal;
                }

                if (player.WallJumpCheck(1)) {
                    player.WallJump(-1);
                    return Player.StNormal;
                }
            }

            if (Input.Grab.Check) {
                bool startClimb = false;
                int dir = 0;
                if (Input.MoveX.Value != -1 && player.ClimbCheck(1, 0)) {
                    player.Facing = Facings.Right;
                    dir = 1;
                    startClimb = true;
                } else {
                    if (Input.MoveX.Value != 1 && player.ClimbCheck(-1, 0)) {
                        player.Facing = Facings.Left;
                        dir = -1;
                        startClimb = true;
                    }
                }

                if (startClimb) {
                    if (SaveData.Instance.Assists.NoGrabbing) {
                        player.Speed = Vector2.Zero;
                        player.ClimbTrigger(dir);
                        return 0;
                    }
                    return Player.StClimb;
                }
            }

            if (player.CanDash) {
                return player.StartDash();
            }

            player.starFlyTimer -= Engine.DeltaTime;

            if (player.starFlyTimer <= 0f) {
                if (Input.MoveY.Value == -1) {
                    player.Speed.Y = -100f;
                }

                if (Input.MoveY.Value < 1) {
                    player.varJumpSpeed = player.Speed.Y;
                    player.AutoJump = true;
                    player.AutoJumpTimer = 0f;
                    player.varJumpTimer = 0.2f;
                }

                if (player.Speed.Y > 0f) {
                    player.Speed.Y = 0f;
                }

                if (Math.Abs(player.Speed.X) > 140f) {
                    player.Speed.X = 140f * Math.Sign(player.Speed.X);
                }

                Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
                return Player.StNormal;
            }

            if (player.starFlyTimer < 0.5f && player.Scene.OnInterval(0.05f)) {
                Color starFlyColor = feather.FlyColor;
                if (player.Sprite.Color == starFlyColor) {
                    player.Sprite.Color = Player.NormalHairColor;
                } else {
                    player.Sprite.Color = starFlyColor;
                }
            }
        }
        return CustomFeatherState;
    }

    #endregion

    /// <summary>
    /// the maximum speed you fly with this feather
    /// </summary>
    public float MaxSpeed;
    /// <summary>
    /// the lowest speed you fly with this feather while holding a direction
    /// </summary>
    public float LowSpeed;

    /// <summary>
    /// The speed you fly with when not holding any direction
    /// </summary>
    public float NeutralSpeed;

    bool RefillStamina, RefillDashes;

    public CustomFeather(EntityData data, Vector2 offset) : base(data.Position + offset) {
        LoadIfNeeded();

        shielded = data.Bool("shielded", false);
        singleUse = data.Bool("singleUse", false);
        RespawnTime = data.Float("respawnTime", 3f);
        FlyColor = ColorHelper.GetColor(data.Attr("flyColor", "ffd65c"));
        FlyTime = data.Float("flyTime", 2f);
        MaxSpeed = data.Float("maxSpeed", 190f);
        LowSpeed = data.Float("lowSpeed", 140f);
        NeutralSpeed = data.Float("neutralSpeed", 91f);
        Collider = new Hitbox(20f, 20f, -10f, -10f);
        Add(new PlayerCollider(OnPlayer, null, null));
        string path = data.Attr("spritePath", "objects/flyFeather/").Replace('\\', '/');
        if (!path.EndsWith('/')) {
            path += '/';
        }
        sprite = new Sprite(GFX.Game, path) {
            Visible = true
        };
        sprite.CenterOrigin();
        sprite.Color = ColorHelper.GetColor(data.Attr("spriteColor", "White"));
        sprite.Justify = new Vector2(0.5f, 0.5f);
        sprite.Add("loop", "idle", 0.06f, "flash");
        sprite.Add("flash", "flash", 0.06f, "loop");
        sprite.Play("loop");
        Add(sprite);

        Add(wiggler = Wiggler.Create(1f, 4f, delegate (float v) {
            sprite.Scale = Vector2.One * (1f + v * 0.2f);
        }, false, false));
        Add(bloom = new BloomPoint(0.5f, 20f));
        Add(light = new VertexLight(Color.White, 1f, 16, 48));
        Add(sine = new SineWave(0.6f, 0f).Randomize());
        Add(outline = new Image(GFX.Game[$"{path}outline"]));
        outline.CenterOrigin();
        outline.Visible = false;
        shieldRadiusWiggle = Wiggler.Create(0.5f, 4f);
        Add(shieldRadiusWiggle);
        moveWiggle = Wiggler.Create(0.8f, 2f);
        moveWiggle.StartZero = true;
        Add(moveWiggle);
        UpdateY();

        P_Collect = new ParticleType(FlyFeather.P_Collect) {
            ColorMode = ParticleType.ColorModes.Static,
            Color = FlyColor
        };
        P_Flying = new ParticleType(FlyFeather.P_Flying) {
            ColorMode = ParticleType.ColorModes.Static,
            Color = FlyColor
        };
        P_Boost = new ParticleType(FlyFeather.P_Boost) {
            ColorMode = ParticleType.ColorModes.Static,
            Color = FlyColor
        };

        RefillStamina = data.Bool("refillStamina", true);
        RefillDashes = data.Bool("refillDashes", true);
    }

    public override void Added(Scene scene) {
        base.Added(scene);
        level = SceneAs<Level>();
    }

    public override void Update() {
        base.Update();

        if (respawnTimer > 0f) {
            respawnTimer -= Engine.DeltaTime;

            if (respawnTimer <= 0f) {
                Respawn();
            }
        }

        UpdateY();
        light.Alpha = Calc.Approach(light.Alpha, sprite.Visible ? 1f : 0f, 4f * Engine.DeltaTime);
        bloom.Alpha = light.Alpha * 0.8f;
    }

    public override void Render() {
        base.Render();

        if (shielded && sprite.Visible) {
            Draw.Circle(Position + sprite.Position, 10f - shieldRadiusWiggle.Value * 2f, Color.White, 3);
        }
    }

    private void Respawn() {
        if (!Collidable) {
            outline.Visible = false;
            Collidable = true;
            sprite.Visible = true;
            wiggler.Start();
            Audio.Play("event:/game/06_reflection/feather_reappear", Position);
            level.ParticlesFG.Emit(FlyFeather.P_Respawn, 16, Position, Vector2.One * 2f, FlyColor);
        }
    }

    private void UpdateY() {
        sprite.X = 0f;
        sprite.Y = bloom.Y = sine.Value * 2f;
        sprite.Position += moveWiggleDir * moveWiggle.Value * -8f;
    }

    private void OnPlayer(Player player) {
        Vector2 speed = player.Speed;

        if (shielded && !player.DashAttacking) {
            player.PointBounce(Center);
            moveWiggle.Start();
            shieldRadiusWiggle.Start();
            moveWiggleDir = (Center - player.Center).SafeNormalize(Vector2.UnitY);
            Audio.Play("event:/game/06_reflection/feather_bubble_bounce", Position);
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
        } else {
            if (StartStarFly(player)) {
                player.starFlyColor = FlyColor;

                if (player.StateMachine.State != CustomFeatherState && player.StateMachine.State != Player.StStarFly) {
                    Audio.Play(shielded ? "event:/game/06_reflection/feather_bubble_get" : "event:/game/06_reflection/feather_get", Position);
                } else {
                    Audio.Play(shielded ? "event:/game/06_reflection/feather_bubble_renew" : "event:/game/06_reflection/feather_renew", Position);
                }
                Collidable = false;
                Add(new Coroutine(CollectRoutine(player, speed), true));
                if (!singleUse) {
                    outline.Visible = true;
                    respawnTimer = RespawnTime;
                }
            }
        }
    }

    public Color FlyColor;
    public float FlyTime;

    public bool StartStarFly(Player player) {
        if (RefillStamina)
            player.RefillStamina();

        if (player.StateMachine.State == Player.StReflectionFall) {
            return false;
        }

        var data = DynamicData.For(player);
        data.Set("fh.customFeather", this);
        if (player.StateMachine.State == CustomFeatherState) {
            player.starFlyTimer = FlyTime;
            player.Sprite.Color = FlyColor;
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
        } else {
            player.StateMachine.State = CustomFeatherState;
        }

        return true;
    }

    private IEnumerator CollectRoutine(Player player, Vector2 playerSpeed) {
        level.Shake(0.3f);
        sprite.Visible = false;
        yield return 0.05f;
        float angle;
        if (playerSpeed != Vector2.Zero) {
            angle = playerSpeed.Angle();
        } else {
            angle = (Position - player.Center).Angle();
        }
        level.ParticlesFG.Emit(P_Collect, 10, Position, Vector2.One * 6f, FlyColor);
        SlashFx.Burst(Position, angle);
        yield break;
    }

    public ParticleType P_Collect;

    public ParticleType P_Boost;

    public ParticleType P_Flying;

    //public static ParticleType P_Respawn;

    private float RespawnTime = 3f;

    private Sprite sprite;

    private Image outline;

    private Wiggler wiggler;

    private BloomPoint bloom;

    private VertexLight light;

    private Level level;

    private SineWave sine;

    private bool shielded;

    private bool singleUse;

    private Wiggler shieldRadiusWiggle;

    private Wiggler moveWiggle;

    private Vector2 moveWiggleDir;

    private float respawnTimer;
}
