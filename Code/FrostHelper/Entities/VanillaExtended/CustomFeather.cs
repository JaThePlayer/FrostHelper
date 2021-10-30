using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System;
using System.Collections;
using System.Reflection;

namespace FrostHelper {
    [Tracked(false)]
    [CustomEntity("FrostHelper/CustomFeather")]
    public class CustomFeather : Entity {
        #region Hooks
        [OnLoad]
        public static void LoadHooks() {
            IL.Celeste.Player.UpdateHair += modFeatherState;
            IL.Celeste.Player.UpdateSprite += modFeatherState;
            IL.Celeste.Player.OnCollideH += modFeatherState;
            IL.Celeste.Player.OnCollideV += modFeatherState;
            IL.Celeste.Player.Render += modFeatherState;
            IL.Celeste.Player.BeforeDownTransition += modFeatherState;
            IL.Celeste.Player.BeforeUpTransition += modFeatherState;
            IL.Celeste.Player.HiccupJump += modFeatherState;

            FrostModule.RegisterILHook(new ILHook(typeof(Player).GetMethod("orig_Update", BindingFlags.Instance | BindingFlags.Public), modFeatherState));
            FrostModule.RegisterILHook(new ILHook(typeof(Player).GetMethod("orig_UpdateSprite", BindingFlags.Instance | BindingFlags.NonPublic), modFeatherState));
        }

        [OnUnload]
        public static void UnloadHooks() {
            IL.Celeste.Player.UpdateHair -= modFeatherState;
            IL.Celeste.Player.UpdateSprite -= modFeatherState;
            IL.Celeste.Player.OnCollideH -= modFeatherState;
            IL.Celeste.Player.OnCollideV -= modFeatherState;
            IL.Celeste.Player.Render -= modFeatherState;
            IL.Celeste.Player.BeforeDownTransition -= modFeatherState;
            IL.Celeste.Player.BeforeUpTransition -= modFeatherState;
            IL.Celeste.Player.HiccupJump -= modFeatherState;
        }

