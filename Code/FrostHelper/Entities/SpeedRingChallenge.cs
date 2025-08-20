using FrostHelper.Helpers;
using System.IO;

namespace FrostHelper;

[CustomEntity("FrostHelper/SpeedRingChallenge")]
[Tracked]
internal sealed class SpeedRingChallenge : Entity {
    private SpeedRingTimerDisplay _timer;
    private readonly EntityID _id;
    public readonly string ChallengeNameId;
    private readonly Vector2[] _nodes;
    private int _currentNodeId = -1;
    public readonly long TimeLimit;
    private long _startChapterTimer = 0;
    private long _finalTimeSpent = -1;
    private bool _started;
    private bool _finished;
    private Strawberry? _berryToSpawn;
    private float _lerp;
    private readonly float _width;
    private readonly float _height;
    private readonly bool _spawnBerry;
    private Color _ringColor;
    private readonly List<MTexture> _arrowTextures;
    private readonly PlayerPlayback? _playback;
    private Vector2 _initialRespawn;
    private List<SpeedRingChallenge> _disabledChallenges;
    
    private readonly bool _recordPlayback;
    private List<Player.ChaserState>? _chaserStates;
    private readonly string _playbackPath;

    private readonly string _flagOnWin;

    public long TimeSpent => _finished ? _finalTimeSpent : Scene == null ? 0 : SceneAs<Level>().Session.Time - _startChapterTimer;

    public SpeedRingChallenge(EntityData data, Vector2 offset, EntityID id) : base(data.Position + offset) {
        _id = id;
        _nodes = data.NodesOffset(offset);
        TimeLimit = TimeSpan.FromSeconds(data.Float("timeLimit", 1f)).Ticks;
        ChallengeNameId = data.Attr("name", "fh_test");
        _width = data.Width;
        _height = data.Height;
        _spawnBerry = data.Bool("spawnBerry", true);
        _arrowTextures = GFX.Game.GetAtlasSubtextures("util/dasharrow/dasharrow");
        _playbackPath = data.Attr("playbackName");
        _playback = CreatePlayback(data);
        _recordPlayback = data.Bool("recordPlayback");
        _flagOnWin = data.Attr("flagOnWin");

        Depth = Depths.Top;
    }

    private PlayerPlayback? CreatePlayback(EntityData data)
    {
        if (string.IsNullOrWhiteSpace(_playbackPath))
            return null;
        
        if (!PlaybackData.Tutorials.TryGetValue(_playbackPath, out var playbackData)) {
            NotificationHelper.Notify($"Could not find playback data at '{_playbackPath}'");
            return null;
        }
            
        var playbackOffset = new Vector2(data.Float("playbackOffsetX"), data.Float("playbackOffsetY"));
        var playback = new PlayerPlayback(Position + playbackOffset + new Vector2(0, 0), PlayerSpriteMode.Playback, playbackData);
        var playbackStartTrim = data.Float("playbackStartTrim");
        var playbackEndTrim = data.Float("playbackEndTrim");
        
        if (playbackEndTrim < playback.TrimEnd && playbackStartTrim < playbackEndTrim && playbackEndTrim > 0) {
            playback.TrimEnd = playbackEndTrim;
        }

        if (playbackStartTrim < playback.TrimEnd && playbackStartTrim > 0) {
            playback.TrimStart = playbackStartTrim;
            playback.Restart();
        }

        playback.startDelay = 0f;
        playback.Active = false;
        playback.Visible = false;

        return playback;
    }

    public override void Added(Scene scene) {
        base.Added(scene);
        
        if (_playback is not null) {
            scene.Add(_playback);
        }
    }

    public override void Awake(Scene scene) {
        base.Awake(scene);

        var level = scene.ToLevel();
        
        _ringColor = FrostModule.SaveData.IsChallengeBeaten(level.Session.Area.SID, ChallengeNameId, TimeLimit) ? Color.Blue : Color.Gold;

        var last = _nodes.Last();
        Collider = new Hitbox(_width, _height, last.X - Position.X, last.Y - Position.Y);
        if (_spawnBerry) {
            _berryToSpawn = null;
            foreach (var berry in Scene.Entities.OfType<Strawberry>()) {
                if (new Rectangle((int) last.X, (int) last.Y, (int) _width, (int) _height).Contains(new Point((int) berry.Position.X, (int) berry.Position.Y))) {
                    _berryToSpawn = berry;
                    break;
                }
            }
            if (_berryToSpawn == null) {
                NotificationHelper.Notify($"Didn't find a berry inside of the final node of the Speed Ring: {ChallengeNameId}, but there's {Scene.Entities.OfType<Strawberry>().Count()} berries");
            } else {
                _berryToSpawn.Active = _berryToSpawn.Visible = _berryToSpawn.Collidable = false;
            }
        }

        Collider.Position = Vector2.Zero;
    }

