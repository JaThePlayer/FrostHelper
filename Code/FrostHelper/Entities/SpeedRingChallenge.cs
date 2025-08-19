using FrostHelper.Helpers;

namespace FrostHelper;

[CustomEntity("FrostHelper/SpeedRingChallenge")]
[Tracked]
public class SpeedRingChallenge : Entity {
    SpeedRingTimerDisplay timer;
    public readonly EntityID ID;
    public readonly string ChallengeNameID;
    Vector2[] nodes;
    public int currentNodeID = -1;
    public readonly long TimeLimit;
    public long StartChapterTimer = 0;
    public long FinalTimeSpent = -1;
    bool started;
    public bool Finished;
    public Strawberry BerryToSpawn;
    float lerp;
    float width, height;
    bool spawnBerry;
    public Color RingColor;
    readonly List<MTexture> ArrowTextures;
    private PlayerPlayback? playback;

    public long TimeSpent => Finished ? FinalTimeSpent : Scene == null ? 0 : SceneAs<Level>().Session.Time - StartChapterTimer;

    

    public SpeedRingChallenge(EntityData data, Vector2 offset, EntityID id) : base(data.Position + offset) {
        ID = id;
        nodes = data.NodesOffset(offset);
        TimeLimit = TimeSpan.FromSeconds(data.Float("timeLimit", 1f)).Ticks;
        ChallengeNameID = data.Attr("name", "fh_test");
        width = data.Width;
        height = data.Height;
        spawnBerry = data.Bool("spawnBerry", true);
        ArrowTextures = GFX.Game.GetAtlasSubtextures("util/dasharrow/dasharrow");
        string playbackName = data.Attr("playbackName");
        if (!string.IsNullOrWhiteSpace(playbackName)) {
            if (!PlaybackData.Tutorials.TryGetValue(playbackName, out var playbackData))
                throw new InvalidOperationException($"Could not find playback data for {playbackName}");
            playback = new PlayerPlayback(Position, PlayerSpriteMode.Playback, playbackData);
            playback.startDelay = 0f;
            playback.Active = false;
            playback.Visible = false;
            
            
        }

        Depth = Depths.Top;
    }

    public override void Added(Scene scene) {
        base.Added(scene);
        if (playback is not null) {
            scene.Add(playback);
        };
    }
    public override void Awake(Scene scene) {
        base.Awake(scene);

        RingColor = FrostModule.SaveData.IsChallengeBeaten(SceneAs<Level>().Session.Area.SID, ChallengeNameID, TimeLimit) ? Color.Blue : Color.Gold;

        var last = nodes.Last();
        if (spawnBerry) {
            Collider = new Hitbox(width, height, last.X - Position.X, last.Y - Position.Y);
            BerryToSpawn = null!;
            foreach (var berry in Scene.Entities.OfType<Strawberry>()) {
                if (new Rectangle((int) last.X, (int) last.Y, (int) width, (int) height).Contains(new Point((int) berry.Position.X, (int) berry.Position.Y))) {
                    BerryToSpawn = berry;
                    break;
                }
            }
            if (BerryToSpawn == null) {
                throw new Exception($"Didn't find a berry inside of the final node of the Speed Ring: {ChallengeNameID}, but there's {Scene.Entities.OfType<Strawberry>().Count()} berries");
            }
            BerryToSpawn.Active = BerryToSpawn.Visible = BerryToSpawn.Collidable = false;
        }

        Collider.Position = Vector2.Zero;
    }

    Vector2 initialRespawn;

    List<SpeedRingChallenge> disabledChallenges;

