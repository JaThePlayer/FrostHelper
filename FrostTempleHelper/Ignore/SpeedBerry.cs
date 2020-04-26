using Celeste;
using Celeste.Mod;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System.Collections;

namespace FrostHelper
{
    /// <summary>
    /// Will be moved to CollabUtils2 ???
    /// </summary>
    [CustomEntity("FrostHelper/SpeedBerry")]
    [RegisterStrawberry(false, true)]
    class SpeedBerry : Strawberry, IStrawberry
    {

        public int TimeLimit;
        public int CurrentTime;

        private Vector2 start;
        private Vector2 nearestSpawn;


        public SpeedBerry(EntityData data, Vector2 offset, EntityID id) : base(fixData(data), offset, id)
        {
            TimeLimit = data.Int("timeLimit", 0) * 60;
        }

        static EntityData fixData(EntityData toFix)
        {
            if (toFix.Values.ContainsKey("moon"))
            {
                toFix.Values["moon"] = true;
            } else
            {
                toFix.Values.Add("moon", true);
            }
            
            return toFix;
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);
            start = Position;
            // The Speed Berry's gimmick needs to save information about the spawn point nearest its home location.
            nearestSpawn = SceneAs<Level>().GetSpawnPoint(start);
            //Sprite sprite = Get<Sprite>();
            if (SaveData.Instance.CheckStrawberry(ID))
            {
                DynData<Strawberry> data = new DynData<Strawberry>(this as Strawberry);
                Sprite sprite = data.Get<Sprite>("sprite");
                Remove(sprite);
                sprite = FrostModule.SpriteBank.Create("ghostspeedberry");
                Add(sprite);
                data.Set("sprite", sprite);
            } else
            {
                //sprite.Path = "speedberry"; // FrostModule.SpriteBank.Create("speedberry");
                DynData<Strawberry> data = new DynData<Strawberry>(this as Strawberry);
                Sprite sprite = data.Get<Sprite>("sprite");
                Remove(sprite);
                sprite = FrostModule.SpriteBank.Create("speedberry");
                Add(sprite);
                data.Set("sprite", sprite);
            }
        }

        public override void Update()
        {
            if (Follower.HasLeader)
            {
                SpeedBerryTimerDisplay.Enabled = true;
                FrostModule.Instance.HasSpeedBerry = true;
                CurrentTime++;
                FrostModule.Instance.SpeedBerryTimeRemaining = TimeLimit - CurrentTime;
                Player player = Follower.Leader.Entity as Player;
                bool a = player.StrawberriesBlocked;
                player.StrawberriesBlocked = true;
                base.Update();
                player.StrawberriesBlocked = a;
                if (timer == null)
                {
                    timer = new SpeedBerryTimerDisplay();
                    SceneAs<Level>().Add(timer);
                }
                
            } else
            {
                SpeedBerryTimerDisplay.Enabled = false;
                FrostModule.Instance.HasSpeedBerry = false;
                SceneAs<Level>().Remove(timer);
                timer = null;
                base.Update();
            }
            if (TimeLimit < CurrentTime)
            {
                // Time ran out
                TimeRanOut = true;
                Dissolve();
            }
            // If this Strawberry didn't block normal collection, we would check here to find out if we could collect it.
            // However, since it CAN'T be collected normally, instead we'll check if the Player is overlapping a SpeedBerryCollectTrigger.
            if (Follower.Leader != null)
            {
                Player player = Follower.Leader.Entity as Player;
                if (player.CollideCheck<SpeedBerryCollectTrigger>())
                {
                    OnCollect();
                }
            }
        }

        public void Dissolve()
        {
            if (Follower.Leader != null)
            {
                Player player = Follower.Leader.Entity as Player;
                player.StrawberryCollectResetTimer = 2.5f;
                //Follower.Leader.LoseFollower(Follower);
                Add(new Coroutine(DissolveRoutine(player), true));
            } else
            {
                Add(new Coroutine(DissolveRoutine(null), true));
            }
            
        }

        private IEnumerator DissolveRoutine(Player follower)
        {
            FrostModule.Instance.HasSpeedBerry = false;
            Sprite sprite = Get<Sprite>();
            Level level = Scene as Level;
            Session session = level.Session;
            session.DoNotLoad.Remove(ID);
            Audio.Play("event:/game/general/seed_poof", Position);
            Collidable = false;
            sprite.Scale = Vector2.One * 0.5f;
            if (follower != null)
            {
                //follower.Die(Vector2.Zero, true, true);
                foreach (Player player in Scene.Tracker.GetEntities<Player>())
                {
                    player.Die(Vector2.Zero, true, true);
                }
            }
            yield return 0.05f;
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
            for (int i = 0; i < 6; i++)
            {
                float dir = Calc.Random.NextFloat(6.28318548f);
                level.ParticlesFG.Emit(StrawberrySeed.P_Burst, 1, Position + Calc.AngleToVector(dir, 4f), Vector2.Zero, dir);
            }
            sprite.Scale = Vector2.Zero;
            Visible = false;
            RemoveSelf();
            yield break;
        }