    public override void Update() {
        base.Update();
        StopPlaybackIfAboutToLoop();
        Active = Visible = !_finished;
        if (!_finished && CollideCheck<Player>()) {
            if (!_started) {
                _startChapterTimer = SceneAs<Level>().Session.Time;
                Scene.Add(_timer = new SpeedRingTimerDisplay(this));
                _started = true;
                StartPlayback();
                _initialRespawn = SceneAs<Level>().Session.RespawnPoint.GetValueOrDefault();
                _disabledChallenges = Scene.Tracker.SafeGetEntities<SpeedRingChallenge>().Cast<SpeedRingChallenge>().ToList();
                _disabledChallenges.Remove(this);
                foreach (var item in _disabledChallenges) {
                    item.Active = item.Collidable = item.Visible = false;
                }

                if (_recordPlayback)
                    _chaserStates = [];
            }

            Vector2 particlePos = (_currentNodeId == -1 ? Position : _nodes[_currentNodeId]) + Height / 2 * Vector2.UnitY;
            Scene.Add(new SummitCheckpoint.ConfettiRenderer(particlePos));
            Audio.Play("event:/game/07_summit/checkpoint_confetti", particlePos);

            _currentNodeId++;

            if (_currentNodeId + (_spawnBerry ? 1 : 0) < _nodes.Length) {
                Collider.Position = _nodes[_currentNodeId] - Position;
            } else {
                // last node
                if (!_finished) {
                    _finalTimeSpent = TimeSpent;
                    _finished = true;
                    Visible = false;
                    StopPlayback();

                    FrostModule.SaveData.SetChallengeTime(SceneAs<Level>().Session.Area.SID, ChallengeNameId, _finalTimeSpent);

                    if (TimeSpent < TimeLimit) {
                        // Finished the time trial in time
                        Scene.OnEndOfFrame += () => {
                            if (_berryToSpawn is { } berry) {
                                berry.Active = berry.Collidable = true;
                                berry.Seeds = [
                                    new StrawberrySeed(berry, Scene.Tracker.GetEntity<Player>().Position, 1,
                                        SaveData.Instance.CheckStrawberry(berry.ID))
                                ];
                                foreach (var item in berry.Seeds) {
                                    Scene.Add(item);
                                }
                            }

                            SceneAs<Level>().Session.DoNotLoad.Add(_id);

                            if (!string.IsNullOrWhiteSpace(_flagOnWin)) {
                                SceneAs<Level>().Session.SetFlag(_flagOnWin);
                            }
                        };

                        if (_recordPlayback && _chaserStates is { }) {
                            var e = new Entity { Active = true };
                            e.Add(new Coroutine(FinishRecordingRoutine()));
                            Scene.Add(e);
                        }
                    }
                    _timer.FadeOut();
                    foreach (var item in _disabledChallenges) {
                        item.Active = item.Collidable = item.Visible = true;
                    }
                }
            }
        }
        
        if (_started && !_finished) {
            SceneAs<Level>().Session.RespawnPoint = _initialRespawn;
            
            if (_recordPlayback && Scene.Tracker.SafeGetEntity<Player>() is {} player)
                _chaserStates?.Add(new(player));
        }
    }

