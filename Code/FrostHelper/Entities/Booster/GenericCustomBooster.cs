using Celeste.Mod.Meta;
using FrostHelper.Helpers;
using FrostHelper.ModIntegration;
using FrostHelper.TweakManagers;
using System.Diagnostics;

namespace FrostHelper.Entities.Boosters {
    [Tracked(true)]
    public class GenericCustomBooster : Entity, IAttachable {
        public static string DynamicDataName => "fh.booster";
        
        #region Hooks
        private static bool _hooksLoaded;

        [HookPreload]
        public static void LoadIfNeeded() {
            if (_hooksLoaded)
                return;
            _hooksLoaded = true;

            IL.Celeste.Player.OnBoundsH += modRedDashState;
            IL.Celeste.Player.OnBoundsV += modRedDashState;
            IL.Celeste.DashBlock.OnDashed += modRedDashStateDashBlock;
            FrostModule.RegisterILHook(new ILHook(typeof(Player).GetProperty("DashAttacking", BindingFlags.Instance | BindingFlags.Public)!.GetGetMethod(true)!, modRedDashState));
            FrostModule.RegisterILHook(new ILHook(typeof(Player).GetMethod("CallDashEvents", BindingFlags.NonPublic | BindingFlags.Instance)!, modCallDashEvents));
            FrostModule.RegisterILHook(new ILHook(typeof(Player).GetMethod("orig_WindMove", BindingFlags.NonPublic | BindingFlags.Instance)!, modBoosterState));

            // eliminate a lag spike when first leaving a booster
            ChangeDashSpeedOnce.LoadIfNeeded();
        }

        [OnUnload]
        public static void Unload() {
            if (!_hooksLoaded)
                return;
            _hooksLoaded = false;

            IL.Celeste.Player.OnBoundsH -= modRedDashState;
            IL.Celeste.Player.OnBoundsV -= modRedDashState;
            IL.Celeste.DashBlock.OnDashed -= modRedDashStateDashBlock;
        }

