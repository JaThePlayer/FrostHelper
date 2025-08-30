using FrostHelper.Helpers;
using System.IO;

namespace FrostHelper;

[Tracked(Inherited = true)]
internal abstract class SpeedRingChallenge : Entity {
    private SpeedRingTimerDisplay _timer;
    private readonly EntityID _id;
    public readonly string ChallengeNameId;

    protected int CurrentNodeId = -1;
    public readonly long TimeLimit;
    private long _startChapterTimer = 0;
    private long _finalTimeSpent = -1;
    private bool _started;
    private bool _finished;
    private Entity? _berryToSpawn;
    protected readonly bool SpawnBerry;
    private readonly List<MTexture> _arrowTextures;
    private readonly PlayerPlayback? _playback;
    private Vector2 _initialRespawn;
    private List<SpeedRingChallenge> _disabledChallenges;
    
    private readonly bool _recordPlayback;
    private List<Player.ChaserState>? _chaserStates;
    private readonly string _playbackPath;

    private readonly string _flagOnWin;

    private readonly Color _colorNotYetBeaten, _colorBeatenBefore;

    private readonly CounterAccessor _progressCounter;

    public long TimeSpent => _finished ? _finalTimeSpent : Scene == null ? 0 : SceneAs<Level>().Session.Time - _startChapterTimer;

