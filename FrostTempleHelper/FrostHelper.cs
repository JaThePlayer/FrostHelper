using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Celeste;
using Celeste.Mod;
using Celeste.Mod.Meta;
using FrostTempleHelper;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;

namespace FrostHelper
{
    public class FrostModule : EverestModule
    {
        public static SpriteBank SpriteBank;
        // Only one alive module instance can exist at any given time.
        public static FrostModule Instance;
        bool initFont = false;
        public bool HasSpeedBerry = false;
        public int SpeedBerryTimeRemaining = -1;
        public FrostModule()
        {
            Instance = this;
        }
        // no save data needed
        public override Type SaveDataType => null;

        // If you don't need to store any settings, => null
        public override Type SettingsType
        {
            get
            {
                return null;
            }
        }

        public override void LoadContent(bool firstLoad)
        {
            SpriteBank = new SpriteBank(GFX.Game, "Graphics/FrostHelper/CustomSprites.xml");
        }

        // Set up any hooks, event handlers and your mod in general here.
        // Load runs before Celeste itself has initialized properly.
        public override void Load()
        {
            Everest.Events.Level.OnLoadEntity += OnLoadEntity;
            On.Celeste.Player.Die += Player_Die;
            On.Celeste.Mod.Entities.LavaBlockerTrigger.Awake += LavaBlockerTrigger_Awake;
            On.Celeste.Mod.Entities.LavaBlockerTrigger.OnStay += LavaBlockerTrigger_OnStay;
            On.Celeste.Mod.Entities.LavaBlockerTrigger.OnLeave += LavaBlockerTrigger_OnLeave;
            On.Celeste.LightningRenderer.Update += LightningRenderer_Update;
            
            // Register new states
            On.Celeste.Player.ctor += Player_ctor;

            // For custom Boosters
            On.Celeste.Player.CallDashEvents += Player_CallDashEvents;

            // For Custom Dream Blocks
            On.Celeste.Player.OnCollideH += Player_OnCollideH;
            On.Celeste.Player.OnCollideV += Player_OnCollideV;
            On.Celeste.Player.UpdateSprite += Player_UpdateSprite;
            On.Celeste.Player.RefillDash += Player_RefillDash;

            // TODO: REMOVE
            // Don't call Update when the game is not Active
            //On.Celeste.Celeste.Update += Celeste_Update;
            //On.Celeste.Celeste.RenderCore += Celeste_RenderCore;
            //On.Monocle.Engine.Draw += Engine_Draw;
        }

        private void Engine_Draw(On.Monocle.Engine.orig_Draw orig, Engine self, GameTime gameTime)
        {
            if (!self.IsActive)
            {
                return;
            }
            orig(self, gameTime);
        }

        private void Celeste_RenderCore(On.Celeste.Celeste.orig_RenderCore orig, Celeste.Celeste self)
        {
            if (!self.IsActive)
            {
                return;
            }
            orig(self);
        }

        private void Celeste_Update(On.Celeste.Celeste.orig_Update orig, Celeste.Celeste self, GameTime gameTime)
        {
            if (!self.IsActive)
            {
                return;
            }
            orig(self, gameTime);
        }

        private bool Player_RefillDash(On.Celeste.Player.orig_RefillDash orig, Player self)
        {
            if (self.StateMachine.State != CustomDreamDashState)
                return orig(self);
            return false;
        }

        private void Player_UpdateSprite(On.Celeste.Player.orig_UpdateSprite orig, Player self)
        {
            if (self.StateMachine.State == CustomDreamDashState)
            {
                if (self.Sprite.CurrentAnimationID != "dreamDashIn" && self.Sprite.CurrentAnimationID != "dreamDashLoop")
                {
                    self.Sprite.Play("dreamDashIn", false, false);
                }
            } else
            {
                orig(self);
            }
        }

        private void Player_OnCollideV(On.Celeste.Player.orig_OnCollideV orig, Player self, CollisionData data)
        {
            if (self.StateMachine.State == 2 || self.StateMachine.State == 5)
            {
                bool flag14 = CustomDreamBlock.DreamDashCheck(self, Vector2.UnitY * (float)Math.Sign(self.Speed.Y));
                if (flag14)
                {
                    self.StateMachine.State = CustomDreamDashState;
                    DynData<Player> ddata = new DynData<Player>(self);
                    ddata.Set("dashAttackTimer", 0f);
                    ddata.Set("gliderBoostTimer", 0f);
                    return;
                }
            }
            if (self.StateMachine.State != CustomDreamDashState)
            {
                orig(self, data);
            }

        }

        private void Player_OnCollideH(On.Celeste.Player.orig_OnCollideH orig, Player self, CollisionData data)
        {
            if (self.StateMachine.State == 2 || self.StateMachine.State == 5)
            {
                bool flag14 = CustomDreamBlock.DreamDashCheck(self, Vector2.UnitX * (float)Math.Sign(self.Speed.X));
                if (flag14)
                {
                    self.StateMachine.State = CustomDreamDashState;
                    DynData<Player> ddata = new DynData<Player>(self);
                    ddata.Set("dashAttackTimer", 0f);
                    ddata.Set("gliderBoostTimer", 0f);
                    return;
                }
            }
            if (self.StateMachine.State != CustomDreamDashState)
            {
                orig(self, data);
            }
            
            
        }

        public static Player StateGetPlayer()
        {
            // TODO: Make smarter
            return (Engine.Scene as Level).Tracker.GetEntity<Player>();
        }

        private void Player_CallDashEvents(On.Celeste.Player.orig_CallDashEvents orig, Player self)
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

