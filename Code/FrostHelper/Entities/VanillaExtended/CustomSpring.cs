using FrostHelper.Helpers;
using FrostHelper.ModIntegration;

namespace FrostHelper;

[CustomEntity("FrostHelper/SpringLeft", "FrostHelper/SpringRight", "FrostHelper/SpringFloor", "FrostHelper/SpringCeiling")]
public class CustomSpring : Spring {
    #region Hooks
    private static bool _hooksLoaded;

    [HookPreload]
    public static void LoadIfNeeded() {
        if (_hooksLoaded)
            return;
        _hooksLoaded = true;

        On.Celeste.TheoCrystal.HitSpring += TheoCrystal_HitSpring;
        On.Celeste.Glider.HitSpring += Glider_HitSpring;
        On.Celeste.Puffer.HitSpring += Puffer_HitSpring;
        IL.Celeste.Spring.BounceAnimate += SpringOnBounceAnimate;
    }

    [OnUnload]
    public static void Unload() {
        if (!_hooksLoaded)
            return;
        _hooksLoaded = false;

        On.Celeste.TheoCrystal.HitSpring -= TheoCrystal_HitSpring;
        On.Celeste.Glider.HitSpring -= Glider_HitSpring;
        On.Celeste.Puffer.HitSpring -= Puffer_HitSpring;
        IL.Celeste.Spring.BounceAnimate -= SpringOnBounceAnimate;
    }

    private static bool Puffer_HitSpring(On.Celeste.Puffer.orig_HitSpring orig, Puffer self, Spring spring) {
        if (spring is CustomSpring customSpring && customSpring.Orientation == CustomOrientations.Ceiling) {
            if (self.hitSpeed.Y <= 0f) {
                self.GotoHitSpeed(224f * Vector2.UnitY);
                self.MoveTowardsX(spring.CenterX, 4f, null);
                self.bounceWiggler.Start();
                self.Alert(true, false);
                return true;
            }
            return false;
        } else {
            return orig(self, spring);
        }
    }

    private static bool Glider_HitSpring(On.Celeste.Glider.orig_HitSpring orig, Glider self, Spring spring) {
        if (spring is CustomSpring customSpring && customSpring.Orientation == CustomOrientations.Ceiling) {
            if (!self.Hold.IsHeld && self.Speed.Y <= 0f) {
                self.Speed.X *= 0.5f;
                self.Speed.Y = -160f;
                self.noGravityTimer = 0.15f;
                self.wiggler.Start();
                return true;
            }
            return false;
        } else {
            return orig(self, spring);
        }
    }

    private static bool TheoCrystal_HitSpring(On.Celeste.TheoCrystal.orig_HitSpring orig, TheoCrystal self, Spring spring) {
        if (spring is CustomSpring customSpring && customSpring.Orientation == CustomOrientations.Ceiling) {
            if (!self.Hold.IsHeld && self.Speed.Y <= 0f) {
                self.Speed.X *= 0.5f;
                self.Speed.Y = -160f;
                self.noGravityTimer = 0.15f;
                return true;
            }
            return false;
        } else {
            return orig(self, spring);
        }
    }
    