        // hackfix for TAS tools crashing when you grab the berry
#pragma warning disable CS0414
        private float collectTimer = 0f;
#pragma warning restore CS0414

        SpeedBerryTimerDisplay timer;

        public bool TimeRanOut;
    }
    
    public class SpeedBerryTimerDisplay : Entity
    {
        public static bool Enabled;
        public SpeedBerryTimerDisplay()
        {
            CompleteTimer = 0f;
            bg = GFX.Gui["strawberryCountBG"];
            DrawLerp = 0f;
            Tag = (Tags.HUD | Tags.Global | Tags.PauseUpdate | Tags.TransitionUpdate);
            Depth = -100;
            //Y = 60f;
            Position = new Vector2(Engine.Width, 120f) / 2f;
            CalculateBaseSizes();
            Add(wiggler = Wiggler.Create(0.5f, 4f, null, false, false));
        }

        static PixelFontSize pixelFontSize;

        public static void CalculateBaseSizes()
        {
            PixelFont font = Dialog.Languages["english"].Font;
            float fontFaceSize = Dialog.Languages["english"].FontFaceSize;
            pixelFontSize = font.Get(fontFaceSize);
            for (int i = 0; i < 10; i++)
            {
                float x = pixelFontSize.Measure(i.ToString()).X;
                bool flag = x > numberWidth;
                if (flag)
                {
                    numberWidth = x;
                }
            }
            spacerWidth = pixelFontSize.Measure('.').X;
        }

        public override void Update()
        {
            DrawLerp = Calc.Approach(DrawLerp, 1f, Engine.DeltaTime * 4f);
            base.Update();
        }

        string FormatTime(int seconds)
        {
            int left = seconds % 60;
            string time = (seconds / 60) + ":" + (left < 10 ? "0" : "") + left;
            return time;
        }

        public override void Render()
        {
            bool flag = DrawLerp <= 0f;
            if (!flag)
            {
                //float num = -500f; //-Ease.CubeIn(1f - DrawLerp);
                float num = Position.X;
                Level level = Scene as Level;
                Session session = level.Session;
                string timeString = FormatTime(FrostModule.Instance.SpeedBerryTimeRemaining / 60);
                //bg.Draw(new Vector2(num, Y), Vector2.Zero, Color.White, new Vector2(-1f, 1f));
                //bg.Draw(new Vector2(num, Y), Vector2.Zero, Color.White, new Vector2(1f, 1f));
                if (Enabled)
                DrawTime(new Vector2(num - (pixelFontSize.Measure(timeString).X / 2) - spacerWidth, Y + 44f), timeString, 1f + wiggler.Value * 0.15f, session.StartedFromBeginning, level.Completed, session.BeatBestTime, 1f);

            }
        }

        public static void DrawTime(Vector2 position, string timeString, float scale = 1f, bool valid = true, bool finished = false, bool bestTime = false, float alpha = 1f)
        {
            PixelFont font = Dialog.Languages["english"].Font;
            float fontFaceSize = Dialog.Languages["english"].FontFaceSize;
            float num = scale;
            float num2 = position.X;
            float num3 = position.Y;
            Color color = Color.White * alpha;
            Color color2 = Color.LightGray * alpha;
            foreach (char c in timeString)
            {
                bool flag2 = c == '.';
                if (flag2)
                {
                    num = scale * 0.7f;
                    num3 -= 5f * scale;
                }
                Color color3 = (c == ':' || c == '.' || num < scale) ? color2 : color;
                float num4 = (((c == ':' || c == '.') ? SpeedBerryTimerDisplay.spacerWidth : SpeedBerryTimerDisplay.numberWidth) + 4f) * num;
                font.DrawOutline(fontFaceSize, c.ToString(), new Vector2(num2 + num4 / 2f, num3), new Vector2(0.5f, 1f), Vector2.One * num, color3, 2f, Color.Black);
                num2 += num4;
            }
        }

        public static float GetTimeWidth(string timeString, float scale = 1f)
        {
            float num = scale;
            float num2 = 0f;
            foreach (char c in timeString)
            {
                bool flag = c == '.';
                if (flag)
                {
                    num = scale * 0.7f;
                }
                float num3 = (((c == ':' || c == '.') ? SpeedBerryTimerDisplay.spacerWidth : SpeedBerryTimerDisplay.numberWidth) + 4f) * num;
                num2 += num3;
            }
            return num2;
        }

        // Note: this type is marked as 'beforefieldinit'.
        static SpeedBerryTimerDisplay()
        {
            SpeedBerryTimerDisplay.numberWidth = 0f;
            SpeedBerryTimerDisplay.spacerWidth = 0f;
        }

        public float CompleteTimer;

        public const int GuiChapterHeight = 58;

        public const int GuiFileHeight = 78;

        private static float numberWidth;

        private static float spacerWidth;

        private MTexture bg;

        public float DrawLerp;

        private Wiggler wiggler;
    }
}