        static void modRedDashState(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcI4(5) && instr.Previous.MatchCallvirt<StateMachine>("get_State"))) {
                cursor.Emit(OpCodes.Ldarg_0); // this
                cursor.EmitCall(FrostModule.GetRedDashState);
            }
        }

        static void modRedDashStateDashBlock(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcI4(5) && instr.Previous.MatchCallvirt<StateMachine>("get_State"))) {
                cursor.Emit(OpCodes.Ldarg_1); // arg 1
                cursor.EmitCall(FrostModule.GetRedDashState);
            }
        }

        static void modCallDashEvents(ILContext il) {
            var cursor = new ILCursor(il);

            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchStfld<Player>("calledDashEvents"))) {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(ShouldCallDashEvents);

                // if delegateOut == false: return
                cursor.Emit(OpCodes.Brtrue, cursor.Instrs[cursor.Index]);
                cursor.Emit(OpCodes.Ret);
            }
        }

        private static bool ShouldCallDashEvents(Player self) {
            if (GetBoosterThatIsBoostingPlayer(self) is { BoostingPlayer: true }) {
                self.calledDashEvents = false;
                self.SetDynamicDataAttached<GenericCustomBooster>(null);
                return false;
            }

#if OLD_YELLOW_BOOSTER
                foreach (YellowBoosterOLD b in self.Scene.Tracker.GetEntities<YellowBoosterOLD>()) {
                    b.sprite.SetColor(Color.White);
                    if (b.StartedBoosting) {
                        b.PlayerBoosted(self, self.DashDir);
                        player_calledDashEvents.SetValue(self, false);
                        return false;
                    }
                    if (b.BoostingPlayer) {
                        player_calledDashEvents.SetValue(self, false);
                        return false;
                    }
                }
#endif
            foreach (GenericCustomBooster b in self.Scene.Tracker.SafeGetEntities<GenericCustomBooster>()) {
                if (b.StartedBoosting /* && b.CollideCheck(self)*/) {
                    b.PlayerBoosted(self, self.DashDir);
                    return false;
                }

                if (b.BoostingPlayer && GetBoosterThatIsBoostingPlayer(self) == b) {
                    return false;
                }
            }

            return true;
        }

        static void modBoosterState(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcI4(Player.StBoost) && instr.Previous.MatchCallvirt<StateMachine>("get_State"))) {
                cursor.Emit(OpCodes.Ldarg_0); // this
                cursor.EmitCall(FrostModule.GetBoosterState);
            }
        }
        #endregion

        public bool BoostingPlayer { get; private set; }
        public string reappearSfx;
        public string enterSfx;
        public string boostSfx;
        public string endSfx;
        public float BoostTime;
        public Color ParticleColor;
        public Color OutlineColor;
        public bool Red;
        public float RespawnTime;
        public bool PreserveSpeed;
        /// <summary>
        /// Set to -1 to refill dashes to dash cap (default)
        /// Set to -2 to not touch dash count at all
        /// </summary>
        public int DashRecovery;

        public bool StaminaRecovery;

        /// <summary>
        /// Speed at which the player entered the booster
        /// </summary>
        public Vector2 EnterSpeed;

        // respawnTime = 0 would already make the booster not respawn in earlier versions, so it will stay that way, even if it could be unintuitive
        public bool ShouldRespawn => RespawnTime > 0f;

        public readonly RedBoostDashOutModes RedBoostDashOutMode;

        public readonly string Directory;

        public GenericCustomBooster(EntityData data, Vector2 offset) : base(data.Position + offset) {
            LoadIfNeeded();

            Depth = -8500;
            Collider = data.Collider("hitbox") ?? new Circle(10f, 0f, 2f);
            Directory = data.Attr("directory", "objects/FrostHelper/blueBooster/");
            sprite = new Sprite(GFX.Game, Directory) {
                Visible = true
            };
            sprite.CenterOrigin();
            sprite.Justify = new Vector2(0.5f, 0.5f);
            sprite.AddLoop("loop", "booster", 0.1f, 0, 1, 2, 3, 4);
            sprite.AddLoop("inside", "booster", 0.1f, 5, 6, 7, 8);
            sprite.AddLoop("spin", "booster", 0.06f, 18, 19, 20, 21, 22, 23, 24, 25);
            sprite.Add("pop", "booster", 0.08f, 9, 10, 11, 12, 13, 14, 15, 16, 17);
            sprite.Play("loop", false);
            Add(sprite);

            Add(new PlayerCollider(OnPlayer, null, null));
            Add(light = new VertexLight(Color.White, 1f, 16, 32));
            Add(bloom = new BloomPoint(0.1f, 16f));
            Add(wiggler = Wiggler.Create(0.5f, 4f, f => {
                sprite.Scale = Vector2.One * (1f + f * 0.25f);
            }, false, false));
            Add(dashRoutine = new Coroutine(false));

            Add(dashListener = new DashListener());
            dashListener.OnDash = OnPlayerDashed;

            Add(new MirrorReflection());
            Add(loopingSfx = new SoundSource());

            Red = data.Bool("red", false);
            particleType = Red ? Booster.P_BurstRed : Booster.P_Burst;

            RespawnTime = data.Float("respawnTime", 1f);
            BoostTime = data.Float("boostTime", 0.3f);
            ParticleColor = ColorHelper.GetColor(data.Attr("particleColor", "Yellow"));
            reappearSfx = data.Attr("reappearSfx", "event:/game/04_cliffside/greenbooster_reappear");
            enterSfx = data.Attr("enterSfx", "event:/game/04_cliffside/greenbooster_enter");
            boostSfx = data.Attr("boostSfx", "event:/game/04_cliffside/greenbooster_dash");
            endSfx = data.Attr("releaseSfx", "event:/game/04_cliffside/greenbooster_end");
            DashRecovery = data.Int("dashes", -1);
            PreserveSpeed = data.Bool("preserveSpeed", false);
            OutlineColor = data.GetColor("outlineColor", "000000");
            RedBoostDashOutMode = data.Enum("redBoostDashOutMode", RedBoostDashOutModes.Default);
            StaminaRecovery = data.Bool("staminaRecovery", true);
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            string outlinePath = $"{Directory.AsSpan().TrimEnd('/')}/outline";
            Image image = new Image(GFX.Game[GFX.Game.Has(outlinePath) ? outlinePath : "objects/booster/outline"]);
            image.CenterOrigin();
            image.Color = Color.White * 0.75f;
            outline = new Entity(Position) {
                Depth = 8999,
                Visible = false
            };
            outline.Add(image);
            outline.Add(new MirrorReflection());
            scene.Add(outline);
        }

        public void Appear() {
            Audio.Play(reappearSfx, Position);
            sprite.Play("appear", false, false);
            wiggler.Start();
            Visible = true;
            AppearParticles();
        }

        private void AppearParticles() {
            ParticleSystem particlesBG = SceneAs<Level>().ParticlesBG;
            for (int i = 0; i < 360; i += 30) {
                particlesBG.Emit(Red ? Booster.P_RedAppear : Booster.P_Appear, 1, Center, Vector2.One * 2f, ParticleColor, i * 0.0174532924f);
            }
        }

        public virtual void OnPlayer(Player player) {
            if (respawnTimer <= 0f && cannotUseTimer <= 0f && !BoostingPlayer) {
                cannotUseTimer = 0.45f - 0.25f + BoostTime;

                Boost(player);

                Audio.Play(enterSfx, Position);
                wiggler.Start();
                sprite.Play("inside", false, false);
                sprite.FlipX = player.Facing == Facings.Left;
            }
        }

        public bool StartedBoosting;
        public virtual void Boost(Player player) {
            API.API.SetCustomBoostState(player, this);

            RedDash = Red;

            EnterSpeed = player.Speed;
            player.Speed = Vector2.Zero;
            player.boostRed = Red;
            player.boostTarget = Center;
            StartedBoosting = true;
        }

        public virtual float GetBoostSpeed() => PreserveSpeed ? EnterSpeed.BiggestAbsComponent() : 240f;


        public virtual bool CanFastbubble() => true;

        public void PlayerBoosted(Player player, Vector2 direction) {
            if (Red) {
                loopingSfx.Play("event:/game/05_mirror_temple/redbooster_move", null, 0f);
                loopingSfx.DisposeOnTransition = false;
            }
            StartedBoosting = false;
            Audio.Play(boostSfx, Position);
            BoostingPlayer = true;
            Tag = Tags.Persistent | Tags.TransitionUpdate;
            sprite.Play("spin", false, false);
            sprite.FlipX = player.Facing == Facings.Left;

            if (ShouldRespawn)
                outline.Visible = true;
            wiggler.Start();
            dashRoutine.Replace(BoostRoutine(player, direction));
        }

        private IEnumerator BoostRoutine(Player player, Vector2 dir) {
            float angle = (-dir).Angle();
            while ((player.StateMachine.State == Player.StDash || player.StateMachine.State == CustomRedBoostState) && BoostingPlayer) {
                if (player.Dead) {
                    PlayerDied();
                } else {
                    sprite.RenderPosition = player.Center + PlayerOffset;
                    loopingSfx.Position = sprite.Position;
                    if (Scene.OnInterval(0.02f)) {
                        (Scene as Level)!.ParticlesBG.Emit(particleType, 2, player.Center - dir * 3f + new Vector2(0f, -2f), new Vector2(3f, 3f), ParticleColor, angle);
                    }
                    yield return null;
                }

            }
            PlayerReleased();

            if (API.API.IsInCustomBoostState(player)) {
                sprite.Visible = false;
            }
            while (SceneAs<Level>().Transitioning) {
                yield return null;
            }
            Tag = 0;
            yield break;
        }

        public virtual void OnPlayerDashed(Vector2 direction) {
            if (BoostingPlayer) {
                BoostingPlayer = false;
            }
        }

        public virtual void PlayerReleased() {
            Audio.Play(endSfx, sprite.RenderPosition);
            sprite.Play("pop", false, false);

            if (!ShouldRespawn) {
                sprite.OnLastFrame += (s) => {
                    RemoveSelf();
                };
                Collidable = false;
                bloom.Visible = false;
                light.Visible = false;
            }

            respawnTimer = RespawnTime;

            cannotUseTimer = 0f;
            
            BoostingPlayer = false;
            wiggler.Stop();
            loopingSfx.Stop(true);
        }

        public virtual void PlayerDied() {
            if (BoostingPlayer) {
                PlayerReleased();
                dashRoutine.Active = false;
                Tag = 0;
            }
        }

        public virtual void Respawn() {
            Audio.Play(reappearSfx, Position);
            sprite.Position = Vector2.Zero;
            sprite.Play("loop", true, false);
            wiggler.Start();
            sprite.Visible = true;
            outline.Visible = false;
            AppearParticles();
        }

        public override void Update() {
            base.Update();

            if (cannotUseTimer > 0f) {
                cannotUseTimer -= Engine.DeltaTime;
            }

            if (ShouldRespawn && respawnTimer > 0f) {
                respawnTimer -= Engine.DeltaTime;
                if (respawnTimer <= 0f) {
                    Respawn();
                }
            }

            Player player = Scene.Tracker.GetNearestEntity<Player>(Position);

            if (!dashRoutine.Active && respawnTimer <= 0f && Collidable) {
                Vector2 target = Vector2.Zero;

                if (player != null && CollideCheck(player)) {
                    target = player.Center + PlayerOffset - Position;
                }
                sprite.Position = Calc.Approach(sprite.Position, target, 80f * Engine.DeltaTime);
            }

            //if (player != null && GetBoosterThatIsBoostingPlayer(player) == this && BoostTime > 0f) {
                //sprite.Position = player.Center + Booster.playerOffset - Position;

                // if the player is far away, render the outline because clearly the bubble got moved
                //if (Vector2.DistanceSquared(Position, player.Center) > 16f * 16f)
                //    outline.Visible = true;
            //}

            if (sprite.CurrentAnimationID == "inside" && !BoostingPlayer && (player is null || !CollideCheck(player))) {
                sprite.Play("loop", false, false);
            }
        }

        public override void Render() {
            Vector2 position = sprite.Position;
            sprite.Position = position.Floor();
            var lastOutlineVisible = outline.Visible;

            if (sprite.Visible && sprite.CurrentAnimationID != "pop") {
                if (OutlineColor.A > 0)
                    sprite.DrawOutline(OutlineColor, 1);
                outline.Visible = false;
            }
            base.Render();
            sprite.Position = position;
            outline.Visible = lastOutlineVisible;
        }

        public virtual void HandleDashRefill(Player player) {
            switch (DashRecovery) {
                case -1:
                    player.RefillDash();
                    break;
                case -2:
                    // no recovery
                    break;
                case < -2:
                    NotificationHelper.Notify($"Invalid value for the 'dashes' property: {DashRecovery}.\nExpected a value >= -2.\nReport this to the mapmaker!");
                    break;
                default:
                    player.Dashes = DashRecovery;
                    break;
            }
        }

        public virtual void HandleBoostBegin(Player player) {
            Level level = player.SceneAs<Level>();
            bool doNotDropTheo = false;
            if (level != null) {
                MapMetaModeProperties meta = level.Session.MapData.Meta;
                doNotDropTheo = (meta != null) && meta.TheoInBubble.GetValueOrDefault();
            }

            HandleDashRefill(player);

            if (StaminaRecovery)
                player.RefillStamina();
            if (doNotDropTheo) {
                return;
            }
            player.Drop();
        }

        public virtual IEnumerator HandleBoostCoroutine(Player player) {
            if (BoostTime > 0.25f) {
                yield return BoostTime - 0.25f;
                Audio.Play(boostSfx, Position);
                Flash();
                yield return 0.25f;
            } else {
                yield return BoostTime;
            }

            player.StateMachine.State = ExitBoostState(player, this);
            yield break;
        }

        public virtual void OnBoostEnd(Player player) { }

        public virtual void Flash() {
        }

        public static ParticleType P_Burst => Booster.P_Burst;

        public static ParticleType P_BurstRed => Booster.P_BurstRed;

        public static ParticleType P_Appear => Booster.P_Appear;

        public static ParticleType P_RedAppear => Booster.P_RedAppear;

        public static Vector2 PlayerOffset => GravityHelperIntegration.InvertIfPlayerInverted(Booster.playerOffset);

        public Sprite sprite;

        public Entity outline;

        public Wiggler wiggler;

        public BloomPoint bloom;

        public VertexLight light;

        public Coroutine dashRoutine;

        public DashListener dashListener;

        public ParticleType particleType;

        public float respawnTimer;

        public float cannotUseTimer;

        public SoundSource loopingSfx;

        #region RedBoostState

        public static int CustomRedBoostState = int.MaxValue;
        public static void RedDashBegin(Entity e) {
            Player player = (e as Player)!;
            player.calledDashEvents = false;
            player.dashStartedOnGround = false;
            Celeste.Celeste.Freeze(0.05f);
            Dust.Burst(player.Position, (-player.DashDir).Angle(), 8, null);
            player.dashCooldownTimer = 0.2f;
            player.dashRefillCooldownTimer = 0.1f;
            player.StartedDashing = true;
            (player.Scene as Level)!.Displacement.AddBurst(player.Center, 0.5f, 0f, 80f, 0.666f, Ease.QuadOut, Ease.QuadOut);
            Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
            player.dashAttackTimer = 0.3f;
            player.gliderBoostTimer = 0.55f;
            player.DashDir = Vector2.Zero;
            player.Speed = Vector2.Zero;
            if (player is { onGround: false, Ducking: true, CanUnDuck: true }) {
                player.Ducking = false;
            }

            player.DashAssistInit();
        }

        public static void RedDashEnd(Entity e) {
        }

        public static int RedDashUpdate(Entity e) {
            Player player = (e as Player)!;
            var booster = GetBoosterThatIsBoostingPlayer(player) ?? throw new UnreachableException();

            player.StartedDashing = false;
            bool ch9hub = false;
            player.gliderBoostTimer = 0.05f;

            // player.CanDash checks player.Dashes > 0. For blue boosters, the player might be in a booster with no dashes.
            // Because `EvenAtZeroDashes` has to make this check pass in some way,
            // the easiest way is to just change player dash count temporarily.
            var oldDashCount = player.Dashes;
            var dashOutMode = booster.RedBoostDashOutMode;
            player.Dashes = dashOutMode switch {
                RedBoostDashOutModes.Default => player.Dashes,
                RedBoostDashOutModes.EvenAtZeroDashes => 1,
                RedBoostDashOutModes.Never => 0,
                _ => throw new UnreachableException(),
            };
            var canDashOut = player.CanDash;
            player.Dashes = oldDashCount;
            
            if (canDashOut) {
                foreach (GenericCustomBooster b in player.Scene.Tracker.SafeGetEntities<GenericCustomBooster>()) {
                    if (b.BoostingPlayer) {
                        b.BoostingPlayer = false;
                        break;
                    }
                }
                return player.StartDash();
            }

            if (player.DashDir.Y == 0f) {
                // check whether the player is inverted
                bool playerInverted = GravityHelperIntegration.IsPlayerInverted?.Invoke() ?? false;
                // loop on all jumpthrus
                foreach (Entity entity in player.Scene.Tracker.SafeGetEntities<JumpThru>()) {
                    JumpThru jumpThru = (JumpThru) entity;

                    var offset = playerInverted ? jumpThru.Bottom - player.Top : player.Bottom - jumpThru.Top;
                    if (player.CollideCheck(jumpThru) && offset <= 6f) {
                        player.MoveVExact((int) -offset, null, null);
                    }
                }
                if (player.CanUnDuck && Input.Jump.Pressed && player.jumpGraceTimer > 0f && !ch9hub) {
                    player.SuperJump();
                    return 0;
                }
            }

            if (!ch9hub) {
                if (player.SuperWallJumpAngleCheck) {
                    if (Input.Jump.Pressed && player.CanUnDuck) {
                        if (player.WallJumpCheck(1)) {
                            player.SuperWallJump(-1);
                            return 0;
                        }
                        if (player.WallJumpCheck(-1)) {
                            player.SuperWallJump(1);
                            return 0;
                        }
                    }
                } else if (Input.Jump.Pressed && player.CanUnDuck) {
                    if (player.WallJumpCheck(1)) {
                        if (player.Facing == Facings.Right && Input.Grab.Check && player.Stamina > 0f && player.Holding == null && !ClimbBlocker.Check(player.Scene, player, player.Position + Vector2.UnitX * 3f)) {
                            player.ClimbJump();
                        } else {
                            player.WallJump(-1);
                        }
                        return 0;
                    }
                    if (player.WallJumpCheck(-1)) {
                        if (player.Facing == Facings.Left && Input.Grab.Check && player.Stamina > 0f && player.Holding == null && !ClimbBlocker.Check(player.Scene, player, player.Position + Vector2.UnitX * -3f)) {
                            player.ClimbJump();
                        } else {
                            player.WallJump(1);
                        }
                        return 0;
                    }
                }
            }
            return CustomRedBoostState;//5;
        }

        public static IEnumerator RedDashCoroutine(Entity e) {
            Player player = (e as Player)!;

            yield return null;
            player.Speed = player.CorrectDashPrecision(player.lastAim) * ChangeDashSpeedOnce.GetDashSpeed(240f);
            player.gliderBoostDir = player.DashDir = player.lastAim;
            player.SceneAs<Level>().DirectionalShake(player.DashDir, 0.2f);
            if (player.DashDir.X != 0f) {
                player.Facing = (Facings) Math.Sign(player.DashDir.X);
            }
            player.CallDashEvents();
            yield break;
        }
        #endregion

        #region BoostState

        public static int CustomBoostState = int.MaxValue;
        public static bool RedDash;

        public static int ExitBoostState(Player player, GenericCustomBooster booster) {
            float boostSpeed = booster.GetBoostSpeed();
            ChangeDashSpeedOnce.ChangeNextDashSpeed(boostSpeed);
            if (RedDash) {
                return CustomRedBoostState;
            } else {
                ChangeDashSpeedOnce.ChangeNextSuperJumpSpeed(boostSpeed + 20f);
                return Player.StDash;
            }
        }

        public static void BoostBegin(Entity e) {
            Player player = (e as Player)!;
            if (GetBoosterThatIsBoostingPlayer(player) is { } booster) {
                booster.HandleBoostBegin(player);
                OnBoostBegin?.Invoke(player, booster);
            }
        }

        // Exposed via Api
        public static event Action<Player, Entity>? OnBoostBegin;

        public static int BoostUpdate(Entity e) {
            Player player = (e as Player)!;
            var booster = GetBoosterThatIsBoostingPlayer(player);

            Vector2 value = Input.Aim.Value * 3f;
            Vector2 vector = Calc.Approach(player.ExactPosition, player.boostTarget - player.Collider.Center + value, 80f * Engine.DeltaTime);

            GravityHelperIntegration.BeginOverride?.Invoke();
            player.MoveToX(vector.X, null);
            player.MoveToY(vector.Y, null);
            GravityHelperIntegration.EndOverride?.Invoke();

            if (booster is { } && (Input.Dash.Pressed || Input.CrouchDashPressed) && booster.CanFastbubble()) {
                player.demoDashed = Input.CrouchDashPressed;
                Input.Dash.ConsumePress();
                Input.CrouchDash.ConsumePress();
                return ExitBoostState(player, booster);
            }

            return CustomBoostState;
        }

        public static void BoostEnd(Entity e) {
            Player player = (e as Player)!;
            Vector2 boostTarget = player.boostTarget;
            Vector2 vector = (boostTarget - player.Collider.Center).Floor();

            GravityHelperIntegration.BeginOverride?.Invoke();
            player.MoveToX(vector.X, null);
            player.MoveToY(vector.Y, null);
            GravityHelperIntegration.EndOverride?.Invoke();

            GetBoosterThatIsBoostingPlayer(player)?.OnBoostEnd(player);
        }

        public static IEnumerator BoostCoroutine(Entity e) {
            Player player = (e as Player)!;
            var booster = GetBoosterThatIsBoostingPlayer(player) ?? throw new NullReferenceException("booster");

            return booster.HandleBoostCoroutine(player);
        }

        public static GenericCustomBooster? GetBoosterThatIsBoostingPlayer(Player e) {
            return e.GetDynamicDataAttached<GenericCustomBooster>();
        }

        #endregion
    }

    public enum RedBoostDashOutModes {
        Default,
        EvenAtZeroDashes,
        Never,
    }
}