    // Patch sfx
    private static void SpringOnBounceAnimate(ILContext il) {
        var cursor = new ILCursor(il);
        while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdstr("event:/game/general/spring"))) {
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate(GetBounceSfx);
        }
    }

    private static string GetBounceSfx(string orig, Spring spring) {
        if (spring is not CustomSpring customSpring)
            return orig;

        return customSpring.BounceSfx;
    }

    public readonly string BounceSfx;

    #endregion
    
    public enum CustomOrientations {
        Floor,
        WallLeft,
        WallRight,
        Ceiling
    }

    private float inactiveTimer;

    public new CustomOrientations Orientation;
    public bool RenderOutline;
    public Sprite Sprite => sprite;

    string dir;

    internal Vector2 speedMult;
    private int Version;

    public bool MultiplyPlayerY;
    
    public bool OneUse;


    /// <summary>
    /// Whether the spring should always activate, regardless of the player's/holdable/fish speed.
    /// </summary>
    public bool AlwaysActivate;

    /// <summary>
    /// Sentinel value for <see cref="DashRecovery"/> and <see cref="StaminaRecovery"/>,
    /// which makes them refill your dashes/stamina instead of adding/removing from it.
    /// </summary>
    internal const int DashAndStaminaRecoveryIsARefill = 10000;
    internal const int DashAndStaminaRecoveryIsIgnored = 10001;
    internal readonly int DashRecovery;
    internal readonly int StaminaRecovery;

    private static readonly Dictionary<string, CustomOrientations> EntityDataNameToOrientation = new() {
        ["FrostHelper/SpringLeft"] = CustomOrientations.WallLeft,
        ["FrostHelper/SpringRight"] = CustomOrientations.WallRight,
        ["FrostHelper/SpringFloor"] = CustomOrientations.Floor,
        ["FrostHelper/SpringCeiling"] = CustomOrientations.Ceiling,
    };

    private static readonly Dictionary<CustomOrientations, Orientations> CustomToRegularOrientation = new() {
        [CustomOrientations.WallLeft] = Orientations.WallLeft,
        [CustomOrientations.WallRight] = Orientations.WallRight,
        [CustomOrientations.Floor] = Orientations.Floor,
        [CustomOrientations.Ceiling] = Orientations.Floor,
    };

    public CustomSpring(EntityData data, Vector2 offset) : this(data, offset, EntityDataNameToOrientation[data.Name]) { }

    public CustomSpring(EntityData data, Vector2 offset, CustomOrientations orientation) : base(data.Position + offset, CustomToRegularOrientation[orientation], data.Bool("playerCanUse", true)) {
        LoadIfNeeded();
        Version = data.Int("version", 0);
        // this class also has lazy hooks, and we don't want a lag spike when first hitting a spring
        TimeBasedClimbBlocker.LoadIfNeeded();

        playerCanUse = data.Bool("playerCanUse", true);
        dir = data.Attr("directory", "objects/spring/");
        RenderOutline = data.Bool("renderOutline", true);

        
        DashRecovery = data.Int("dashRecovery", DashAndStaminaRecoveryIsARefill);
        StaminaRecovery = data.Int("staminaRecovery", DashAndStaminaRecoveryIsARefill);
        
        //speedMult = FrostModule.StringToVec2(data.Attr("speedMult", "1"));
        // LEGACY BEHAVIOUR TIME!
        // there was a bug that made multiplying the Y speed of horizontal springs not work
        // thankfully noone knew about the "supply a Vec2" behaviour either
        // if only a float is supplied, the Y value will be set to 1
        /*
        speedMult = orientation switch {
            CustomOrientations.WallLeft or CustomOrientations.WallRight => data.GetVec2("speedMult", Vector2.One, true),
            _ => new(data.Float("speedMult", 1f)), // other orientations only care about the Y component anyway
        };*/
        speedMult = data.GetVec2("speedMult", Vector2.One);
        MultiplyPlayerY = orientation switch {
            CustomOrientations.WallLeft or CustomOrientations.WallRight => data.Attr("speedMult").Contains(','),
            _ => true,
        };

        DisabledColor = Color.White;
        Orientation = orientation;
        base.Orientation = CustomToRegularOrientation[orientation];
        Remove(Get<PlayerCollider>());
        Add(new PlayerCollider(NewOnCollide));
        Remove(Get<HoldableCollider>());
        Add(new HoldableCollider(NewOnHoldable));
        
        Remove(Get<PufferCollider>());
        var pufferCollider = new PufferCollider(NewOnPuffer);
        Add(pufferCollider);

        Remove(sprite);
        Add(sprite = new Sprite(GFX.Game, dir) {
            Color = data.GetColor("color", "White"),
        });
        sprite.Add("idle", "", 0f, new int[1]);
        sprite.Add("bounce", "", 0.07f, "idle", 0, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 3, 4, 5);
        sprite.Add("disabled", "white", 0.07f);
        sprite.Play("idle");
        sprite.Origin.X = sprite.Width / 2f;
        sprite.Origin.Y = sprite.Height;

        Depth = -8501;

        Add(Wiggler.Create(1f, 4f, delegate (float v) {
            Sprite.Scale.Y = 1f + v * 0.2f;
        }));

        var attachGroup = data.Int("attachGroup", -1);
        var oldMover = staticMover;
        var mover = attachGroup switch {
            -1 => new StaticMover {
                OnAttach = oldMover.OnAttach,
            },
            _ => new GroupedStaticMover(attachGroup, true).SetOnAttach(oldMover.OnAttach)
        };

        mover.OnEnable = oldMover.OnEnable;
        mover.OnDisable = oldMover.OnDisable;
        mover.SolidChecker = oldMover.SolidChecker;
        mover.JumpThruChecker = oldMover.JumpThruChecker;

        switch (orientation) {
            case CustomOrientations.Floor:
                Collider = new Hitbox(16f, 6f, -8f, -6f);
                pufferCollider.Collider = new Hitbox(16f, 10f, -8f, -10f);
                break;
            case CustomOrientations.WallLeft:
                Collider = new Hitbox(6f, 16f, 0f, -8f);
                pufferCollider.Collider = new Hitbox(12f, 16f, 0f, -8f);
                Sprite.Rotation = MathHelper.PiOver2;
                break;
            case CustomOrientations.WallRight:
                Collider = new Hitbox(6f, 16f, -6f, -8f);
                pufferCollider.Collider = new Hitbox(12f, 16f, -12f, -8f);
                Sprite.Rotation = -MathHelper.PiOver2;
                break;
            case CustomOrientations.Ceiling:
                Collider = new Hitbox(16f, 6f, -8f, 0);
                pufferCollider.Collider = new Hitbox(16f, 10f, -8f, -4f);
                Sprite.Rotation = MathHelper.Pi;
                mover.SolidChecker = (Solid s) => CollideCheck(s, Position - Vector2.UnitY);
                mover.JumpThruChecker = (JumpThru jt) => CollideCheck(jt, Position - Vector2.UnitY);
                break;
            default:
                throw new Exception("Orientation not supported!");
        }

        Remove(oldMover);
        Add(mover);
        staticMover = mover;

        OneUse = data.Bool("oneUse");
        if (OneUse) {
            Add(new Coroutine(OneUseParticleRoutine()));
        }

        AlwaysActivate = data.Bool("alwaysActivate", false);
        BounceSfx = data.Attr("sfx", "event:/game/general/spring");
    }

    public override void Update() {
        base.Update();

        inactiveTimer -= Engine.DeltaTime;
    }

    public override void Render() {
        if (!CameraCullHelper.IsVisible(sprite))
            return;

        if (Collidable && RenderOutline)
            sprite.DrawOutlineFast(Color.Black);
        sprite.Render();
    }

    private void NewOnCollide(Player player) {
        if (player.StateMachine.State == Player.StDreamDash || !playerCanUse)
            return;

        var prevDashes = player.Dashes;
        var prevStamina = player.Stamina;
        
        if (DashRecovery < 0 && prevDashes < -DashRecovery)
            return;
        if (StaminaRecovery < 0 && prevStamina < -StaminaRecovery)
            return;

        if (Version == 0 && NewOnCollide_Version0(player)) {
            return;
        }

        switch (Orientation) {
            case CustomOrientations.Floor:
            case CustomOrientations.Ceiling:
                // weird check here to fix buffered spring cancels
                var realY = GravityHelperIntegration.InvertIfPlayerInverted(player.Speed.Y);
                if (!AlwaysActivate && (Orientation == CustomOrientations.Floor && realY < 0 ||
                    Orientation == CustomOrientations.Ceiling && (realY > 0 || realY == 0 && inactiveTimer > 0))) {
                    return;
                }

                var playerInverted = GravityHelperIntegration.IsPlayerInverted?.Invoke() ?? false;

                BounceAnimate();

                if (playerInverted && Orientation == CustomOrientations.Floor) {
                    GravityHelperIntegration.InvertedSuperBounce(player, Top);
                    player.Speed.Y *= speedMult.Y;
                } else if (!playerInverted && Orientation == CustomOrientations.Ceiling) {
                    player.SuperBounce(Bottom + player.Height);
                    player.Speed.Y *= -speedMult.Y;
                } else {
                    player.SuperBounce(Orientation == CustomOrientations.Floor ? Top : Bottom);
                    player.Speed.Y *= speedMult.Y;
                }

                player.varJumpSpeed = player.Speed.Y;

                OnSuccessfulPlayerHit(player, prevDashes, prevStamina);

                if (playerInverted && Orientation == CustomOrientations.Floor || Orientation == CustomOrientations.Ceiling) {
                    TimeBasedClimbBlocker.NoClimbTimer = 4f / 60f;
                    inactiveTimer = 6f * Engine.DeltaTime;
                }

                break;
            case CustomOrientations.WallLeft:
                // If AlwaysActivate is enabled, we'll set the player's speed to the target direction,
                // to make the early return in player.SideBounce never activate.
                // Alternatively, player.SideBounce could be hooked to do this, but that's more work and overhead :p
                if (AlwaysActivate)
                    player.Speed.X = 1;
                
                if (player.SideBounce(1, Right, CenterY)) {
                    BounceAnimate();
                    player.Speed *= speedMult;
                    if (MultiplyPlayerY)
                        player.varJumpSpeed = player.Speed.Y;
                    OnSuccessfulPlayerHit(player, prevDashes, prevStamina);
                }
                break;
            case CustomOrientations.WallRight:
                // read comment in case WallLeft
                if (AlwaysActivate)
                    player.Speed.X = -1;
                
                if (player.SideBounce(-1, Left, CenterY)) {
                    player.Speed *= speedMult;
                    if (MultiplyPlayerY)
                        player.varJumpSpeed = player.Speed.Y;
                    BounceAnimate();

                    OnSuccessfulPlayerHit(player, prevDashes, prevStamina);
                }
                break;
            default:
                throw new Exception("Orientation not supported!");
        }
    }

    /// <summary>
    /// Old version of the NewOnCollide method before PR4. This has broken upside-down spring behaviour - it launches the player way too high.
    /// </summary>
    private bool NewOnCollide_Version0(Player player) {
        var prevDashes = player.Dashes;
        var prevStamina = player.Stamina;
        
        switch (Orientation) {
            case CustomOrientations.Floor:
                if (AlwaysActivate || GravityHelperIntegration.InvertIfPlayerInverted(player.Speed.Y) >= 0f) {
                    BounceAnimate();
                    GravityHelperIntegration.SuperBounce(player, Top);
                    player.Speed.Y *= speedMult.Y;

                    OnSuccessfulPlayerHit(player, prevDashes, prevStamina);
                }
                return true;
            case CustomOrientations.Ceiling:
                // weird check here to fix buffered spring cancels
                if (AlwaysActivate || (
                        GravityHelperIntegration.InvertIfPlayerInverted(player.Speed.Y) < 0f
                        || (player.Speed.Y == 0f && inactiveTimer <= 0f)
                )) {
                    BounceAnimate();

                    if (GravityHelperIntegration.IsPlayerInverted?.Invoke() ?? false) {
                        player.SuperBounce(Bottom + player.Height);
                        player.Speed.Y *= speedMult.Y;
                    } else {
                        player.SuperBounce(Bottom + player.Height);
                        player.Speed.Y *= -speedMult.Y;
                    }

                    player.varJumpSpeed = player.Speed.Y;
                    OnSuccessfulPlayerHit(player, prevDashes, prevStamina);

                    TimeBasedClimbBlocker.NoClimbTimer = 4f / 60f;
                    inactiveTimer = 6f * Engine.DeltaTime;
                }
                return true;
        }
        return false;
    }

    private void OnSuccessfulHit() {
        if (OneUse) {
            Add(new Coroutine(BreakRoutine()));
        }
    }

    private void OnSuccessfulPlayerHit(Player player, int prevDashes, float prevStamina) {
        OnSuccessfulHit();

        switch (DashRecovery) {
            case DashAndStaminaRecoveryIsARefill:
                break;
            case DashAndStaminaRecoveryIsIgnored:
                player.Dashes = prevDashes;
                break;
            case > DashAndStaminaRecoveryIsIgnored:
                NotificationHelper.Notify($"DashRecovery value of {DashRecovery} is invalid and reserved for future use.\nPlease use a different value!");
                break;
            case < 0:
                player.Dashes = prevDashes + DashRecovery;
                break;
            default:
                player.Dashes = DashRecovery;
                break;
        }
        
        switch (StaminaRecovery) {
            case DashAndStaminaRecoveryIsARefill:
                break;
            case DashAndStaminaRecoveryIsIgnored:
                player.Stamina = prevStamina;
                break;
            case > DashAndStaminaRecoveryIsIgnored:
                NotificationHelper.Notify($"StaminaRecovery value of {StaminaRecovery} is invalid and reserved for future use.\nPlease use a different value!");
                break;
            case < 0:
                player.Stamina = prevStamina + StaminaRecovery;
                break;
            default:
                player.Stamina = StaminaRecovery;
                break;
        }
    }

    public IEnumerator BreakRoutine() {
        Collidable = false;
        Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
        Audio.Play("event:/game/general/platform_disintegrate", Center);
        foreach (Image image in Components.GetAll<Image>()) {
            SceneAs<Level>().Particles.Emit(CrumblePlatform.P_Crumble, 2, Position + image.Position, Vector2.One * 3f);
        }

        float t = 1f;
        while (t > 0f) {
            foreach (Image image in Components.GetAll<Image>()) {
                image.Scale = Vector2.One * t;
                image.Rotation += Engine.DeltaTime * 4;
            }
            t -= Engine.DeltaTime * 4;
            yield return null;
        }
        Visible = false;
        RemoveSelf();
        yield break;
    }

    private void NewOnHoldable(Holdable h) {
        if (h.HitSpring(this)) {
            BounceAnimate();
            OnSuccessfulHit();

            // Apply speed multiplier
            if (h is { SpeedGetter: { }, SpeedSetter: { } }) {
                // Old implementation didn't work with custom holdables
                if (Version < 2 && h.Entity is not Glider and not TheoCrystal) {
                    return;
                }
                
                var speed = h.GetSpeed();
                switch (Orientation)
                {
                    case CustomOrientations.Floor:
                        speed.Y *= speedMult.Y;
                        break;
                    case CustomOrientations.Ceiling:
                        speed.Y *= -speedMult.Y;
                        break;
                    default:
                        speed *= speedMult;
                        break;
                }
                h.SetSpeed(speed);
            }
        }
    }

    private void NewOnPuffer(Puffer p) {
        if (p.HitSpring(this)) {
            p.hitSpeed *= speedMult;
            BounceAnimate();
            OnSuccessfulHit();
        }
    }

    private static readonly ParticleType P_Crumble_Up = new() {
        Color = Calc.HexToColor("847E87"),
        FadeMode = ParticleType.FadeModes.Late,
        Size = 1f,
        Direction = MathHelper.PiOver2,
        SpeedMin = -5f,
        SpeedMax = -25f,
        LifeMin = 0.8f,
        LifeMax = 1f,
        Acceleration = Vector2.UnitY * -20f
    };

    private static readonly ParticleType P_Crumble_Down = new() {
        Color = Calc.HexToColor("847E87"),
        FadeMode = ParticleType.FadeModes.Late,
        Size = 1f,
        Direction = MathHelper.PiOver2,
        SpeedMin = 5f,
        SpeedMax = 25f,
        LifeMin = 0.8f,
        LifeMax = 1f,
        Acceleration = Vector2.UnitY * 20f
    };

    private static readonly ParticleType P_Crumble_Left = new() {
        Color = Calc.HexToColor("847E87"),
        FadeMode = ParticleType.FadeModes.Late,
        Size = 1f,
        Direction = 0f,
        SpeedMin = 5f,
        SpeedMax = 25f,
        LifeMin = 0.8f,
        LifeMax = 1f,
        Acceleration = Vector2.UnitY * 20f
    };

    private static readonly ParticleType P_Crumble_Right = new() {
        Color = Calc.HexToColor("847E87"),
        FadeMode = ParticleType.FadeModes.Late,
        Size = 1f,
        Direction = 0f,
        SpeedMin = -5f,
        SpeedMax = -25f,
        LifeMin = 0.8f,
        LifeMax = 1f,
        Acceleration = Vector2.UnitY * -20f
    };

    private IEnumerator OneUseParticleRoutine() {
        while (true) {
            switch (Orientation) {
                case CustomOrientations.Floor:
                    SceneAs<Level>().Particles.Emit(P_Crumble_Up, 2, Position, new(3f));
                    break;
                case CustomOrientations.WallRight:
                    SceneAs<Level>().Particles.Emit(P_Crumble_Right, 2, Position, new(2f));
                    break;
                case CustomOrientations.WallLeft:
                    SceneAs<Level>().Particles.Emit(P_Crumble_Left, 2, Position, new(2f));
                    break;
                case CustomOrientations.Ceiling:
                    SceneAs<Level>().Particles.Emit(P_Crumble_Down, 2, Position, new(2f));
                    break;
            }
            yield return 0.25f;
        }
    }
}
