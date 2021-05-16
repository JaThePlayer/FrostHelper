using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FrostHelper
{
    [CustomEntity("FrostHelper/SpeedRingChallenge")]
    [Tracked]
    public class SpeedRingChallenge : Entity
    {
        SpeedRingTimerDisplay timer;

        public readonly EntityID ID;
        Vector2 offset;

        public readonly string ChallengeNameID;

        Vector2[] nodes;
        public int currentNodeID = -1;

        public readonly long TimeLimit;

        public long StartChapterTimer = 0;
        public long TimeSpent => Finished ? FinalTimeSpent : Scene == null ? 0 : SceneAs<Level>().Session.Time - StartChapterTimer;

        public long FinalTimeSpent = -1;

        bool started;

        public bool Finished;

        public Strawberry BerryToSpawn;

        float width, height;
        bool spawnBerry;

        public Color RingColor;

        public SpeedRingChallenge(EntityData data, Vector2 offset, EntityID id) : base(data.Position + offset)
        {
            this.offset = offset;
            ID = id;
            nodes = data.NodesOffset(offset);
            TimeLimit = TimeSpan.FromSeconds(data.Float("timeLimit", 1f)).Ticks;
            ChallengeNameID = data.Attr("name", "fh_test");
            width = data.Width;
            height = data.Height;
            spawnBerry = data.Bool("spawnBerry", true);

        }

        public override void Awake(Scene scene)
        {
            base.Awake(scene);

            RingColor = FrostModule.SaveData.IsChallengeBeaten(SceneAs<Level>().Session.Area.SID, ChallengeNameID, TimeLimit) ? Color.Blue : Color.Gold;

            var last = nodes.Last();
            if (spawnBerry)
            {
                Collider = new Hitbox(width, height, last.X - Position.X, last.Y - Position.Y);
                BerryToSpawn = null;
                foreach (var berry in Scene.Entities.OfType<Strawberry>())
                {
                    if (new Rectangle((int)last.X, (int)last.Y, (int)width, (int)height).Contains(new Point((int)berry.Position.X, (int)berry.Position.Y)))
                    {
                        BerryToSpawn = berry;
                        break;
                    }
                }
                if (BerryToSpawn == null)
                {
                    throw new Exception($"Didn't find a berry inside of the final node of the Speed Ring: {ChallengeNameID}, but there's {Scene.Entities.OfType<Strawberry>().Count()} berries");
                }
                BerryToSpawn.Active = BerryToSpawn.Visible = BerryToSpawn.Collidable = false;
            }
            
            Collider.Position = Vector2.Zero;
        }

        Vector2 initialRespawn;

        List<SpeedRingChallenge> disabledChallenges;

        public override void Update()
        {
            base.Update();
            Active = Visible = !Finished;
            if (!Finished && CollideCheck<Player>())
            {
                if (!started)
                {
                    StartChapterTimer = SceneAs<Level>().Session.Time;
                    Scene.Add(timer = new SpeedRingTimerDisplay(this));
                    started = true;
                    initialRespawn = SceneAs<Level>().Session.RespawnPoint.GetValueOrDefault();
                    disabledChallenges = Scene.Tracker.GetEntities<SpeedRingChallenge>().Cast<SpeedRingChallenge>().ToList();
                    disabledChallenges.Remove(this);
                    foreach (var item in disabledChallenges)
                    {
                        item.Active = item.Collidable = item.Visible = false;
                    }
                }

                Vector2 particlePos = (currentNodeID == -1 ? Position : nodes[currentNodeID]) + (Height / 2)*Vector2.UnitY;
                Scene.Add(new SummitCheckpoint.ConfettiRenderer(particlePos));
                Audio.Play("event:/game/07_summit/checkpoint_confetti", particlePos);

                currentNodeID++;

                if (currentNodeID+1 < nodes.Length)
                {
                    Collider.Position = nodes[currentNodeID] - Position;
                } else
                {
                    // last node
                    if (!Finished)
                    {
                        FinalTimeSpent = TimeSpent;
                        Finished = true;
                        Visible = false;

                        FrostModule.SaveData.SetChallengeTime(SceneAs<Level>().Session.Area.SID, ChallengeNameID, FinalTimeSpent);

                        if (TimeSpent < TimeLimit)
                        {
                            // Finished the time trial in time
                            Scene.OnEndOfFrame += () =>
                            {
                                BerryToSpawn.Active = BerryToSpawn.Collidable = true;
                                BerryToSpawn.Seeds = new List<StrawberrySeed>
                                {
                                    new StrawberrySeed(BerryToSpawn, Scene.Tracker.GetEntity<Player>().Position, 1, SaveData.Instance.CheckStrawberry(BerryToSpawn.ID))
                                };
                                foreach (var item in BerryToSpawn.Seeds)
                                {
                                    Scene.Add(item);
                                }
                                SceneAs<Level>().Session.DoNotLoad.Add(ID);
                            };
                        }
                        timer.FadeOut();
                        foreach (var item in disabledChallenges)
                        {
                            item.Active = item.Collidable = item.Visible = true;
                        }
                    }
                }
            }
            if (started && !Finished)
            {
                SceneAs<Level>().Session.RespawnPoint = initialRespawn;
            }
        }


        public override void Render()
        {
            base.Render();
            if (!(Scene as Level).Paused)
            {
                lerp += 3f * Engine.DeltaTime;
                if (lerp >= 1f)
                {
                    lerp = 0f;
                }
            }
            
            DrawRing(Collider.Center + Position);//currentNodeID == -1 ? Collider.Center : nodes[currentNodeID]);
        }
        float lerp;

        private void DrawRing(Vector2 position)
        {
            float maxRadiusY = MathHelper.Lerp(4f, Height / 2, lerp);
            float maxRadiusX = MathHelper.Lerp(4f, Width, lerp);
            Vector2 value = GetVectorAtAngle(0f);
            for (int i = 1; i <= 8; i++)
            {
                float radians = (float)i * 0.3926991f;
                Vector2 vectorAtAngle = GetVectorAtAngle(radians);
                Draw.Line(position + value, position + vectorAtAngle, RingColor);
                Draw.Line(position - value, position - vectorAtAngle, RingColor);
                value = vectorAtAngle;
            }

            Vector2 GetVectorAtAngle(float radians)
            {
                Vector2 vector = Calc.AngleToVector(radians, 1f);
                Vector2 scaleFactor = new Vector2(MathHelper.Lerp(maxRadiusX, maxRadiusX * 0.5f, Math.Abs(Vector2.Dot(vector, Calc.AngleToVector(0f, 1f)))), MathHelper.Lerp(maxRadiusY, maxRadiusY * 0.5f, Math.Abs(Vector2.Dot(vector, Calc.AngleToVector(0f, 1f)))));
                return vector * scaleFactor;
            }
        }
    }

    public class SpeedRingTimerDisplay : Entity
    {
        float fadeTime;
        bool fading;

        Wiggler wiggler;
        readonly SpeedRingChallenge TrackedChallenge;


        string Name;
        Vector2 NameMeasure;
        /*
        public void Track(SpeedRingChallenge challenge)
        {
            TrackedChallenge = challenge;
        } */

        public SpeedRingTimerDisplay(SpeedRingChallenge challenge)
        {
            Tag = (Tags.HUD | Tags.PauseUpdate);
            calculateBaseSizes();
            Add(wiggler = Wiggler.Create(0.5f, 4f, null, false, false));
            TrackedChallenge = challenge;
            fadeTime = 3f;

            createTween(0.1f, t => {
                Position = Vector2.Lerp(OffscreenPos, OnscreenPos, t.Eased);
            });

            Name = Dialog.Clean(challenge.ChallengeNameID);
            NameMeasure = ActiveFont.Measure(Name);
        }


        private void createTween(float fadeTime, Action<Tween> onUpdate)
        {
            Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeInOut, fadeTime, true);
            tween.OnUpdate = onUpdate;
            Add(tween);
        }

        public void FadeOut()
        {
            fadeTime = 5f;
            fading = true;
        }

        private void calculateBaseSizes()
        {
            // compute the max size of a digit and separators in the English font, for the timer part.
            PixelFont font = Dialog.Languages["english"].Font;
            float fontFaceSize = Dialog.Languages["english"].FontFaceSize;
            PixelFontSize pixelFontSize = font.Get(fontFaceSize);
            for (int i = 0; i < 10; i++)
            {
                float digitWidth = pixelFontSize.Measure(i.ToString()).X;
                if (digitWidth > numberWidth)
                {
                    numberWidth = digitWidth;
                }
            }
            spacerWidth = pixelFontSize.Measure('.').X;
            numberHeight = pixelFontSize.Measure("0:.").Y;

            /*
            // measure the ranks in the font for the current language.
            rankMeasurements = new Dictionary<string, Vector2>() {
                { "Gold", ActiveFont.Measure(Dialog.Clean("collabutils2_speedberry_gold") + " ") * targetTimeScale},
                { "Silver", ActiveFont.Measure(Dialog.Clean("collabutils2_speedberry_silver") + " ") * targetTimeScale},
                { "Bronze", ActiveFont.Measure(Dialog.Clean("collabutils2_speedberry_bronze") + " ") * targetTimeScale}
            }; */
        }

        private void drawTime(Vector2 position, string timeString, Color color, float scale = 1f, float alpha = 1f)
        {
            PixelFont font = Dialog.Languages["english"].Font;
            float fontFaceSize = Dialog.Languages["english"].FontFaceSize;
            float currentScale = scale;
            float currentX = position.X;
            float currentY = position.Y;
            color = color * alpha;
            Color colorDoubleAlpha = color * alpha;

            foreach (char c in timeString)
            {
                bool flag2 = c == '.';
                if (flag2)
                {
                    currentScale = scale * 0.7f;
                    currentY -= 5f * scale;
                }
                Color colorToUse = (c == ':' || c == '.' || currentScale < scale) ? colorDoubleAlpha : color;
                float advance = (((c == ':' || c == '.') ? spacerWidth : numberWidth) + 4f) * currentScale;
                font.DrawOutline(fontFaceSize, c.ToString(), new Vector2(currentX + advance / 2, currentY), new Vector2(0.5f, 1f), Vector2.One * currentScale, colorToUse, 2f, Color.Black);
                currentX += advance;
            }
        }


        public override void Render()
        {
            base.Render();
            if (fading)
            {
                fadeTime -= Engine.DeltaTime;
                if (fadeTime < 0)
                {
                    createTween(0.6f, (t) =>
                    {
                        Position = Vector2.Lerp(OnscreenPos, OffscreenPos, t.Eased);
                    });
                    fading = false;
                }
            }

            //if (!(drawLerp <= 0f) && fadeTime > 0f)
            {
                ActiveFont.DrawOutline(Name, Position - (NameMeasure.X * Vector2.UnitX / 2 * 0.7f), new Vector2(0f, 1f), Vector2.One * 0.7f, Color.White, 2f, Color.Black);
                string txt = TimeSpan.FromTicks(TimeSpent).ShortGameplayFormat();
                drawTime(Position - (getTimeWidth(txt) * Vector2.UnitX/2) + NameMeasure.Y * Vector2.UnitY*1.2f*0.7f, txt, TimeSpent > TrackedChallenge.TimeLimit ? Color.Gray : Color.Gold);
                txt = TimeSpan.FromTicks(TrackedChallenge.TimeLimit).ShortGameplayFormat();
                drawTime(Position - (getTimeWidth(txt) * Vector2.UnitX / 2 * 0.7f) + NameMeasure.Y * Vector2.UnitY * 1.8f * 0.7f, txt, Color.Gold, 0.7f);
            }
        }

        private float getTimeWidth(string timeString, float scale = 1f)
        {
            float currentScale = scale;
            float currentWidth = 0f;
            foreach (char c in timeString)
            {
                if (c == '.')
                {
                    currentScale = scale * 0.7f;
                }
                currentWidth += (((c == ':' || c == '.') ? spacerWidth : numberWidth) + 4f) * currentScale;
            }
            return currentWidth;
        }

        public static Vector2 OffscreenPos => new Vector2(Engine.Width / 2f, -81f);
        public static Vector2 OnscreenPos => new Vector2(Engine.Width / 2f, 89f);

        static float spacerWidth;
        static float numberWidth;
        static float numberHeight;

        public long TimeSpent => TrackedChallenge.TimeSpent;
    }
}