    public override void Update() {
        base.Update();
        Active = Visible = !Finished;
        if (!Finished && CollideCheck<Player>()) {
            if (!started) {
                StartChapterTimer = SceneAs<Level>().Session.Time;
                Scene.Add(timer = new SpeedRingTimerDisplay(this));
                started = true;
                //This is where the playback should start
                StartPlayback();
                initialRespawn = SceneAs<Level>().Session.RespawnPoint.GetValueOrDefault();
                disabledChallenges = Scene.Tracker.SafeGetEntities<SpeedRingChallenge>().Cast<SpeedRingChallenge>().ToList();
                disabledChallenges.Remove(this);
                foreach (var item in disabledChallenges) {
                    item.Active = item.Collidable = item.Visible = false;
                }
            }

            Vector2 particlePos = (currentNodeID == -1 ? Position : nodes[currentNodeID]) + Height / 2 * Vector2.UnitY;
            Scene.Add(new SummitCheckpoint.ConfettiRenderer(particlePos));
            Audio.Play("event:/game/07_summit/checkpoint_confetti", particlePos);

            currentNodeID++;

            if (currentNodeID + 1 < nodes.Length) {
                Collider.Position = nodes[currentNodeID] - Position;
            } else {
                // last node
                if (!Finished) {
                    FinalTimeSpent = TimeSpent;
                    Finished = true;
                    Visible = false;
                    StopPlayback();

                    FrostModule.SaveData.SetChallengeTime(SceneAs<Level>().Session.Area.SID, ChallengeNameID, FinalTimeSpent);

                    if (TimeSpent < TimeLimit) {
                        // Finished the time trial in time
                        Scene.OnEndOfFrame += () => {
                            BerryToSpawn.Active = BerryToSpawn.Collidable = true;
                            BerryToSpawn.Seeds = new List<StrawberrySeed>
                            {
                                new StrawberrySeed(BerryToSpawn, Scene.Tracker.GetEntity<Player>().Position, 1, SaveData.Instance.CheckStrawberry(BerryToSpawn.ID))
                            };
                            foreach (var item in BerryToSpawn.Seeds) {
                                Scene.Add(item);
                            }
                            SceneAs<Level>().Session.DoNotLoad.Add(ID);
                        };
                    }
                    timer.FadeOut();
                    foreach (var item in disabledChallenges) {
                        item.Active = item.Collidable = item.Visible = true;
                    }
                }
            }
        }
        if (started && !Finished) {
            SceneAs<Level>().Session.RespawnPoint = initialRespawn;
        }
    }

    private void StartPlayback() {
        if (playback is null) return;
        playback.Active = true;
        playback.Restart();
    }

    private void StopPlayback() {
        if (playback is null) return;
        if (playback.Visible)
            Audio.Play("event:/new_content/char/tutorial_ghost/disappear", playback.Position);
        playback.Active = playback.Visible = false;
    }

    public override void Render() {
        base.Render();
        if (!(Scene as Level)!.Paused) {
            lerp += 3f * Engine.DeltaTime;
            if (lerp >= 1f) {
                lerp = 0f;
            }
        }

        DrawRing(Collider.Center + Position);//currentNodeID == -1 ? Collider.Center : nodes[currentNodeID]);

        if (!started)
            return;

        #region Arrow

        Player player = Scene.Tracker.GetEntity<Player>();
        if (player == null) {
            return;
        }

        float direction = Calc.Angle(player.Center, Center);
        float scale = 1f;
        MTexture? mtexture = null;
        float rotation = float.MaxValue;
        for (int i = 0; i < 8; i++) {
            float angleDifference = Calc.AngleDiff(6.28318548f * (i / 8f), direction);
            if (Math.Abs(angleDifference) < Math.Abs(rotation)) {
                rotation = angleDifference;
                mtexture = ArrowTextures[i];
            }
        }
        if (mtexture != null) {
            if (Math.Abs(rotation) < 0.05f) {
                rotation = 0f;
            }
            mtexture.DrawOutlineCentered((player.Center + Calc.AngleToVector(direction, 40f)).Round(), Color.White, Ease.BounceOut(scale), rotation);
        }
        #endregion
    }


    private void DrawRing(Vector2 position) {
        float maxRadiusY = MathHelper.Lerp(4f, Height / 2, lerp);
        float maxRadiusX = MathHelper.Lerp(4f, Width, lerp);
        Vector2 value = GetVectorAtAngle(0f);
        for (int i = 1; i <= 8; i++) {
            float radians = i * 0.3926991f;
            Vector2 vectorAtAngle = GetVectorAtAngle(radians);
            Draw.Line(position + value, position + vectorAtAngle, RingColor);
            Draw.Line(position - value, position - vectorAtAngle, RingColor);
            value = vectorAtAngle;
        }

        Vector2 GetVectorAtAngle(float radians) {
            Vector2 vector = Calc.AngleToVector(radians, 1f);
            Vector2 scaleFactor = new Vector2(MathHelper.Lerp(maxRadiusX, maxRadiusX * 0.5f, Math.Abs(Vector2.Dot(vector, Calc.AngleToVector(0f, 1f)))), MathHelper.Lerp(maxRadiusY, maxRadiusY * 0.5f, Math.Abs(Vector2.Dot(vector, Calc.AngleToVector(0f, 1f)))));
            return vector * scaleFactor;
        }
    }
}

