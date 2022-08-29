namespace FrostHelper;

[CustomEntity("FrostHelper/ChronosTrigger")]
public class ChronosTrigger : Trigger {
    public float StartTime;
    public float CurrentTime;

    public new bool Triggered = false;

    Player player;

    public ChronosTrigger(EntityData data, Vector2 offset) : base(data, offset) {
        StartTime = CurrentTime = data.Float("time", 2f);
    }

    public override void Removed(Scene scene) {
        base.Removed(scene);
        On.Celeste.Player.UseRefill -= Player_UseRefill;
        On.Celeste.HeartGem.Collect -= HeartGem_Collect;
    }

    public override void OnEnter(Player player) {
        base.OnEnter(player);
        if (!Triggered) {
            ChronosDisplay display;
            if ((display = player.Scene.Tracker.GetEntity<ChronosDisplay>()) != null) {
                display.TrackedTrigger = this;
            } else {
                Scene.Add(new ChronosDisplay(this));
            }
            Triggered = true;
            this.player = player;
            On.Celeste.Player.UseRefill += Player_UseRefill;
            On.Celeste.HeartGem.Collect += HeartGem_Collect;
        }
    }

    private void HeartGem_Collect(On.Celeste.HeartGem.orig_Collect orig, HeartGem self, Player player) {
        if (Scene == null) {
            On.Celeste.Player.UseRefill -= Player_UseRefill;
            On.Celeste.HeartGem.Collect -= HeartGem_Collect;
            orig(self, player);
            return;
        }
        orig(self, player);
        RemoveSelf();
        ChronosDisplay display;
        if ((display = player.Scene.Tracker.GetEntity<ChronosDisplay>()) != null)
            display.RemoveSelf();
    }

    private bool Player_UseRefill(On.Celeste.Player.orig_UseRefill orig, Player self, bool twoDashes) {
        if (Scene == null) {
            On.Celeste.Player.UseRefill -= Player_UseRefill;
            On.Celeste.HeartGem.Collect -= HeartGem_Collect;
            return orig(self, twoDashes);
        }

        bool ret = orig(self, twoDashes);
        if (CurrentTime < 0) {
            Distort.Anxiety = 0f;
            Engine.TimeRate = 1f;
        }
        CurrentTime = StartTime;
        return true;
    }

    bool died;
    public override void Update() {
        base.Update();
        if (player != null && !player.Dead && Triggered && !player.CollideCheck<PlaybackBillboard>()) {
            if (CurrentTime <= 0 && !died) {
                // time ran out
                Engine.TimeRate -= Engine.DeltaTime * 3f;
                Distort.Anxiety += Engine.DeltaTime * 1.1f;
                if (Engine.TimeRate < 0.1f) {
                    Engine.TimeRate = 0f;
                    player.StateMachine.State = Player.StDummy;
                    player.StateMachine.Locked = true;

                    if (Input.Talk.Pressed || Input.MenuConfirm.Pressed || Input.Dash.Pressed || Input.Jump.Pressed) {
                        player.Die(Vector2.Zero, true);
                        Engine.TimeRate = 1f;
                        died = true;
                    }
                }
            } else {
                CurrentTime -= Engine.DeltaTime;
            }
        } else if (CurrentTime < 0 && player != null && !player.Dead && player.CollideCheck<PlaybackBillboard>()) {
            Distort.Anxiety = 0f;
            Engine.TimeRate = 1f;
        }
    }
}

[Tracked]
public class ChronosDisplay : Entity {
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