    public SpeedRingChallenge(EntityData data, Vector2 offset, EntityID id) : base(data.Position + offset) {
        _id = id;

        TimeLimit = TimeSpan.FromSeconds(data.Float("timeLimit", 1f)).Ticks;
        ChallengeNameId = data.Attr("name", "fh_test");
        SpawnBerry = data.Bool("spawnBerry", true);
        _arrowTextures = GFX.Game.GetAtlasSubtextures("util/dasharrow/dasharrow");
        _playbackPath = data.Attr("playbackName");
        _playback = CreatePlayback(data);
        _recordPlayback = data.Bool("recordPlayback");
        _flagOnWin = data.Attr("flagOnWin");

        _colorNotYetBeaten = data.GetColor("colorUnbeaten", "ffd700");
        _colorBeatenBefore = data.GetColor("colorBeaten", "0000ff");
        _progressCounter = new CounterAccessor(data.Attr("progressCounter"));
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

    private void SetProgressCounter(Scene scene, int value) {
        if (!string.IsNullOrWhiteSpace(_progressCounter.CounterName)) {
            _progressCounter.Set(scene.ToLevel().Session, value);
        }
    }
    
    private void IncrementProgressCounter(Scene scene) {
        if (!string.IsNullOrWhiteSpace(_progressCounter.CounterName)) {
            var session = scene.ToLevel().Session;
            _progressCounter.Set(session, _progressCounter.Get(session) + 1);
        }
    }

    public override void Added(Scene scene) {
        base.Added(scene);

        SetProgressCounter(scene, -1);
        
        if (_playback is not null) {
            scene.Add(_playback);
        }

        scene.Add(new ArrowDisplay(this));
    }

    protected bool IsChallengeBeaten(Scene scene) =>
        FrostModule.SaveData.IsChallengeBeaten(scene.ToLevel().Session.Area.SID, ChallengeNameId, TimeLimit);
    
    protected Color GetRingColor(Scene scene)
        => IsChallengeBeaten(scene) ? _colorBeatenBefore : _colorNotYetBeaten;

    private Entity? FindStrawberry() {
        Strawberry? berryToSpawn = null;
        var rect = GetStrawberrySearchHitbox();
        foreach (var berry in Scene.Entities.OfType<Strawberry>()) {
            if (rect.Contains(new Point((int) berry.Position.X, (int) berry.Position.Y))) {
                berryToSpawn = berry;
                break;
            }
        }
        if (berryToSpawn == null) {
            NotificationHelper.Notify($"Didn't find a berry inside of the final node of the Speed Ring: {ChallengeNameId}, but there's {Scene.Entities.OfType<Strawberry>().Count()} berries");
        } else {
            berryToSpawn.Active = berryToSpawn.Visible = berryToSpawn.Collidable = false;
        }

        return berryToSpawn;
    }

    protected abstract Rectangle GetStrawberrySearchHitbox();

    protected abstract void MoveToNextNode();

    public override void Awake(Scene scene) {
        base.Awake(scene);
        
        if (SpawnBerry) {
            _berryToSpawn = FindStrawberry();
        }
    }

    protected abstract Vector2 NodeCenterPos(int index);

    protected abstract int NodeCount();

    protected abstract bool CheckNodeCollision();
    
    public override void Update() {
        base.Update();
        StopPlaybackIfAboutToLoop();
        Active = Visible = !_finished;
        if (!_finished && CheckNodeCollision()) {
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

            Vector2 particlePos = NodeCenterPos(CurrentNodeId) + base.Height / 2 * Vector2.UnitY;
            Scene.Add(new SummitCheckpoint.ConfettiRenderer(particlePos));
            Audio.Play("event:/game/07_summit/checkpoint_confetti", particlePos);

            CurrentNodeId++;
            
            IncrementProgressCounter(Scene);

            if (CurrentNodeId < NodeCount()) {
                MoveToNextNode();
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
                                berry.Active = true;

                                switch (berry)
                                {
                                    case IStrawberrySeeded seeded:
                                    {
                                        seeded.Seeds.Add(
                                            new GenericStrawberrySeed(seeded, Scene.Tracker.GetEntity<Player>().Position, 1,
                                                SaveData.Instance.CheckStrawberry(berry.SourceId))
                                        );
                                        foreach (var item in seeded.Seeds)
                                            Scene.Add(item);
                                        break;
                                    }
                                    case Strawberry strawberry:
                                    {
                                        strawberry.Seeds = [
                                            new StrawberrySeed(strawberry, Scene.Tracker.GetEntity<Player>().Position, 1,
                                                SaveData.Instance.CheckStrawberry(strawberry.ID))
                                        ];
                                        foreach (var item in strawberry.Seeds)
                                            Scene.Add(item);
                                        break;
                                    }
                                }
                            }

                            SceneAs<Level>().Session.DoNotLoad.Add(_id);

                            if (!string.IsNullOrWhiteSpace(_flagOnWin)) {
                                SceneAs<Level>().Session.SetFlag(_flagOnWin);
                            }
                        };

                        if (_recordPlayback && _chaserStates is { }) {
                            var e = new Entity { Active = true };
                            e.AddTag(Tags.FrozenUpdate);
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
            t += 1f / 60f;
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

    protected abstract void RenderRing();

    protected abstract Vector2 ArrowPos();
    
    public override void Render() {
        RenderRing();
        
        base.Render();
    }

    internal void DrawArrow()
    {
        if (!_started || _finished)
            return;
        
        Player player = Scene.Tracker.GetEntity<Player>();
        if (player == null) {
            return;
        }

        var arrowPos = ArrowPos();
        float direction = Calc.Angle(player.Center, arrowPos);
        var dist = float.Min(40f, (player.Center - arrowPos).Length());
        var alpha = Calc.Map(float.Min(120f, (player.Center - arrowPos).Length()), 0, 120f, 0f, 1f);
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
            mtexture.DrawOutlineCentered((player.Center + Calc.AngleToVector(direction, dist)).Round(), Color.White, Ease.BounceOut(scale * alpha), rotation);
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

    private string? _pb;
    private long _pbTime;
    
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


    private Tween CreateTween(float fadeTime, Action<Tween> onUpdate) {
        Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeInOut, fadeTime, true);
        tween.OnUpdate = onUpdate;
        Add(tween);

        return tween;
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

    public override void Awake(Scene scene) {
        base.Awake(scene);

        _pbTime = FrostModule.SaveData.GetChallengeTime(SceneAs<Level>().Session.Area.SID, _trackedChallenge.ChallengeNameId);
    }

    public override void Render() {
        base.Render();
        if (_fading) {
            _fadeTime -= Engine.DeltaTime;
            if (_fadeTime < 0) {
                var t = CreateTween(0.6f, (t) => {
                    Position = Vector2.Lerp(OnscreenPos, OffscreenPos, t.Eased);
                });
                t.OnComplete += _ => RemoveSelf();
                _fading = false;
            }
        }

        //if (!(drawLerp <= 0f) && fadeTime > 0f)
        {
            var scale = 0.7f;
            ActiveFont.DrawOutline(_name, Position - (_nameMeasure.X * Vector2.UnitX / 2 * scale), new Vector2(0f, 1f), Vector2.One * scale, Color.White, 2f, Color.Black);
            string txt = TimeSpan.FromTicks(TimeSpent).ShortGameplayFormat();
            DrawTime(Position - (GetTimeWidth(txt) * Vector2.UnitX / 2) + _nameMeasure.Y * Vector2.UnitY * 1.2f * scale, txt, TimeSpent > _trackedChallenge.TimeLimit ? Color.Gray : Color.Gold);
            txt = TimeSpan.FromTicks(_trackedChallenge.TimeLimit).ShortGameplayFormat();
            DrawTime(Position - (GetTimeWidth(txt) * Vector2.UnitX / 2 * scale) + _nameMeasure.Y * Vector2.UnitY * 1.8f * scale, txt, Color.Gold, scale);

            if (_pbTime > 0) {
                scale = 0.6f;
                var pbDialog = Dialog.Clean("FH_PB");
                _pb ??= $"{pbDialog}: {TimeSpan.FromTicks(_pbTime).ShortGameplayFormat()}";
                DrawTime(Position - ((GetTimeWidth(_pb) + GetTimeWidth($"{pbDialog}: ")) * Vector2.UnitX / 2 * scale) + _nameMeasure.Y * Vector2.UnitY * 2.5f * 0.7f, _pb, Color.LightSkyBlue, 0.6f);

                if (_fading && TimeSpent < _pbTime) {
                    var newPbText = Dialog.Clean("FH_NewPB");
                    DrawTime(Position - (GetTimeWidth(newPbText) * Vector2.UnitX / 2 * scale) + _nameMeasure.Y * Vector2.UnitY * 3.1f * 0.7f, newPbText, Color.LightSkyBlue, 0.6f);
                }
            }
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

internal sealed class ArrowDisplay : Entity {
    private readonly SpeedRingChallenge _challenge;
    
    public ArrowDisplay(SpeedRingChallenge challenge) {
        Depth = Depths.Top;
        _challenge = challenge;
        Visible = true;
        Active = false;
    }

    public override void Render() {
        _challenge.DrawArrow();
    }
}