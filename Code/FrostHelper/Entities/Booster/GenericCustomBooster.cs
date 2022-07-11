using Celeste.Mod.Meta;
using FrostHelper.ModIntegration;

#if PLAYERSTATEHELPER
using Celeste.Mod.PlayerStateHelper.API;
using FrostHelper.CustomStates;
#endif

namespace FrostHelper.Entities.Boosters {
    [Tracked(true)]
    public class GenericCustomBooster : Entity {
        #region Hooks
        [OnLoad]
        public static void Load() {
            IL.Celeste.Player.OnBoundsH += modRedDashState;
            IL.Celeste.Player.OnBoundsV += modRedDashState;
            IL.Celeste.DashBlock.OnDashed += modRedDashStateDashBlock;
            FrostModule.RegisterILHook(new ILHook(typeof(Player).GetProperty("DashAttacking", BindingFlags.Instance | BindingFlags.Public).GetGetMethod(true), modRedDashState));
        }

        [OnUnload]
        public static void Unload() {
            IL.Celeste.Player.OnBoundsH -= modRedDashState;
            IL.Celeste.Player.OnBoundsV -= modRedDashState;
            IL.Celeste.DashBlock.OnDashed -= modRedDashStateDashBlock;
        }

        static void modRedDashState(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcI4(5) && instr.Previous.MatchCallvirt<StateMachine>("get_State"))) {
                cursor.Emit(OpCodes.Ldarg_0); // this
                cursor.EmitDelegate<Func<int, Player, int>>(FrostModule.GetRedDashState);
            }
        }

        static void modRedDashStateDashBlock(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcI4(5) && instr.Previous.MatchCallvirt<StateMachine>("get_State"))) {
                cursor.Emit(OpCodes.Ldarg_1); // arg 1
                cursor.EmitDelegate<Func<int, Player, int>>(FrostModule.GetRedDashState);
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
        public bool Red;
        public float RespawnTime;
        public bool PreserveSpeed;
        /// <summary>
        /// Set to -1 to refill dashes to dash cap (default)
        /// </summary>
        public int DashRecovery;

        /// <summary>
        /// Speed at which the player entered the booster
        /// </summary>
        public Vector2 EnterSpeed;

        public GenericCustomBooster(EntityData data, Vector2 offset) : base(data.Position + offset) {
            Depth = -8500;
            Collider = new Circle(10f, 0f, 2f);
            sprite = new Sprite(GFX.Game, data.Attr("directory", "objects/FrostHelper/blueBooster/")) {
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

            Add(new PlayerCollider(new Action<Player>(OnPlayer), null, null));
            Add(light = new VertexLight(Color.White, 1f, 16, 32));
            Add(bloom = new BloomPoint(0.1f, 16f));
            Add(wiggler = Wiggler.Create(0.5f, 4f, delegate (float f) {
                sprite.Scale = Vector2.One * (1f + f * 0.25f);
            }, false, false));
            Add(dashRoutine = new Coroutine(false));

            Add(dashListener = new DashListener());
            dashListener.OnDash = new Action<Vector2>(OnPlayerDashed);

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
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            Image image = new Image(GFX.Game["objects/booster/outline"]);
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
            player.SetValue("boostRed", Red);
            FrostModule.player_boostTarget.SetValue(player, Center);
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
            cannotUseTimer = 0f;
            respawnTimer = RespawnTime;
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

            if (respawnTimer > 0f) {
                respawnTimer -= Engine.DeltaTime;
                if (respawnTimer <= 0f) {
                    Respawn();
                }
            }

            Player player = Scene.Tracker.GetNearestEntity<Player>(Position);

            if (!dashRoutine.Active && respawnTimer <= 0f) {
                Vector2 target = Vector2.Zero;

                if (player != null && CollideCheck(player)) {
                    target = player.Center + PlayerOffset - Position;
                }
                sprite.Position = Calc.Approach(sprite.Position, target, 80f * Engine.DeltaTime);
            }

            if (player != null && GetBoosterThatIsBoostingPlayer(player) == this && BoostTime > 0f) {
                //sprite.Position = player.Center + Booster.playerOffset - Position;

                // if the player is far away, render the outline because clearly the bubble got moved
                if (Vector2.DistanceSquared(Position, player.Center) > 16f * 16f)
                    outline.Visible = true;
            }

            if (sprite.CurrentAnimationID == "inside" && !BoostingPlayer && (player is null || !CollideCheck(player))) {
                sprite.Play("loop", false, false);
            }
        }

        public override void Render() {
            Vector2 position = sprite.Position;
            sprite.Position = position.Floor();

            if (sprite.CurrentAnimationID != "pop" && sprite.Visible) {
                sprite.DrawOutline(1);
            }
            base.Render();
            sprite.Position = position;
        }

        public virtual void HandleDashRefill(Player player) {
            if (DashRecovery == -1) {
                player.RefillDash();
            } else {
                player.Dashes = DashRecovery;
            }
        }

        public virtual void HandleBoostBegin(Player player) {
            Level level = player.SceneAs<Level>();
            bool doNotDropTheo = false;
            if (level != null) {
                MapMetaModeProperties meta = level.Session.MapData.GetMeta();
                doNotDropTheo = (meta != null) && meta.TheoInBubble.GetValueOrDefault();
            }

            HandleDashRefill(player);

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
        public static MethodInfo Player_CallDashEvents = typeof(Player).GetMethod("CallDashEvents", BindingFlags.NonPublic | BindingFlags.Instance);
        public static MethodInfo Player_DashAssistInit = typeof(Player).GetMethod("DashAssistInit", BindingFlags.NonPublic | BindingFlags.Instance);
        public static MethodInfo Player_CorrectDashPrecision = typeof(Player).GetMethod("CorrectDashPrecision", BindingFlags.NonPublic | BindingFlags.Instance);
        public static MethodInfo Player_SuperJump = typeof(Player).GetMethod("SuperJump", BindingFlags.NonPublic | BindingFlags.Instance);
        public static MethodInfo Player_SuperWallJump = typeof(Player).GetMethod("SuperWallJump", BindingFlags.NonPublic | BindingFlags.Instance);
        public static MethodInfo Player_ClimbJump = typeof(Player).GetMethod("ClimbJump", BindingFlags.NonPublic | BindingFlags.Instance);
        public static MethodInfo player_WallJump = typeof(Player).GetMethod("WallJump", BindingFlags.Instance | BindingFlags.NonPublic);
        public static MethodInfo player_WallJumpCheck = typeof(Player).GetMethod("WallJumpCheck", BindingFlags.Instance | BindingFlags.NonPublic);

        public static int CustomRedBoostState;
        public static void RedDashBegin(Entity e) {
            Player player = (e as Player)!;
            DynData<Player> data = new DynData<Player>(player);
            data["calledDashEvents"] = false;
            data["dashStartedOnGround"] = false;
            Celeste.Celeste.Freeze(0.05f);
            Dust.Burst(player.Position, (-player.DashDir).Angle(), 8, null);
            data["dashCooldownTimer"] = 0.2f;
            data["dashRefillCooldownTimer"] = 0.1f;
            data["StartedDashing"] = true;
            (player.Scene as Level)!.Displacement.AddBurst(player.Center, 0.5f, 0f, 80f, 0.666f, Ease.QuadOut, Ease.QuadOut);
            Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
            data["dashAttackTimer"] = 0.3f;
            data["gliderBoostTimer"] = 0.55f;
            player.DashDir = Vector2.Zero;
            player.Speed = Vector2.Zero;
            if (!data.Get<bool>("onGround") && player.Ducking && player.CanUnDuck) {
                player.Ducking = false;
            }

            Player_DashAssistInit.Invoke(player, new object[] { });
        }

        public static void RedDashEnd(Entity e) {
        }

        public static int RedDashUpdate(Entity e) {
            Player player = (e as Player)!;
            DynData<Player> data = new DynData<Player>(player);

            data["StartedDashing"] = false;
            bool ch9hub = false;//this.LastBooster != null && this.LastBooster.Ch9HubTransition;
            data["gliderBoostTimer"] = 0.05f;
            if (player.CanDash) {
                foreach (GenericCustomBooster b in player.Scene.Tracker.GetEntities<GenericCustomBooster>()) {
                    if (b.BoostingPlayer) {
                        b.BoostingPlayer = false;
                        break;
                    }
                }
                return player.StartDash();
            }
            if (player.DashDir.Y == 0f) {
                foreach (Entity entity in player.Scene.Tracker.GetEntities<JumpThru>()) {
                    JumpThru jumpThru = (JumpThru) entity;
                    if (player.CollideCheck(jumpThru) && player.Bottom - jumpThru.Top <= 6f) {
                        player.MoveVExact((int) (jumpThru.Top - player.Bottom), null, null);
                    }
                }
                if (player.CanUnDuck && Input.Jump.Pressed && data.Get<float>("jumpGraceTimer") > 0f && !ch9hub) {
                    Player_SuperJump.Invoke(player, null);
                    return 0;
                }
            }
            if (!ch9hub) {
                if (data.Get<bool>("SuperWallJumpAngleCheck")) {
                    if (Input.Jump.Pressed && player.CanUnDuck) {
                        if ((bool) player_WallJumpCheck.Invoke(player, new object[] { 1 })) {
                            Player_SuperWallJump.Invoke(player, new object[] { -1 });
                            return 0;
                        }
                        if ((bool) player_WallJumpCheck.Invoke(player, new object[] { -1 })) {
                            Player_SuperWallJump.Invoke(player, new object[] { 1 });
                            return 0;
                        }
                    }
                } else if (Input.Jump.Pressed && player.CanUnDuck) {
                    if ((bool) player_WallJumpCheck.Invoke(player, new object[] { 1 })) {
                        if (player.Facing == Facings.Right && Input.Grab.Check && player.Stamina > 0f && player.Holding == null && !ClimbBlocker.Check(player.Scene, player, player.Position + Vector2.UnitX * 3f)) {
                            Player_ClimbJump.Invoke(player, null);
                        } else {
                            player_WallJump.Invoke(player, new object[] { -1 });
                        }
                        return 0;
                    }
                    if ((bool) player_WallJumpCheck.Invoke(player, new object[] { -1 })) {
                        if (player.Facing == Facings.Left && Input.Grab.Check && player.Stamina > 0f && player.Holding == null && !ClimbBlocker.Check(player.Scene, player, player.Position + Vector2.UnitX * -3f)) {
                            Player_ClimbJump.Invoke(player, null);
                        } else {
                            player_WallJump.Invoke(player, new object[] { 1 });
                        }
                        return 0;
                    }
                }
            }
            return CustomRedBoostState;//5;
        }

        public static IEnumerator RedDashCoroutine(Entity e) {
            Player player = (e as Player)!;
            DynData<Player> data = new DynData<Player>(player);

            yield return null;
            player.Speed = (Vector2) Player_CorrectDashPrecision.Invoke(player, new object[] { data.Get<Vector2>("lastAim") }) * ChangeDashSpeedOnce.GetDashSpeed(240f);
            data["gliderBoostDir"] = player.DashDir = data.Get<Vector2>("lastAim");
            player.SceneAs<Level>().DirectionalShake(player.DashDir, 0.2f);
            if (player.DashDir.X != 0f) {
                player.Facing = (Facings) Math.Sign(player.DashDir.X);
            }
            Player_CallDashEvents.Invoke(player, null);
            yield break;
        }
        #endregion

        #region BoostState

        public static int CustomBoostState;
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
            GetBoosterThatIsBoostingPlayer(player).HandleBoostBegin(player);
        }

        public static int BoostUpdate(Entity e) {
            Player player = (e as Player)!;
            var booster = GetBoosterThatIsBoostingPlayer(player);

            Vector2 boostTarget = GravityHelperIntegration.InvertIfPlayerInverted( (Vector2) FrostModule.player_boostTarget.GetValue(player));
            Vector2 value = Input.Aim.Value * 3f;
            Vector2 vector = Calc.Approach(player.ExactPosition, boostTarget - player.Collider.Center + value, 80f * Engine.DeltaTime);
            player.MoveToX(vector.X, null);
            player.MoveToY(vector.Y, null);

            if ((Input.Dash.Pressed || Input.CrouchDashPressed) && booster.CanFastbubble()) {
                player.SetValue("demoDashed", Input.CrouchDashPressed);
                Input.Dash.ConsumePress();
                Input.CrouchDash.ConsumePress();
                return ExitBoostState(player, booster);
            }

            return CustomBoostState;
        }

        public static void BoostEnd(Entity e) {
            Player player = (e as Player)!;
            Vector2 boostTarget = (Vector2) FrostModule.player_boostTarget.GetValue(player);
            Vector2 vector = (boostTarget - player.Collider.Center).Floor();
            player.MoveToX(vector.X, null);
            player.MoveToY(vector.Y, null);

            GetBoosterThatIsBoostingPlayer(player).OnBoostEnd(player);
            //new DynData<Player>(player).Set<GenericCustomBooster>("fh.customBooster", null!);
        }

        public static IEnumerator BoostCoroutine(Entity e) {
            Player player = (e as Player)!;
            GenericCustomBooster booster = GetBoosterThatIsBoostingPlayer(player);

            return booster.HandleBoostCoroutine(player);
        }

        public static GenericCustomBooster GetBoosterThatIsBoostingPlayer(Player e) {
            return new DynData<Player>(e).Get<GenericCustomBooster>("fh.customBooster");
        }

        #endregion
    }
}