public class SpeedRingTimerDisplay : Entity {
    float fadeTime;
    bool fading;

    readonly SpeedRingChallenge TrackedChallenge;

    string Name;
    Vector2 NameMeasure;

    public SpeedRingTimerDisplay(SpeedRingChallenge challenge) {
        Tag = Tags.HUD | Tags.PauseUpdate;
        CalculateBaseSizes();
        Add(Wiggler.Create(0.5f, 4f, null, false, false));
        TrackedChallenge = challenge;
        fadeTime = 3f;

        CreateTween(0.1f, t => {
            Position = Vector2.Lerp(OffscreenPos, OnscreenPos, t.Eased);
        });

        Name = Dialog.Clean(challenge.ChallengeNameID);
        NameMeasure = ActiveFont.Measure(Name);
    }


    private void CreateTween(float fadeTime, Action<Tween> onUpdate) {
        Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeInOut, fadeTime, true);
        tween.OnUpdate = onUpdate;
        Add(tween);
    }

    public void FadeOut() {
        fadeTime = 5f;
        fading = true;
    }

    private void CalculateBaseSizes() {
        // compute the max size of a digit and separators in the English font, for the timer part.
        PixelFont font = Dialog.Languages["english"].Font;
        float fontFaceSize = Dialog.Languages["english"].FontFaceSize;
        PixelFontSize pixelFontSize = font.Get(fontFaceSize);
        for (int i = 0; i < 10; i++) {
            float digitWidth = pixelFontSize.Measure(i.ToString()).X;
            if (digitWidth > numberWidth) {
                numberWidth = digitWidth;
            }
        }
        spacerWidth = pixelFontSize.Measure('.').X;
    }

    private void DrawTime(Vector2 position, string timeString, Color color, float scale = 1f, float alpha = 1f) {
        PixelFont font = Dialog.Languages["english"].Font;
        float fontFaceSize = Dialog.Languages["english"].FontFaceSize;
        float currentScale = scale;
        float currentX = position.X;
        float currentY = position.Y;
        color *= alpha;
        Color colorDoubleAlpha = color * alpha;

        foreach (char c in timeString) {
            bool flag2 = c == '.';
            if (flag2) {
                currentScale = scale * 0.7f;
                currentY -= 5f * scale;
            }
            Color colorToUse = (c == ':' || c == '.' || currentScale < scale) ? colorDoubleAlpha : color;
            float advance = (((c == ':' || c == '.') ? spacerWidth : numberWidth) + 4f) * currentScale;
            font.DrawOutline(fontFaceSize, c.ToString(), new Vector2(currentX + advance / 2, currentY), new Vector2(0.5f, 1f), Vector2.One * currentScale, colorToUse, 2f, Color.Black);
            currentX += advance;
        }
    }


    public override void Render() {
        base.Render();
        if (fading) {
            fadeTime -= Engine.DeltaTime;
            if (fadeTime < 0) {
                CreateTween(0.6f, (t) => {
                    Position = Vector2.Lerp(OnscreenPos, OffscreenPos, t.Eased);
                });
                fading = false;
            }
        }

        //if (!(drawLerp <= 0f) && fadeTime > 0f)
        {
            ActiveFont.DrawOutline(Name, Position - (NameMeasure.X * Vector2.UnitX / 2 * 0.7f), new Vector2(0f, 1f), Vector2.One * 0.7f, Color.White, 2f, Color.Black);
            string txt = TimeSpan.FromTicks(TimeSpent).ShortGameplayFormat();
            DrawTime(Position - (GetTimeWidth(txt) * Vector2.UnitX / 2) + NameMeasure.Y * Vector2.UnitY * 1.2f * 0.7f, txt, TimeSpent > TrackedChallenge.TimeLimit ? Color.Gray : Color.Gold);
            txt = TimeSpan.FromTicks(TrackedChallenge.TimeLimit).ShortGameplayFormat();
            DrawTime(Position - (GetTimeWidth(txt) * Vector2.UnitX / 2 * 0.7f) + NameMeasure.Y * Vector2.UnitY * 1.8f * 0.7f, txt, Color.Gold, 0.7f);
        }
    }

    private float GetTimeWidth(string timeString, float scale = 1f) {
        float currentScale = scale;
        float currentWidth = 0f;
        foreach (char c in timeString) {
            if (c == '.') {
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

    public long TimeSpent => TrackedChallenge.TimeSpent;
}