        static void modFeatherState(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcI4(19) && instr.Previous.MatchCallvirt<StateMachine>("get_State"))) {
                cursor.Emit(OpCodes.Ldarg_0); // this
                cursor.EmitDelegate<Func<int, Player, int>>(FrostModule.GetFeatherState);
            }
            cursor.Index = 0;
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcI4(5) && instr.Previous.MatchCallvirt<StateMachine>("get_State"))) {
                cursor.Emit(OpCodes.Ldarg_0); // this
                cursor.EmitDelegate<Func<int, Player, int>>(FrostModule.GetRedDashState);
            }
        }
        #endregion

        #region CustomState
        public static int CustomFeatherState;
        public static MethodInfo player_StarFlyReturnToNormalHitbox = typeof(Player).GetMethod("StarFlyReturnToNormalHitbox", BindingFlags.Instance | BindingFlags.NonPublic);
        public static MethodInfo player_WallJump = typeof(Player).GetMethod("WallJump", BindingFlags.Instance | BindingFlags.NonPublic);
        public static MethodInfo player_WallJumpCheck = typeof(Player).GetMethod("WallJumpCheck", BindingFlags.Instance | BindingFlags.NonPublic);

        public static void CustomFeatherBegin(Entity e) {
            Player player = e as Player;
            DynData<Player> data = new DynData<Player>(player);
            CustomFeather feather = (CustomFeather) data["fh.customFeather"];
            player.Sprite.Play("startStarFly", false, false);
            data["starFlyTransforming"] = true;
            data["starFlyTimer"] = feather.FlyTime;
            data["starFlySpeedLerp"] = 0f;
            data["jumpGraceTimer"] = 0f;
            BloomPoint starFlyBloom = (BloomPoint) data["starFlyBloom"];
            if (starFlyBloom == null) {
                player.Add(starFlyBloom = new BloomPoint(new Vector2(0f, -6f), 0f, 16f));
            }
            starFlyBloom.Visible = true;
            starFlyBloom.Alpha = 0f;
            data["starFlyBloom"] = starFlyBloom;
            player.Collider = (Hitbox) data["starFlyHitbox"];
            data["hurtbox"] = data["starFlyHurtbox"];
            SoundSource starFlyLoopSfx = (SoundSource) data["starFlyLoopSfx"];
            SoundSource starFlyWarningSfx = (SoundSource) data["starFlyWarningSfx"];
            if (starFlyLoopSfx == null) {
                player.Add(starFlyLoopSfx = new SoundSource());
                starFlyLoopSfx.DisposeOnTransition = false;
                player.Add(starFlyWarningSfx = new SoundSource());
                starFlyWarningSfx.DisposeOnTransition = false;
            }
            starFlyLoopSfx.Play("event:/game/06_reflection/feather_state_loop", "feather_speed", 1f);
            starFlyWarningSfx.Stop(true);
            data["starFlyLoopSfx"] = starFlyLoopSfx;
            data["starFlyWarningSfx"] = starFlyWarningSfx;
        }
        public static void CustomFeatherEnd(Entity e) {
            Player player = e as Player;
            DynData<Player> data = new DynData<Player>(player);
            CustomFeather feather = (CustomFeather) data["fh.customFeather"];
            player.Play("event:/game/06_reflection/feather_state_end", null, 0f);
            ((SoundSource) data["starFlyWarningSfx"]).Stop(true);
            ((SoundSource) data["starFlyLoopSfx"]).Stop(true);
            player.Hair.DrawPlayerSpriteOutline = false;
            player.Sprite.Color = Color.White;
            player.SceneAs<Level>().Displacement.AddBurst(player.Center, 0.25f, 8f, 32f, 1f, null, null);
            ((BloomPoint) data["starFlyBloom"]).Visible = false;
            player.Sprite.HairCount = (int) data["startHairCount"];
            player_StarFlyReturnToNormalHitbox.Invoke(player, null);
            bool flag = player.StateMachine.State != 2;
            if (flag) {
                player.SceneAs<Level>().Particles.Emit(feather.P_Boost, 12, player.Center, Vector2.One * 4f, (-player.Speed).Angle());
            }
        }
        public static IEnumerator CustomFeatherCoroutine(Entity e) {
            Player player = e as Player;
            DynData<Player> data = new DynData<Player>(player);
            CustomFeather feather = (CustomFeather) data["fh.customFeather"];
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
            data["starFlyTransforming"] = false;
            data["starFlyTimer"] = feather.FlyTime;
            player.RefillDash();
            player.RefillStamina();
            Vector2 dir = Input.Aim.Value;
            bool flag = dir == Vector2.Zero;
            if (flag) {
                dir = Vector2.UnitX * (float) player.Facing;
            }
            player.Speed = dir * 250f;
            data["starFlyLastDir"] = dir;
            player.SceneAs<Level>().Particles.Emit(feather.P_Boost, 12, player.Center, Vector2.One * 4f, feather.FlyColor, (-dir).Angle());
            Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
            player.SceneAs<Level>().DirectionalShake((Vector2) data["starFlyLastDir"], 0.3f);
            while ((float) data["starFlyTimer"] > 0.5f) {
                yield return null;
            }
            ((SoundSource) data["starFlyWarningSfx"]).Play("event:/game/06_reflection/feather_state_warning", null, 0f);
            yield break;
        }

        public static int StarFlyUpdate(Entity e) {
            Player player = e as Player;
            Level level = player.SceneAs<Level>();
            DynData<Player> data = new DynData<Player>(player);
            BloomPoint bloomPoint = (BloomPoint) data["starFlyBloom"];
            CustomFeather feather = (CustomFeather) data["fh.customFeather"];
            // 2f -> StarFlyTime
            float StarFlyTime = feather.FlyTime;
            bloomPoint.Alpha = Calc.Approach(bloomPoint.Alpha, 0.7f, Engine.DeltaTime * StarFlyTime);
            data["starFlyBloom"] = bloomPoint;
            Input.Rumble(RumbleStrength.Climb, RumbleLength.Short);
            if ((bool) data["starFlyTransforming"]) {
                player.Speed = Calc.Approach(player.Speed, Vector2.Zero, 1000f * Engine.DeltaTime);
            } else {
                Vector2 aimValue = Input.Aim.Value;
                bool notAiming = false;
                bool flag3 = aimValue == Vector2.Zero;
                if (flag3) {
                    notAiming = true;
                    aimValue = (Vector2) data["starFlyLastDir"];
                }
                Vector2 lastSpeed = player.Speed.SafeNormalize(Vector2.Zero);
                bool flag4 = lastSpeed == Vector2.Zero;
                if (flag4) {
                    lastSpeed = aimValue;
                } else {
                    lastSpeed = lastSpeed.RotateTowards(aimValue.Angle(), 5.58505344f * Engine.DeltaTime);
                }
                data["starFlyLastDir"] = lastSpeed;
                float target;
                if (notAiming) {
                    data["starFlySpeedLerp"] = 0f;
                    target = feather.NeutralSpeed; // was 91f
                } else {
                    bool flag6 = lastSpeed != Vector2.Zero && Vector2.Dot(lastSpeed, aimValue) >= 0.45f;
                    if (flag6) {
                        data["starFlySpeedLerp"] = Calc.Approach((float) data["starFlySpeedLerp"], 1f, Engine.DeltaTime / 1f);
                        target = MathHelper.Lerp(feather.LowSpeed, feather.MaxSpeed, (float) data["starFlySpeedLerp"]);
                    } else {
                        data["starFlySpeedLerp"] = 0f;
                        target = 140f;
                    }
                }
                SoundSource ss = (SoundSource) data["starFlyLoopSfx"];
                ss.Param("feather_speed", notAiming ? 0 : 1);
                data["starFlyLoopSfx"] = ss;

                float num = player.Speed.Length();
                num = Calc.Approach(num, target, 1000f * Engine.DeltaTime);
                player.Speed = lastSpeed * num;
                bool flag7 = level.OnInterval(0.02f);
                if (flag7) {
                    level.Particles.Emit(feather.P_Flying, 1, player.Center, Vector2.One * 2f, feather.FlyColor, (-player.Speed).Angle());
                }
                bool pressed = Input.Jump.Pressed;
                if (pressed) {
                    bool flag8 = player.OnGround(3);
                    if (flag8) {
                        player.Jump(true, true);
                        return 0;
                    }
                    bool flag9 = (bool) player_WallJumpCheck.Invoke(player, new object[] { -1 });
                    if (flag9) {
                        player_WallJump.Invoke(player, new object[] { 1 });
                        return 0;
                    }
                    bool flag10 = (bool) player_WallJumpCheck.Invoke(player, new object[] { 1 });
                    if (flag10) {
                        player_WallJump.Invoke(player, new object[] { -1 });
                        return 0;
                    }
                }
                bool check = Input.Grab.Check;
                if (check) {
                    bool flag11 = false;
                    int dir = 0;
                    bool flag12 = Input.MoveX.Value != -1 && player.ClimbCheck(1, 0);
                    if (flag12) {
                        player.Facing = Facings.Right;
                        dir = 1;
                        flag11 = true;
                    } else {
                        bool flag13 = Input.MoveX.Value != 1 && player.ClimbCheck(-1, 0);
                        if (flag13) {
                            player.Facing = Facings.Left;
                            dir = -1;
                            flag11 = true;
                        }
                    }
                    bool flag14 = flag11;
                    if (flag14) {
                        bool noGrabbing = Celeste.SaveData.Instance.Assists.NoGrabbing;
                        if (noGrabbing) {
                            player.Speed = Vector2.Zero;
                            player.ClimbTrigger(dir);
                            return 0;
                        }
                        return 1;
                    }
                }
                bool canDash = player.CanDash;
                if (canDash) {
                    return player.StartDash();
                }
                float starFlyTimer = (float) data["starFlyTimer"];
                starFlyTimer -= Engine.DeltaTime;
                data["starFlyTimer"] = starFlyTimer;
                bool flag15 = starFlyTimer <= 0f;
                if (flag15) {
                    bool flag16 = Input.MoveY.Value == -1;
                    if (flag16) {
                        player.Speed.Y = -100f;
                    }
                    bool flag17 = Input.MoveY.Value < 1;
                    if (flag17) {
                        data["varJumpSpeed"] = player.Speed.Y;
                        player.AutoJump = true;
                        player.AutoJumpTimer = 0f;
                        data["varJumpTimer"] = 0.2f;
                    }
                    bool flag18 = player.Speed.Y > 0f;
                    if (flag18) {
                        player.Speed.Y = 0f;
                    }
                    bool flag19 = Math.Abs(player.Speed.X) > 140f;
                    if (flag19) {
                        player.Speed.X = 140f * Math.Sign(player.Speed.X);
                    }
                    Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
                    return 0;
                }
                bool flag20 = starFlyTimer < 0.5f && player.Scene.OnInterval(0.05f);
                if (flag20) {
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

        public CustomFeather(EntityData data, Vector2 offset) : base(data.Position + offset) {
            shielded = data.Bool("shielded", false);
            singleUse = data.Bool("singleUse", false);
            RespawnTime = data.Float("respawnTime", 3f);
            FlyColor = ColorHelper.GetColor(data.Attr("flyColor", "ffd65c"));
            FlyTime = data.Float("flyTime", 2f);
            MaxSpeed = data.Float("maxSpeed", 190f);
            LowSpeed = data.Float("lowSpeed", 140f);
            NeutralSpeed = data.Float("neutralSpeed", 91f);
            Collider = new Hitbox(20f, 20f, -10f, -10f);
            Add(new PlayerCollider(new Action<Player>(OnPlayer), null, null));
            string path = data.Attr("spritePath", "objects/flyFeather/").Replace('\\', '/');
            if (path[path.Length - 1] != '/') {
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
            Add(outline = new Image(GFX.Game[data.Attr("outlinePath", "objects/flyFeather/outline")]));
            outline.CenterOrigin();
            outline.Visible = false;
            shieldRadiusWiggle = Wiggler.Create(0.5f, 4f, null, false, false);
            Add(shieldRadiusWiggle);
            moveWiggle = Wiggler.Create(0.8f, 2f, null, false, false);
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
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            level = SceneAs<Level>();
        }

        public override void Update() {
            base.Update();

            if (respawnTimer > 0f) {
                respawnTimer -= Engine.DeltaTime;
                bool flag2 = respawnTimer <= 0f;
                if (flag2) {
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
            bool flag = shielded && !player.DashAttacking;
            if (flag) {
                player.PointBounce(Center);
                moveWiggle.Start();
                shieldRadiusWiggle.Start();
                moveWiggleDir = (Center - player.Center).SafeNormalize(Vector2.UnitY);
                Audio.Play("event:/game/06_reflection/feather_bubble_bounce", Position);
                Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
            } else {
                if (StartStarFly(player)) {
                    if (player.StateMachine.State != CustomFeatherState && player.StateMachine.State != Player.StStarFly) {
                        Audio.Play(shielded ? "event:/game/06_reflection/feather_bubble_get" : "event:/game/06_reflection/feather_get", Position);
                    } else {
                        Audio.Play(shielded ? "event:/game/06_reflection/feather_bubble_renew" : "event:/game/06_reflection/feather_renew", Position);
                    }
                    Collidable = false;
                    Add(new Coroutine(CollectRoutine(player, speed), true));
                    bool flag5 = !singleUse;
                    if (flag5) {
                        outline.Visible = true;
                        respawnTimer = RespawnTime;
                    }
                }
            }
        }

        public Color FlyColor;
        public float FlyTime;

        public bool StartStarFly(Player player) {
            DynData<Player> data = new DynData<Player>(player);
            player.RefillStamina();
            bool result;
            if (player.StateMachine.State == Player.StReflectionFall) {
                result = false;
            } else {
                data["fh.customFeather"] = this;
                if (player.StateMachine.State == CustomFeatherState) {
                    data["starFlyTimer"] = FlyTime;
                    player.Sprite.Color = FlyColor;
                    Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
                } else {
                    player.StateMachine.State = CustomFeatherState;
                }
                result = true;
            }
            return result;
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
}
