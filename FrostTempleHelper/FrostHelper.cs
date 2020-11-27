using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Celeste;
using Celeste.Mod;
using Celeste.Mod.Helpers;
using Celeste.Mod.Meta;
using FrostHelper;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

namespace FrostHelper
{
    public class FrostModule : EverestModule
    {
        public static SpriteBank SpriteBank;
        // Only one alive module instance can exist at any given time.
        public static FrostModule Instance;

        public FrostModule()
        {
            Instance = this;
        }
        // no save data needed
        public override Type SaveDataType => typeof(FrostHelperSaveData);
        public static FrostHelperSaveData SaveData => (FrostHelperSaveData)Instance._SaveData;

        // If you don't need to store any settings, => null
        public override Type SettingsType
        {
            get
            {
                return null;
            }
        }

        public override void PrepareMapDataProcessors(MapDataFixup context)
        {
            base.PrepareMapDataProcessors(context);

            context.Add<FrostMapDataProcessor>();
        }

        public override void LoadContent(bool firstLoad)
        {
            SpriteBank = new SpriteBank(GFX.Game, "Graphics/FrostHelper/CustomSprites.xml");
            BadelineChaserBlock.Load();
            BadelineChaserBlockActivator.Load();
        }

        // Set up any hooks, event handlers and your mod in general here.
        // Load runs before Celeste itself has initialized properly.
        public override void Load()
        {
            // Legacy entity creation (for back when we didn't have the CustomEntity attribute)
            Everest.Events.Level.OnLoadEntity += OnLoadEntity;
            // Custom Rising Lava integrations
            On.Celeste.Mod.Entities.LavaBlockerTrigger.Awake += LavaBlockerTrigger_Awake;
            On.Celeste.Mod.Entities.LavaBlockerTrigger.OnStay += LavaBlockerTrigger_OnStay;
            On.Celeste.Mod.Entities.LavaBlockerTrigger.OnLeave += LavaBlockerTrigger_OnLeave;
            // Lightning Color Trigger
            On.Celeste.LightningRenderer.Update += LightningRenderer_Update;
            On.Celeste.LightningRenderer.Awake += LightningRenderer_Awake;
            On.Celeste.LightningRenderer.Reset += LightningRenderer_Reset;
            // Register new states
            On.Celeste.Player.ctor += Player_ctor;

            // For custom Boosters
            On.Celeste.Player.CallDashEvents += Player_CallDashEvents;

            // For Custom Dream Blocks
            CustomDreamBlock.Load();
            CustomDreamBlockV2.Load();
            // Custom dream blocks and feathers
            On.Celeste.Player.UpdateSprite += Player_UpdateSprite;

            // custom feathers
            IL.Celeste.Player.UpdateHair += modFeatherState;
            IL.Celeste.Player.OnCollideH += modFeatherState;
            IL.Celeste.Player.OnCollideV += modFeatherState;
            //IL.Celeste.Player.Update += modFeatherState;
            playerUpdateHook = new ILHook(typeof(Player).GetMethod("orig_Update", BindingFlags.Instance | BindingFlags.Public), modFeatherState);
            IL.Celeste.Player.Render += modFeatherState;

            CustomSpinner.LoadHooks();
        }

        private void LightningRenderer_Reset(On.Celeste.LightningRenderer.orig_Reset orig, LightningRenderer self)
        {
            if (self.Scene is Level)
            {
                var session = (self.Scene as Level).Session;
                Color[] colors = new Color[2]
                {
                SessionHelper.ReadColorFromSession(session, "fh.lightningColorA", Color.White),
                SessionHelper.ReadColorFromSession(session, "fh.lightningColorB", Color.White)
                };
                if (colors[0] != Color.White)
                {
                    LightningColorTrigger.ChangeLightningColor(self, colors);
                }
            }
            orig(self);
        }

        

        ILHook playerUpdateHook;