            orig(self);
        }

        private void Player_ctor(On.Celeste.Player.orig_ctor orig, Player self, Vector2 position, PlayerSpriteMode spriteMode)
        {
            orig(self, position, spriteMode);
            // Let's define new states
            // .AddState is defined in StateMachineExt
            yellowBoostState = self.StateMachine.AddState(new Func<int>(YellowBoostUpdate), YellowBoostCoroutine, YellowBoostBegin, YellowBoostEnd);
            blueBoostState = self.StateMachine.AddState(new Func<int>(BlueBoostUpdate), BlueBoostCoroutine, BlueBoostBegin, BlueBoostEnd);
            grayBoostState = self.StateMachine.AddState(new Func<int>(GrayBoostUpdate), GrayBoostCoroutine, GrayBoostBegin, GrayBoostEnd);
            CustomDreamDashState = self.StateMachine.AddState(new Func<int>(CustomDreamBlock.DreamDashUpdate), null, CustomDreamBlock.DreamDashBegin, CustomDreamBlock.DreamDashEnd);
        }

        public static int CustomDreamDashState;
        public static FieldInfo player_boostTarget = typeof(Player).GetField("boostTarget", BindingFlags.Instance | BindingFlags.NonPublic);

        #region YellowBoost

        public static int yellowBoostState;
        
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
                result = yellowBoostState;
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
            Everest.Events.Level.OnLoadEntity -= OnLoadEntity;
            On.Celeste.Player.Die -= Player_Die;
            On.Celeste.Mod.Entities.LavaBlockerTrigger.Awake -= LavaBlockerTrigger_Awake;
            On.Celeste.Mod.Entities.LavaBlockerTrigger.OnStay -= LavaBlockerTrigger_OnStay;
            On.Celeste.Mod.Entities.LavaBlockerTrigger.OnLeave -= LavaBlockerTrigger_OnLeave;
            On.Celeste.LightningRenderer.Update -= LightningRenderer_Update;
            On.Celeste.Player.ctor -= Player_ctor;
            On.Celeste.Player.CallDashEvents -= Player_CallDashEvents;
        }

        
        static PixelFont font;
        static float fontFaceSize;
        static PixelFontSize pixelFontSize;
        static float spacerWidth;

        static float numberWidth = 0f;

        static void getNumberWidth()
        {
            for (int i = 0; i < 10; i++)
            {
                float x = pixelFontSize.Measure(i.ToString()).X;
                bool flag = x > numberWidth;
                if (flag)
                {
                    numberWidth = x;
                }
            }
        }

        // Handle Speed Berries
        private PlayerDeadBody Player_Die(On.Celeste.Player.orig_Die orig, Player self, Vector2 direction, bool evenIfInvincible, bool registerDeathInStats)
        {
            Session session = self.SceneAs<Level>().Session;
            SpeedBerry speedStrawb = null;
            foreach (Player player in self.Scene.Tracker.GetEntities<Player>())
            {
                foreach (Follower follower in player.Leader.Followers)
                {
                    if (follower.Entity is SpeedBerry)
                    {
                        SpeedBerryTimerDisplay.Enabled = false;
                        if ((follower.Entity as SpeedBerry).TimeRanOut)
                        speedStrawb = (follower.Entity as SpeedBerry);
                    }
                }
            }
            
            PlayerDeadBody body = orig(self, direction, evenIfInvincible, registerDeathInStats);
            
            if (body != null)
            {
                
                if (speedStrawb != null)
                {
                    body.HasGolden = true;
                    body.DeathAction = delegate ()
                    {
                        Engine.Scene = new LevelExit(LevelExit.Mode.GoldenBerryRestart, session, null)
                        {
                            GoldenStrawberryEntryLevel = speedStrawb.ID.Level
                        };
                    };
                }
            }

            return body;
        }

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
        }


        private static bool OnLoadEntity(Level level, LevelData levelData, Vector2 offset, EntityData entityData)
        {
            switch (entityData.Name)
            {
                case "FrostHelper/KeyIce":
                    level.Add(new KeyIce(entityData, offset, new EntityID(levelData.Name, entityData.ID), null));
                    return true;
                case "FrostHelper/SlowCrushBlock":
                    level.Add(new SlowCrushBlock(entityData, offset));
                    return true;
                case "FrostHelper/CustomZipMover":
                    level.Add(new CustomZipMover(entityData, offset, entityData.Float("percentage", 100f), entityData.Enum<CustomZipMover.LineColor>("color", CustomZipMover.LineColor.Normal)));
                    return true;
                case "FrostHelper/IceSpinner":
                case "FrostHelperExt/CustomBloomSpinner":
                    level.Add(new CrystalStaticSpinner(entityData, offset, entityData.Bool("attachToSolid", false), entityData.Attr("directory", "danger/FrostHelper/icecrystal"), entityData.Attr("destroyColor", "639bff"), entityData.Bool("isCore", false), entityData.Attr("tint", "")));
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
                default:
                    return false;
            }
        }

        public static Vector2 StringToVec2(string str)
        {
            string[] strSplit = str.Split(',');
            if (strSplit.Length < 2)
            {
                //Logger.Log("Frost Helper", $"[ERROR] Vector2 doesn't have enough parameters! string: {str}");
                //throw new Exception($"[Frost Helper] Vector2 doesn't have enough parameters! string: {str}");
                return new Vector2(float.Parse(strSplit[0]), float.Parse(strSplit[0]));
            }
            return new Vector2(float.Parse(strSplit[0]), float.Parse(strSplit[1]));
        }
    }

    public class ColorHelper
    {
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
}