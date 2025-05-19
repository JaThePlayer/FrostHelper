namespace FrostHelper;

[CustomEntity("FrostHelper/ChronosTrigger")]
[Tracked]
internal sealed class ChronosTrigger : Trigger {
    public readonly float StartTime;
    public float CurrentTime;
    private bool _triggered = false;
    private bool _died;
    Player _player;


    private static bool _hooksLoaded;
    private static void LoadHooksIfNeeded() {
        if (_hooksLoaded)
            return;
        _hooksLoaded = true;
        
        On.Celeste.Player.UseRefill += Player_UseRefill;
        On.Celeste.HeartGem.Collect += HeartGem_Collect;
    }

    [OnUnload]
    internal static void UnloadHooks() {
        if (!_hooksLoaded)
            return;
        _hooksLoaded = false;
        
        On.Celeste.Player.UseRefill -= Player_UseRefill;
        On.Celeste.HeartGem.Collect -= HeartGem_Collect;
    }
    
    public ChronosTrigger(EntityData data, Vector2 offset) : base(data, offset) {
        LoadHooksIfNeeded();
        StartTime = CurrentTime = data.Float("time", 2f);
    }

    public override void OnEnter(Player player) {
        base.OnEnter(player);
        
        if (_triggered)
            return;
        
        if (player.Scene.Tracker.SafeGetEntity<ChronosDisplay>() is {} display) {
            display.TrackedTrigger = this;
        } else {
            Scene.Add(new ChronosDisplay(this));
        }
        
        _triggered = true;
        _player = player;
    }

    private static bool Player_UseRefill(On.Celeste.Player.orig_UseRefill orig, Player self, bool twoDashes) {
        if (self.Scene?.Tracker.SafeGetEntity<ChronosTrigger>() is not {} chronosTrigger) {
            return orig(self, twoDashes);
        }

        _ = orig(self, twoDashes);
        if (chronosTrigger.CurrentTime < 0) {
            Distort.Anxiety = 0f;
            Engine.TimeRate = 1f;
        }
        
        chronosTrigger.CurrentTime = chronosTrigger.StartTime;
        return true; // we'll always use the refill.
    }

    private static void HeartGem_Collect(On.Celeste.HeartGem.orig_Collect orig, HeartGem self, Player player) {
        orig(self, player);
        if (self.Scene?.Tracker.SafeGetEntity<ChronosTrigger>() is not {} chronosTrigger)
            return;
        
        chronosTrigger.RemoveSelf();
        ChronosDisplay? display;
        if ((display = player.Scene.Tracker.SafeGetEntity<ChronosDisplay>()) != null)
            display.RemoveSelf();
    }

    public override void Update() {
        base.Update();
        if (_player is not { Dead: false })
            return;
        
        if (_triggered && !_player.CollideCheck<PlaybackBillboard>()) {
            if (CurrentTime <= 0 && !_died) {
                // time ran out
                Engine.TimeRate -= Engine.DeltaTime * 3f;
                Distort.Anxiety += Engine.DeltaTime * 1.1f;
                if (Engine.TimeRate < 0.1f) {
                    Engine.TimeRate = 0f;
                    _player.StateMachine.State = Player.StDummy;
                    _player.StateMachine.Locked = true;

                    if (Input.Talk.Pressed || Input.MenuConfirm.Pressed || Input.Dash.Pressed || Input.Jump.Pressed) {
                        _player.Die(Vector2.Zero, true);
                        Engine.TimeRate = 1f;
                        _died = true;
                    }
                }
            } else {
                CurrentTime -= Engine.DeltaTime;
            }
        } else if (CurrentTime < 0 && _player.CollideCheck<PlaybackBillboard>()) {
            Distort.Anxiety = 0f;
            Engine.TimeRate = 1f;
        }
    }
}

[Tracked]
internal sealed class ChronosDisplay : Entity {
    float fadeTime;
    bool fading;
    public ChronosTrigger TrackedTrigger;

    private void createTween(float fadeTime, Action<Tween> onUpdate) {
        Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeInOut, fadeTime, true);
        tween.OnUpdate = onUpdate;
        Add(tween);
    }

    public ChronosDisplay(ChronosTrigger challenge) {
        Tag = Tags.HUD | Tags.PauseUpdate | Tags.Persistent;

        Add(Wiggler.Create(0.5f, 4f, null, false, false));
        TrackedTrigger = challenge;
        fadeTime = 3f;

        createTween(0.1f, t => {
            Position = Vector2.Lerp(OffscreenPos, OnscreenPos, t.Eased);
        });
    }

    public override void Render() {
        base.Render();
        if (fading) {
            fadeTime -= Engine.DeltaTime;
            if (fadeTime < 0) {
                createTween(0.6f, (t) => {
                    Position = Vector2.Lerp(OnscreenPos, OffscreenPos, t.Eased);
                });
                fading = false;
            }
        }

        // base
        Draw.Rect(Position, Engine.Width / 4f, 40f, Color.Gray);
        // fill
        Draw.Rect(Position, Engine.Width / 4f * (Math.Max(TrackedTrigger.CurrentTime, 0f) / TrackedTrigger.StartTime), 40f, Color.Green);
        // outline
        Draw.HollowRect(Position + Vector2.UnitY * 2f, Engine.Width / 4f, 40f, Color.White);
    }

    public static Vector2 OffscreenPos => new Vector2(Engine.Width / 2f - (Engine.Width / 8f), -81f);
    public static Vector2 OnscreenPos => new Vector2(Engine.Width / 2f - (Engine.Width / 8f), 40f);
}