        void modFeatherState(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcI4(19)))
            {
                cursor.Emit(OpCodes.Pop);
                cursor.EmitDelegate<Func<int>>(getFeatherState);
            }
        }

        static int getFeatherState()
        {
            if (Engine.Scene is Level)
                return StateGetPlayer().StateMachine.State == CustomFeatherState ? CustomFeatherState : 19;
            else
                return 19;
        }
        
        #region CustomDreamBlock

        public static int CustomDreamDashState;

        private static void Player_UpdateSprite(On.Celeste.Player.orig_UpdateSprite orig, Player self)
        {

            if (self.StateMachine.State == CustomDreamDashState)
            {
                if (self.Sprite.CurrentAnimationID != "dreamDashIn" && self.Sprite.CurrentAnimationID != "dreamDashLoop")
                {
                    self.Sprite.Play("dreamDashIn", false, false);
                }
            }
            else if (self.StateMachine.State == CustomFeatherState)
            {
                self.Sprite.Scale.X = Calc.Approach(self.Sprite.Scale.X, 1f, 1.75f * Engine.DeltaTime);
                self.Sprite.Scale.Y = Calc.Approach(self.Sprite.Scale.Y, 1f, 1.75f * Engine.DeltaTime);
            }
            else
            {
                orig(self);
            }
        }
        #endregion

        public static Player StateGetPlayer()
        {
            // TODO: Make smarter
            return (Engine.Scene as Level).Tracker.GetEntity<Player>();
        }

        private void Player_CallDashEvents(On.Celeste.Player.orig_CallDashEvents orig, Player self)
        {
            // sometimes crashes? Can't reproduce this myself and I have no clue why it happens, just gonna try catch this :/
            try
            {
                foreach (YellowBooster b in self.Scene.Tracker.GetEntities<YellowBooster>())
                {
                    b.sprite.SetColor(Color.White);
                    if (b.StartedBoosting)
                    {
                        b.PlayerBoosted(self, self.DashDir);
                        return;
                    }
                    if (b.BoostingPlayer)
                    {
                        return;
                    }
                }
                foreach (BlueBooster b in self.Scene.Tracker.GetEntities<BlueBooster>())
                {
                    if (b.StartedBoosting)
                    {
                        b.PlayerBoosted(self, self.DashDir);
                        return;
                    }
                    if (b.BoostingPlayer)
                    {
                        return;
                    }
                }
                foreach (GrayBooster b in self.Scene.Tracker.GetEntities<GrayBooster>())
                {
                    if (b.StartedBoosting)
                    {
                        b.PlayerBoosted(self, self.DashDir);
                        return;
                    }
                    if (b.BoostingPlayer)
                    {
                        return;
                    }
                }
            }
            catch {}
            orig(self);
        }

        private void Player_ctor(On.Celeste.Player.orig_ctor orig, Player self, Vector2 position, PlayerSpriteMode spriteMode)
        {
            orig(self, position, spriteMode);
            new DynData<Player>(self).Set<float>("lastDreamSpeed", 0f);
            // Let's define new states
            // .AddState is defined in StateMachineExt
            YellowBoostState = self.StateMachine.AddState(new Func<int>(YellowBoostUpdate), YellowBoostCoroutine, YellowBoostBegin, YellowBoostEnd);
            blueBoostState = self.StateMachine.AddState(new Func<int>(BlueBoostUpdate), BlueBoostCoroutine, BlueBoostBegin, BlueBoostEnd);
            grayBoostState = self.StateMachine.AddState(new Func<int>(GrayBoostUpdate), GrayBoostCoroutine, GrayBoostBegin, GrayBoostEnd);
            CustomDreamDashState = self.StateMachine.AddState(new Func<int>(CustomDreamBlock.DreamDashUpdate), null, CustomDreamBlock.DreamDashBegin, CustomDreamBlock.DreamDashEnd);
            CustomFeatherState = self.StateMachine.AddState(StarFlyUpdate, CustomFeatherCoroutine, CustomFeatherBegin, CustomFeatherEnd);
        }

        #region CustomFeathers
        public static int CustomFeatherState;
        public static MethodInfo player_StarFlyReturnToNormalHitbox = typeof(Player).GetMethod("StarFlyReturnToNormalHitbox", BindingFlags.Instance | BindingFlags.NonPublic);
        public static MethodInfo player_WallJump = typeof(Player).GetMethod("WallJump", BindingFlags.Instance | BindingFlags.NonPublic);
        public static MethodInfo player_WallJumpCheck = typeof(Player).GetMethod("WallJumpCheck", BindingFlags.Instance | BindingFlags.NonPublic);

        private void CustomFeatherBegin()
        {
            Player player = StateGetPlayer();
            DynData<Player> data = new DynData<Player>(player);
            CustomFeather feather = (CustomFeather)data["fh.customFeather"];
            player.Sprite.Play("startStarFly", false, false);
            data["starFlyTransforming"] = true;
            data["starFlyTimer"] = feather.FlyTime;
            data["starFlySpeedLerp"] = 0f;
            data["jumpGraceTimer"] = 0f;
            BloomPoint starFlyBloom = (BloomPoint)data["starFlyBloom"];
            if (starFlyBloom == null)
            {
                player.Add(starFlyBloom = new BloomPoint(new Vector2(0f, -6f), 0f, 16f));
            }
            starFlyBloom.Visible = true;
            starFlyBloom.Alpha = 0f;
            data["starFlyBloom"] = starFlyBloom;
            player.Collider = (Hitbox)data["starFlyHitbox"];
            data["hurtbox"] = data["starFlyHurtbox"];
            SoundSource starFlyLoopSfx = (SoundSource)data["starFlyLoopSfx"];
            SoundSource starFlyWarningSfx = (SoundSource)data["starFlyWarningSfx"];
            if (starFlyLoopSfx == null)
            {
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
        private void CustomFeatherEnd()
        {
            Player player = StateGetPlayer();
            DynData<Player> data = new DynData<Player>(player);
            CustomFeather feather = (CustomFeather)data["fh.customFeather"];
            player.Play("event:/game/06_reflection/feather_state_end", null, 0f);
            ((SoundSource)data["starFlyWarningSfx"]).Stop(true);
            ((SoundSource)data["starFlyLoopSfx"]).Stop(true);
            player.Hair.DrawPlayerSpriteOutline = false;
            player.Sprite.Color = Color.White;
            player.SceneAs<Level>().Displacement.AddBurst(player.Center, 0.25f, 8f, 32f, 1f, null, null);
            ((BloomPoint)data["starFlyBloom"]).Visible = false;
            player.Sprite.HairCount = (int)data["startHairCount"];
            player_StarFlyReturnToNormalHitbox.Invoke(player, null);
            bool flag = player.StateMachine.State != 2;
            if (flag)
            {
                player.SceneAs<Level>().Particles.Emit(feather.P_Boost, 12, player.Center, Vector2.One * 4f, (-player.Speed).Angle());
            }
        }
        private IEnumerator CustomFeatherCoroutine()
        {
            Player player = StateGetPlayer();
            DynData<Player> data = new DynData<Player>(player);
            CustomFeather feather = (CustomFeather)data["fh.customFeather"];
            while (player.Sprite.CurrentAnimationID == "startStarFly")
            {
                yield return null;
            }
            while (player.Speed != Vector2.Zero)
            {
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
            if (flag)
            {
                dir = Vector2.UnitX * (float)player.Facing;
            }
            player.Speed = dir * 250f;
            data["starFlyLastDir"] = dir;
            player.SceneAs<Level>().Particles.Emit(feather.P_Boost, 12, player.Center, Vector2.One * 4f, feather.FlyColor,(-dir).Angle());
            dir = default(Vector2);
            Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
            player.SceneAs<Level>().DirectionalShake((Vector2)data["starFlyLastDir"], 0.3f);
            while ((float)data["starFlyTimer"] > 0.5f)
            {
                yield return null;
            }
            ((SoundSource)data["starFlyWarningSfx"]).Play("event:/game/06_reflection/feather_state_warning", null, 0f);
            yield break;
        }

        private int StarFlyUpdate()
        {
            Player player = StateGetPlayer();
            Level level = player.SceneAs<Level>();
            DynData<Player> data = new DynData<Player>(player);
            BloomPoint bloomPoint = (BloomPoint)data["starFlyBloom"];
            CustomFeather feather = (CustomFeather)data["fh.customFeather"];
            // 2f -> StarFlyTime
            float StarFlyTime = feather.FlyTime;
            bloomPoint.Alpha = Calc.Approach(bloomPoint.Alpha, 0.7f, Engine.DeltaTime * StarFlyTime);
            data["starFlyBloom"] = bloomPoint;
            Input.Rumble(RumbleStrength.Climb, RumbleLength.Short);
            if ((bool)data["starFlyTransforming"])
            {
                player.Speed = Calc.Approach(player.Speed, Vector2.Zero, 1000f * Engine.DeltaTime);
            }
            else
            {
                Vector2 aimValue = Input.Aim.Value;
                bool notAiming = false;
                bool flag3 = aimValue == Vector2.Zero;
                if (flag3)
                {
                    notAiming = true;
                    aimValue = (Vector2)data["starFlyLastDir"];
                }
                Vector2 lastSpeed = player.Speed.SafeNormalize(Vector2.Zero);
                bool flag4 = lastSpeed == Vector2.Zero;
                if (flag4)
                {
                    lastSpeed = aimValue;
                }
                else
                {
                    lastSpeed = lastSpeed.RotateTowards(aimValue.Angle(), 5.58505344f * Engine.DeltaTime);
                }
                data["starFlyLastDir"] = lastSpeed;
                float target;
                if (notAiming)
                {
                    data["starFlySpeedLerp"] = 0f;
                    target = feather.NeutralSpeed; // was 91f
                }
                else
                {
                    bool flag6 = lastSpeed != Vector2.Zero && Vector2.Dot(lastSpeed, aimValue) >= 0.45f;
                    if (flag6)
                    {
                        data["starFlySpeedLerp"] = Calc.Approach((float)data["starFlySpeedLerp"], 1f, Engine.DeltaTime / 1f);
                        target = MathHelper.Lerp(feather.LowSpeed, feather.MaxSpeed, (float)data["starFlySpeedLerp"]);
                    }
                    else
                    {
                        data["starFlySpeedLerp"] = 0f;
                        target = 140f;
                    }
                }
                SoundSource ss = (SoundSource)data["starFlyLoopSfx"];
                ss.Param("feather_speed", (float)(notAiming ? 0 : 1));
                data["starFlyLoopSfx"] = ss;

                float num = player.Speed.Length();
                num = Calc.Approach(num, target, 1000f * Engine.DeltaTime);
                player.Speed = lastSpeed * num;
                bool flag7 = level.OnInterval(0.02f);
                if (flag7)
                {
                    level.Particles.Emit(feather.P_Flying, 1, player.Center, Vector2.One * 2f, feather.FlyColor, (-player.Speed).Angle());
                }
                bool pressed = Input.Jump.Pressed;
                if (pressed)
                {
                    bool flag8 = player.OnGround(3);
                    if (flag8)
                    {
                        player.Jump(true, true);
                        return 0;
                    }
                    bool flag9 = (bool)player_WallJumpCheck.Invoke(player, new object[] { -1 });
                    if (flag9)
                    {
                        player_WallJump.Invoke(player, new object[] { 1 });
                        return 0;
                    }
                    bool flag10 = (bool)player_WallJumpCheck.Invoke(player, new object[] { 1 });
                    if (flag10)
                    {
                        player_WallJump.Invoke(player, new object[] { -1 });
                        return 0;
                    }
                }
                bool check = Input.Grab.Check;
                if (check)
                {
                    bool flag11 = false;
                    int dir = 0;
                    bool flag12 = Input.MoveX.Value != -1 && player.ClimbCheck(1, 0);
                    if (flag12)
                    {
                        player.Facing = Facings.Right;
                        dir = 1;
                        flag11 = true;
                    }
                    else
                    {
                        bool flag13 = Input.MoveX.Value != 1 && player.ClimbCheck(-1, 0);
                        if (flag13)
                        {
                            player.Facing = Facings.Left;
                            dir = -1;
                            flag11 = true;
                        }
                    }
                    bool flag14 = flag11;
                    if (flag14)
                    {
                        bool noGrabbing = Celeste.SaveData.Instance.Assists.NoGrabbing;
                        if (noGrabbing)
                        {
                            player.Speed = Vector2.Zero;
                            player.ClimbTrigger(dir);
                            return 0;
                        }
                        return 1;
                    }
                }
                bool canDash = player.CanDash;
                if (canDash)
                {
                    return player.StartDash();
                }
                float starFlyTimer = (float)data["starFlyTimer"];
                starFlyTimer -= Engine.DeltaTime;
                data["starFlyTimer"] = starFlyTimer;
                bool flag15 = starFlyTimer <= 0f;
                if (flag15)
                {
                    bool flag16 = Input.MoveY.Value == -1;
                    if (flag16)
                    {
                        player.Speed.Y = -100f;
                    }
                    bool flag17 = Input.MoveY.Value < 1;
                    if (flag17)
                    {
                        data["varJumpSpeed"] = player.Speed.Y;
                        player.AutoJump = true;
                        player.AutoJumpTimer = 0f;
                        data["varJumpTimer"] = 0.2f;
                    }
                    bool flag18 = player.Speed.Y > 0f;
                    if (flag18)
                    {
                        player.Speed.Y = 0f;
                    }
                    bool flag19 = Math.Abs(player.Speed.X) > 140f;
                    if (flag19)
                    {
                        player.Speed.X = 140f * (float)Math.Sign(player.Speed.X);
                    }
                    Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
                    return 0;
                }
                bool flag20 = starFlyTimer < 0.5f && player.Scene.OnInterval(0.05f);
                if (flag20)
                {
                    Color starFlyColor = feather.FlyColor;
                    if (player.Sprite.Color == starFlyColor)
                    {
                        player.Sprite.Color = Player.NormalHairColor;
                    }
                    else
                    {
                        player.Sprite.Color = starFlyColor;
                    }
                }
            }
            return CustomFeatherState;
        }
        #endregion

        public static FieldInfo player_boostTarget = typeof(Player).GetField("boostTarget", BindingFlags.Instance | BindingFlags.NonPublic);

        #region YellowBoost

        public static int YellowBoostState;
        
        private void YellowBoostBegin()
        {
            Player player = StateGetPlayer();
            player.CurrentBooster = null;
            Level level = player.SceneAs<Level>();
            bool? flag;
            if (level == null)
            {
                flag = null;
            }
            else
            {
                MapMetaModeProperties meta = level.Session.MapData.GetMeta();
                flag = ((meta != null) ? meta.TheoInBubble : null);
            }
            bool? flag2 = flag;
            player.RefillDash();
            player.RefillStamina();
            if (flag2.GetValueOrDefault())
            {
                return;
            }
            player.Drop();
        }

        private int YellowBoostUpdate()
        {
            Player player = StateGetPlayer();
            Vector2 boostTarget = (Vector2)player_boostTarget.GetValue(player);
            Vector2 value = Input.Aim.Value * 3f;
            Vector2 vector = Calc.Approach(player.ExactPosition, boostTarget - player.Collider.Center + value, 80f * Engine.DeltaTime);
            player.MoveToX(vector.X, null);
            player.MoveToY(vector.Y, null);
            bool pressed = Input.Dash.Pressed;
            // the state we should be in afterwards
            int result;
            if (pressed)
            {
                Input.Dash.ConsumePress();
                result = Player.StDash;
            }
            else
            {
                result = YellowBoostState;
            }
            return result;
        }

        private void YellowBoostEnd()
        {
            Player player = StateGetPlayer();
            Vector2 boostTarget = (Vector2)player_boostTarget.GetValue(player);
            Vector2 vector = (boostTarget - player.Collider.Center).Floor();
            player.MoveToX(vector.X, null);
            player.MoveToY(vector.Y, null);
        }

        private IEnumerator YellowBoostCoroutine()
        {
            Player player = StateGetPlayer();
            YellowBooster booster = null;
            foreach (YellowBooster b in player.Scene.Tracker.GetEntities<YellowBooster>())
            {
                if (b.StartedBoosting)
                {
                    booster = b;
                    break;
                }
            }
            yield return booster.BoostTime / 6; // was 0.25
            booster.sprite.SetColor(booster.FlashTint);
            yield return booster.BoostTime / 3;
            booster.sprite.SetColor(Color.White);
            yield return booster.BoostTime / 6;
            booster.sprite.SetColor(booster.FlashTint);
            yield return booster.BoostTime / 3;
            booster.sprite.SetColor(Color.White);
            // Player didn't dash out, time to kill them :(
            player.Die(player.DashDir);
            booster.PlayerDied();
            //player.StateMachine.State = Player.StDash;
            yield break;
        }

        #endregion

        #region BlueBoost

        public static int blueBoostState;

        private void BlueBoostBegin()
        {
            Player player = StateGetPlayer();
            Level level = player.SceneAs<Level>();
            bool? flag;
            if (level == null)
            {
                flag = null;
            }
            else
            {
                MapMetaModeProperties meta = level.Session.MapData.GetMeta();
                flag = ((meta != null) ? meta.TheoInBubble : null);
            }
            bool? flag2 = flag;
            // Blue boosters don't recover dashes, just like in Frozen Waterfall
            //player.RefillDash();
            player.RefillStamina();
            if (flag2.GetValueOrDefault())
            {
                return;
            }
            player.Drop();
        }

        private int BlueBoostUpdate()
        {
            Player player = StateGetPlayer();
            Vector2 boostTarget = (Vector2)player_boostTarget.GetValue(player);
            Vector2 value = Input.Aim.Value * 3f;
            Vector2 vector = Calc.Approach(player.ExactPosition, boostTarget - player.Collider.Center + value, 80f * Engine.DeltaTime);
            player.MoveToX(vector.X, null);
            player.MoveToY(vector.Y, null);
            bool pressed = Input.Dash.Pressed;
            // the state we should be in afterwards
            int result;
            if (pressed)
            {
                Input.Dash.ConsumePress();
                result = Player.StDash;
            }
            else
            {
                result = blueBoostState;
            }
            return result;
        }

        private void BlueBoostEnd()
        {
            Player player = StateGetPlayer();
            Vector2 boostTarget = (Vector2)player_boostTarget.GetValue(player);
            Vector2 vector = (boostTarget - player.Collider.Center).Floor();
            player.MoveToX(vector.X, null);
            player.MoveToY(vector.Y, null);
        }

        private IEnumerator BlueBoostCoroutine()
        {
            Player player = StateGetPlayer();
            BlueBooster booster = null;
            foreach (BlueBooster b in player.Scene.Tracker.GetEntities<BlueBooster>())
            {
                if (b.StartedBoosting)
                {
                    booster = b;
                    break;
                }
            }
            yield return booster.BoostTime;
            player.StateMachine.State = Player.StDash;
            yield break;
        }

        #endregion

        #region GrayBoost

        public static int grayBoostState;

        private void GrayBoostBegin()
        {
            Player player = StateGetPlayer();
            Level level = player.SceneAs<Level>();
            bool? flag;
            if (level == null)
            {
                flag = null;
            }
            else
            {
                MapMetaModeProperties meta = level.Session.MapData.GetMeta();
                flag = ((meta != null) ? meta.TheoInBubble : null);
            }
            bool? flag2 = flag;
            player.RefillDash();
            player.RefillStamina();
            if (flag2.GetValueOrDefault())
            {
                return;
            }
            player.Drop();
        }

        private int GrayBoostUpdate()
        {
            Player player = StateGetPlayer();
            Vector2 boostTarget = (Vector2)player_boostTarget.GetValue(player);
            Vector2 value = Input.Aim.Value * 3f;
            Vector2 vector = Calc.Approach(player.ExactPosition, boostTarget - player.Collider.Center + value, 80f * Engine.DeltaTime);
            player.MoveToX(vector.X, null);
            player.MoveToY(vector.Y, null);
            // the state we should be in afterwards
            return grayBoostState;
        }

        private void GrayBoostEnd()
        {
            Player player = StateGetPlayer();
            Vector2 boostTarget = (Vector2)player_boostTarget.GetValue(player);
            Vector2 vector = (boostTarget - player.Collider.Center).Floor();
            player.MoveToX(vector.X, null);
            player.MoveToY(vector.Y, null);
        }

        private IEnumerator GrayBoostCoroutine()
        {
            Player player = StateGetPlayer();
            GrayBooster booster = null;
            foreach (GrayBooster b in player.Scene.Tracker.GetEntities<GrayBooster>())
            {
                if (b.StartedBoosting)
                {
                    booster = b;
                    break;
                }
            }
            yield return booster.BoostTime;
            player.StateMachine.State = Player.StDash;
            yield break;
        }

        #endregion

        private void LightningRenderer_Update(On.Celeste.LightningRenderer.orig_Update orig, LightningRenderer self)
        {
            orig(self);
            // Update any coroutines
            foreach (Component c in self.Components)
            {
                c.Update();
            }
        }

        // Unload the entirety of your mod's content, remove any event listeners and undo all hooks.
        public override void Unload()
        {
            // Legacy entity creation (for back when we didn't have the CustomEntity attribute)
            Everest.Events.Level.OnLoadEntity -= OnLoadEntity;
            // Custom Rising Lava integrations
            On.Celeste.Mod.Entities.LavaBlockerTrigger.Awake -= LavaBlockerTrigger_Awake;
            On.Celeste.Mod.Entities.LavaBlockerTrigger.OnStay -= LavaBlockerTrigger_OnStay;
            On.Celeste.Mod.Entities.LavaBlockerTrigger.OnLeave -= LavaBlockerTrigger_OnLeave;
            // Lightning Color Trigger
            On.Celeste.LightningRenderer.Update -= LightningRenderer_Update;
            On.Celeste.LightningRenderer.Awake -= LightningRenderer_Awake;
            On.Celeste.LightningRenderer.Reset -= LightningRenderer_Reset;
            // Register new states
            On.Celeste.Player.ctor -= Player_ctor;

            // For custom Boosters
            On.Celeste.Player.CallDashEvents -= Player_CallDashEvents;

            // For Custom Dream Blocks
            // legacy
            //On.Celeste.Player.OnCollideH -= Player_OnCollideH;
            //On.Celeste.Player.OnCollideV -= Player_OnCollideV;
            //On.Celeste.Player.RefillDash -= Player_RefillDash;
            //On.Celeste.Player.DreamDashUpdate -= Player_DreamDashUpdate;
            //On.Celeste.Player.DreamDashEnd -= Player_DreamDashEnd;
            // Custom dream blocks and feathers
            On.Celeste.Player.UpdateSprite -= Player_UpdateSprite;

            // custom feathers
            IL.Celeste.Player.UpdateHair -= modFeatherState;
            IL.Celeste.Player.OnCollideH -= modFeatherState;
            IL.Celeste.Player.OnCollideV -= modFeatherState;
            playerUpdateHook.Dispose();
            IL.Celeste.Player.Render -= modFeatherState;

            CustomSpinner.UnloadHooks();
        }

        private void LightningRenderer_Awake(On.Celeste.LightningRenderer.orig_Awake orig, LightningRenderer self, Scene scene)
        {
            orig(self,scene);
            if (scene is Level)
            {
                var session = (scene as Level).Session;
                Color[] colors = new Color[2]
                {
                SessionHelper.ReadColorFromSession(session, "fh.lightningColorA", Color.White),
                SessionHelper.ReadColorFromSession(session, "fh.lightningColorB", Color.White)
                };
                if (colors[0] != Color.White)
                {
                    LightningColorTrigger.ChangeLightningColor(self, colors);
                }
            }
        }
        
        // Make custom rising lava work with Lava Blocker Triggers:
        #region LavaBlockerInteraction
        private void LavaBlockerTrigger_OnLeave(On.Celeste.Mod.Entities.LavaBlockerTrigger.orig_OnLeave orig, Celeste.Mod.Entities.LavaBlockerTrigger self, Player player)
        {
            foreach (CustomRisingLava lava in customRisingLavas)
                if (lava != null)
                    lava.waiting = false;
            orig(self, player);
        }

        private void LavaBlockerTrigger_OnStay(On.Celeste.Mod.Entities.LavaBlockerTrigger.orig_OnStay orig, Celeste.Mod.Entities.LavaBlockerTrigger self, Player player)
        {
            foreach (CustomRisingLava lava in customRisingLavas)
                if (lava != null)
                    lava.waiting = true;
            orig(self, player);
        }

        List<CustomRisingLava> customRisingLavas;

        private void LavaBlockerTrigger_Awake(On.Celeste.Mod.Entities.LavaBlockerTrigger.orig_Awake orig, Celeste.Mod.Entities.LavaBlockerTrigger self, Scene scene)
        {
            orig(self, scene);
            customRisingLavas = scene.Entities.OfType<CustomRisingLava>().ToList();
        }
        #endregion
        // Optional, initialize anything after Celeste has initialized itself properly.
        public override void Initialize()
        {
            foreach (EverestModule mod in Everest.Modules)
            {
                if (mod.Metadata.Name == "FrostHelperExtension")
                {
                    throw new Exception("MOD CONFLICT: Please uninstall the FrostHelperExtension mod");
                }
            }

            //On.Celeste.LevelData.CreateEntityData += LevelData_CreateEntityData;
        }

        /*
        private EntityData LevelData_CreateEntityData(On.Celeste.LevelData.orig_CreateEntityData orig, LevelData self, BinaryPacker.Element entity)
        {
            var data = orig(self, entity);
            if (data.Name == "FrostHelper/IceSpinner") data.Name = "FHS";
            return data;
        } */

        private static bool OnLoadEntity(Level level, LevelData levelData, Vector2 offset, EntityData entityData)
        {
            switch (entityData.Name)
            {
                case "FrostHelper/IceSpinner":
                case "FrostHelperExt/CustomBloomSpinner":
                    level.Add(new CustomSpinner(entityData, offset, entityData.Bool("attachToSolid", false), entityData.Attr("directory", "danger/FrostHelper/icecrystal"), entityData.Attr("destroyColor", "639bff"), entityData.Bool("isCore", false), entityData.Attr("tint", "ffffff")));
                    //level.Add(new CustomSpinner(entityData, offset));
                    return true; 
                case "FrostHelper/KeyIce":
                    level.Add(new KeyIce(entityData, offset, new EntityID(levelData.Name, entityData.ID), null));
                    return true;
                case "FrostHelper/SlowCrushBlock":
                    level.Add(new SlowCrushBlock(entityData, offset));
                    return true;
                case "FrostHelper/CustomZipMover":
                    level.Add(new CustomZipMover(entityData, offset, entityData.Float("percentage", 100f), entityData.Enum("color", CustomZipMover.LineColor.Normal)));
                    return true;
                case "FrostHelper/Skateboard":
                    level.Add(new Skateboard(entityData, offset));
                    return true;
                case "FrostHelper/ToggleSwapBlock":
                    level.Add(new ToggleSwapBlock(entityData, offset));
                    return true;
                case "FrostHelper/CassetteTempoTrigger":
                    level.Add(new CassetteTempoTrigger(entityData, offset));
                    return true;
                case "FrostHelper/CustomRisingLava":
                    level.Add(new CustomRisingLava(entityData, offset));
                    return true;
                case "FrostHelper/StaticBumper":
                    level.Add(new StaticBumper(entityData, offset));
                    return true;
                case "FrostHelper/NoDashArea":
                    level.Add(new NoDashArea(entityData, offset));
                    return true;
                case "FrostHelper/SpringLeft":
                    level.Add(new CustomSpring(entityData, offset, Spring.Orientations.WallLeft));
                    return true;
                case "FrostHelper/SpringRight":
                    level.Add(new CustomSpring(entityData, offset, Spring.Orientations.WallRight));
                    return true;
                case "FrostHelper/SpringFloor":
                    level.Add(new CustomSpring(entityData, offset, Spring.Orientations.Floor));
                    return true;
                case "FrostHelper/CustomDreamBlock":
                    if (entityData.Bool("old", false))
                    {
                        level.Add(new CustomDreamBlock(entityData, offset));
                    } else
                    {
                        level.Add(new CustomDreamBlockV2(entityData, offset));
                    }
                    return true;
                default:
                    return false;
            }
        }

        public static Vector2 StringToVec2(string str)
        {
            string[] strSplit = str.Split(',');
            if (strSplit.Length < 2)
            {
                return new Vector2(float.Parse(strSplit[0]), float.Parse(strSplit[0]));
            }
            return new Vector2(float.Parse(strSplit[0]), float.Parse(strSplit[1]));
        }

        /// <summary>
        /// Returns a list of colors from a comma-separated string of types
        /// </summary>
        public static Type[] GetTypes(string typeString)
        {
            string[] split = typeString.Trim().Split(',');
            Type[] parsed = new Type[split.Length];
            for (int i = 0; i < split.Length; i++)
            {

                parsed[i] = split[i].Trim() == "" ? null : FakeAssembly.GetEntryAssembly().GetType(split[i].Trim(), true, true);
            }
            return parsed;
        }
    }

    public class ColorHelper
    {
        /// <summary>
        /// Returns a list of colors from a comma-separated string of hex colors OR xna color names
        /// </summary>
        public static Color[] GetColors(string colors)
        {
            string[] split = colors.Trim().Split(',');
            Color[] parsed = new Color[split.Length];
            for (int i = 0; i < split.Length; i++)
            {
                parsed[i] = GetColor(split[i]);
            }
            return parsed;
        }

        public static Color GetColor(string color)
        {
            foreach (PropertyInfo propertyInfo in colorProps)
            {
                bool flag = color.Equals(propertyInfo.Name, StringComparison.OrdinalIgnoreCase);
                if (flag)
                {
                    return (Color)propertyInfo.GetValue(default(Color), null);
                }
            }
            try
            {
                return Calc.HexToColor(color.Replace("#", ""));
            }
            catch
            {
            }
            return Color.Transparent;
        }

        private static readonly PropertyInfo[] colorProps = typeof(Color).GetProperties();
    }

    public class EaseHelper
    {
        public static Ease.Easer GetEase(string name)
        {
            foreach (FieldInfo propertyInfo in easeProps)
            {
                bool flag = name.Equals(propertyInfo.Name, StringComparison.OrdinalIgnoreCase);
                if (flag)
                {
                    return (Ease.Easer)propertyInfo.GetValue(default(Ease));
                }
            }
            return Ease.Linear;
        }

        private static readonly FieldInfo[] easeProps = typeof(Ease).GetFields(BindingFlags.Static | BindingFlags.Public);
    }

    public static class SessionHelper
    {
        public static void WriteColorToSession(Session session, string baseFlag, Color color)
        {
            session.SetCounter(baseFlag, Convert.ToInt32(color.R.ToString("x2") + color.G.ToString("x2") + color.B.ToString("x2"), 16));
            session.SetCounter($"{baseFlag}Alpha", color.A);
            session.SetCounter($"{baseFlag}Set", 1);
        }

        public static Color ReadColorFromSession(Session session, string baseFlag, Color baseColor)
        {
            if (session.GetCounter($"{baseFlag}Set") == 1)
            {
                Color c = Calc.HexToColor(session.GetCounter(baseFlag));
                c.A = (byte)session.GetCounter($"{baseFlag}Alpha");
                return c;
            }
            return baseColor;
        }
    }
}