    private IEnumerator FinishRecordingRoutine() {
        _chaserStates ??= [];
        
        // Wait for 0.5s to record a bit after the finish line:
        float t = 0f;
        while (t <= 0.5f && Scene.Tracker.SafeGetEntity<Player>() is {} player) {
            t += Engine.DeltaTime;
            _chaserStates.Add(new(player));
            yield return null;
        }
        
        var mod = Everest.Content.Get($"Maps/{((Level) Engine.Scene).Session.Area.SID}").Source.Mod;
        if (mod is { PathDirectory: {} modRoot }) {
            var virtPath = $"Tutorials/{_playbackPath}.bin";
            NotificationHelper.Notify($"Saved playback to '{virtPath}' in mod '{mod}'");
            var fullPath = Path.Combine(modRoot, virtPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            ExportWithStartPos(_chaserStates, fullPath, Position);
            _chaserStates.Clear();
        } else {
            NotificationHelper.Notify($"Couldn't save playback:\nMap is zipped or unpackaged!");
        }
    }
    
    // PlaybackData.Export, but the starting position is supplied via argument, instead of using the first chaser state.
    private static void ExportWithStartPos(List<Player.ChaserState> list, string path, Vector2 position)
    {
        float timeStamp = list[0].TimeStamp;
        using BinaryWriter binaryWriter = new(File.OpenWrite(path));
        
        binaryWriter.Write("TIMELINE");
        binaryWriter.Write(2);
        binaryWriter.Write(list.Count);
        foreach (Player.ChaserState chaserState in list)
        {
            binaryWriter.Write(chaserState.Position.X - position.X);
            binaryWriter.Write(chaserState.Position.Y - position.Y);
            binaryWriter.Write(chaserState.TimeStamp - timeStamp);
            binaryWriter.Write(chaserState.Animation);
            binaryWriter.Write((int) chaserState.Facing);
            binaryWriter.Write(chaserState.OnGround);
            binaryWriter.Write(chaserState.HairColor.R);
            binaryWriter.Write(chaserState.HairColor.G);
            binaryWriter.Write(chaserState.HairColor.B);
            binaryWriter.Write(chaserState.Depth);
            binaryWriter.Write(chaserState.Scale.X);
            binaryWriter.Write(chaserState.Scale.Y);
            binaryWriter.Write(chaserState.DashDirection.X);
            binaryWriter.Write(chaserState.DashDirection.Y);
        }
    }
    
    private void StartPlayback() {
        if (_playback is null)
            return;
        
        _playback.Active = true;
        _playback.Restart();
    }
    
    private void StopPlayback() {
        if (_playback is null)
            return;
        
        if (_playback.Visible)
            Audio.Play("event:/new_content/char/tutorial_ghost/disappear", _playback.Position);
        
        _playback.Active = _playback.Visible = false;
    }

    private void StopPlaybackIfAboutToLoop() {
        if (_playback is null || !_playback.Active || _playback.Visible)
            return;
        StopPlayback();
    } 
    
    public override void Render() {
        base.Render();

        if (!Scene.ToLevel().Paused) {
            _lerp += 3f * Engine.DeltaTime;
            if (_lerp >= 1f) {
                _lerp = 0f;
            }
        }

        DrawRing(Collider.Center + Position);

        if (!_started)
            return;

        #region Arrow

        Player player = Scene.Tracker.GetEntity<Player>();
        if (player == null) {
            return;
        }

        float direction = Calc.Angle(player.Center, Center);
        const float scale = 1f;
        MTexture? mtexture = null;
        float rotation = float.MaxValue;
        for (int i = 0; i < 8; i++) {
            float angleDifference = Calc.AngleDiff(6.28318548f * (i / 8f), direction);
            if (Math.Abs(angleDifference) < Math.Abs(rotation)) {
                rotation = angleDifference;
                mtexture = _arrowTextures[i];
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
        float maxRadiusY = MathHelper.Lerp(4f, Height / 2, _lerp);
        float maxRadiusX = MathHelper.Lerp(4f, Width, _lerp);
        Vector2 value = GetVectorAtAngle(0f);
        for (int i = 1; i <= 8; i++) {
            float radians = i * 0.3926991f;
            Vector2 vectorAtAngle = GetVectorAtAngle(radians);
            Draw.Line(position + value, position + vectorAtAngle, _ringColor);
            Draw.Line(position - value, position - vectorAtAngle, _ringColor);
            value = vectorAtAngle;
        }

        Vector2 GetVectorAtAngle(float radians) {
            Vector2 vector = Calc.AngleToVector(radians, 1f);
            Vector2 scaleFactor = new Vector2(MathHelper.Lerp(maxRadiusX, maxRadiusX * 0.5f, Math.Abs(Vector2.Dot(vector, Calc.AngleToVector(0f, 1f)))), MathHelper.Lerp(maxRadiusY, maxRadiusY * 0.5f, Math.Abs(Vector2.Dot(vector, Calc.AngleToVector(0f, 1f)))));
            return vector * scaleFactor;
        }
    }
}

internal sealed class SpeedRingTimerDisplay : Entity {
    private float _fadeTime;
    private bool _fading;

    private readonly SpeedRingChallenge _trackedChallenge;

    private readonly string _name;
    private readonly Vector2 _nameMeasure;

    private static Vector2 OffscreenPos => new Vector2(Engine.Width / 2f, -81f);
    private static Vector2 OnscreenPos => new Vector2(Engine.Width / 2f, 89f);

    private static float _spacerWidth;
    private static float _numberWidth;

    private long TimeSpent => _trackedChallenge.TimeSpent;
    
    public SpeedRingTimerDisplay(SpeedRingChallenge challenge) {
        Tag = Tags.HUD | Tags.PauseUpdate;
        CalculateBaseSizes();
        Add(Wiggler.Create(0.5f, 4f, null, false, false));
        _trackedChallenge = challenge;
        _fadeTime = 3f;

        CreateTween(0.1f, t => {
            Position = Vector2.Lerp(OffscreenPos, OnscreenPos, t.Eased);
        });

        _name = Dialog.Clean(challenge.ChallengeNameId);
        _nameMeasure = ActiveFont.Measure(_name);
    }


    private void CreateTween(float fadeTime, Action<Tween> onUpdate) {
        Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeInOut, fadeTime, true);
        tween.OnUpdate = onUpdate;
        Add(tween);
    }

    public void FadeOut() {
        _fadeTime = 5f;
        _fading = true;
    }

    private static void CalculateBaseSizes() {
        // compute the max size of a digit and separators in the English font, for the timer part.
        PixelFont font = Dialog.Languages["english"].Font;
        float fontFaceSize = Dialog.Languages["english"].FontFaceSize;
        PixelFontSize pixelFontSize = font.Get(fontFaceSize);
        for (int i = 0; i < 10; i++) {
            float digitWidth = pixelFontSize.Measure(i.ToString()).X;
            if (digitWidth > _numberWidth) {
                _numberWidth = digitWidth;
            }
        }
        _spacerWidth = pixelFontSize.Measure('.').X;
    }

    private static void DrawTime(Vector2 position, string timeString, Color color, float scale = 1f, float alpha = 1f) {
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
            float advance = (((c == ':' || c == '.') ? _spacerWidth : _numberWidth) + 4f) * currentScale;
            font.DrawOutline(fontFaceSize, c.ToString(), new Vector2(currentX + advance / 2, currentY), new Vector2(0.5f, 1f), Vector2.One * currentScale, colorToUse, 2f, Color.Black);
            currentX += advance;
        }
    }


    public override void Render() {
        base.Render();
        if (_fading) {
            _fadeTime -= Engine.DeltaTime;
            if (_fadeTime < 0) {
                CreateTween(0.6f, (t) => {
                    Position = Vector2.Lerp(OnscreenPos, OffscreenPos, t.Eased);
                });
                _fading = false;
            }
        }

        //if (!(drawLerp <= 0f) && fadeTime > 0f)
        {
            ActiveFont.DrawOutline(_name, Position - (_nameMeasure.X * Vector2.UnitX / 2 * 0.7f), new Vector2(0f, 1f), Vector2.One * 0.7f, Color.White, 2f, Color.Black);
            string txt = TimeSpan.FromTicks(TimeSpent).ShortGameplayFormat();
            DrawTime(Position - (GetTimeWidth(txt) * Vector2.UnitX / 2) + _nameMeasure.Y * Vector2.UnitY * 1.2f * 0.7f, txt, TimeSpent > _trackedChallenge.TimeLimit ? Color.Gray : Color.Gold);
            txt = TimeSpan.FromTicks(_trackedChallenge.TimeLimit).ShortGameplayFormat();
            DrawTime(Position - (GetTimeWidth(txt) * Vector2.UnitX / 2 * 0.7f) + _nameMeasure.Y * Vector2.UnitY * 1.8f * 0.7f, txt, Color.Gold, 0.7f);
        }
    }

    private static float GetTimeWidth(string timeString, float scale = 1f) {
        float currentScale = scale;
        float currentWidth = 0f;
        foreach (char c in timeString) {
            if (c == '.') {
                currentScale = scale * 0.7f;
            }
            currentWidth += (((c == ':' || c == '.') ? _spacerWidth : _numberWidth) + 4f) * currentScale;
        }
        return currentWidth;
    }
}
