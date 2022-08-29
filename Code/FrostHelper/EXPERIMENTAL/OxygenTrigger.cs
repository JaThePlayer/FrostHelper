namespace FrostHelper.EXPERIMENTAL; 

[CustomEntity("FrostHelper/OxygenLossTrigger")]

public class OxygenLossTrigger : Trigger {
    private OxygenManager Manager;

    public float LossPerSecond;


    public OxygenLossTrigger(EntityData data, Vector2 offset) : base(data, offset) {
        LossPerSecond = data.Float("lossPerSecond", 1f);
    }

    public override void Added(Scene scene) {
        base.Added(scene);

        Manager = ControllerHelper<OxygenManager>.AddToSceneIfNeeded(scene);

        // block berries when inside the trigger
        scene.Add(new BlockField(Position, (int)Width, (int) Height));
    }

    public override void OnEnter(Player player) {
        base.OnEnter(player);
    }

    public override void OnStay(Player player) {
        base.OnStay(player);

        if (!Manager.ConsumedOxygen)
            Manager.ConsumeOxygen(LossPerSecond * Engine.DeltaTime);
    }

    public override void OnLeave(Player player) {
        base.OnLeave(player);
    }
}

[Tracked]
public class OxygenManager : Entity {
    public const float DEFAULT_MAX_OXYGEN = 1.0f;

    public float MaxOxygen = DEFAULT_MAX_OXYGEN;
    public float Oxygen = DEFAULT_MAX_OXYGEN;

    public float PassiveRegenPerSecond = DEFAULT_MAX_OXYGEN;
    
    public bool ConsumedOxygen;

    public OxygenDisplay Display;

    public float SuffocationTimer = 1f;

    public OxygenManager() {
        LoadIfNeeded();

        Depth = Depths.Top;
    }

    public void ConsumeOxygen(float amt) {
        Oxygen = Calc.Approach(Oxygen, 0f, amt);
        ConsumedOxygen = true;
    }

    public void RefillOxygen(bool overflow) {
        Oxygen = overflow ? MaxOxygen * 1.75f : MaxOxygen;
    }

    private static float TimeRateDelta => Engine.DeltaTime * 2.75f;

    public override void Update() {
        base.Update();
        if (Scene.Tracker.GetEntity<Player>() is { Dead: false } player) {
            if (ConsumedOxygen) {
                ConsumedOxygen = false;
            } else if (Oxygen < MaxOxygen) {
                // TODO: Quadratic easing to make this a bit nicer
                Oxygen = Calc.Approach(Oxygen, MaxOxygen, PassiveRegenPerSecond * Engine.DeltaTime);
            }

            if (Oxygen <= 0f && !SaveData.Instance.Assists.Invincible) {
                SuffocationTimer -= TimeRateDelta;
                if (SuffocationTimer < 0.05f) {
                    SuffocationTimer = 0f;
                    player.Die(Vector2.Zero, true);
                    //SuffocationTimer = 1f;
                }
            } else {
                SuffocationTimer = Calc.Approach(SuffocationTimer, 1f, TimeRateDelta);
            }
        }

    }

    public override void Added(Scene scene) {
        base.Added(scene);
        Display = new OxygenDisplay(this);
        scene.Add(Display);
    }

    private static bool _hooksLoaded = false;

    public static void LoadIfNeeded() {
        if (_hooksLoaded)
            return;
        _hooksLoaded = true;

        On.Celeste.Player.UseRefill += Player_UseRefill;
    }

    private static bool Player_UseRefill(On.Celeste.Player.orig_UseRefill orig, Player self, bool twoDashes) {
        var oxy = self.Scene.Tracker.GetEntity<OxygenManager>();
        if (oxy is { } && oxy.Oxygen < oxy.MaxOxygen) { // todo: special handling for two-dash refills?
            self.Dashes = 0; // force the orig function to recover dashes
        }

        var ret = orig(self, twoDashes);
        if (ret)
            oxy?.RefillOxygen(twoDashes);

        return ret;
    }

    [OnUnload]
    public static void Unload() {
        if (!_hooksLoaded)
            return;
        _hooksLoaded = false;

        On.Celeste.Player.UseRefill -= Player_UseRefill;
    }
}

[Tracked]
public class OxygenDisplay : Entity {
    float fadeTime;
    bool fading;
    public OxygenManager Manager;

    private void createTween(float fadeTime, Action<Tween> onUpdate) {
        Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeInOut, fadeTime, true);
        tween.OnUpdate = onUpdate;
        Add(tween);
    }

    public OxygenDisplay(OxygenManager oxy) {
        Tag = Tags.HUD | Tags.PauseUpdate | Tags.Persistent;

        Add(Wiggler.Create(0.5f, 4f, null, false, false));
        Manager = oxy;
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

        var overflow = false;
        var percent = (Math.Max(Manager.Oxygen, 0f) / Manager.MaxOxygen);
        if (percent > 1f) {
            percent = 1f;
            overflow = true;
        }

        // base
        Draw.Rect(Position, Engine.Width / 4f, 40f, Color.Gray);
        // fill
        Draw.Rect(Position, Engine.Width / 4f * percent, 40f, overflow ? Color.Pink : Color.Green);
        // outline
        Draw.HollowRect(Position + Vector2.UnitY * 2f, Engine.Width / 4f, 40f, Color.White);

        if (Manager.SuffocationTimer < 1f) {
            Draw.Rect(0, 0, 1920 + 16, 1080 + 16, Color.Black * (Ease.CubeIn(1f - Manager.SuffocationTimer) * .9f));
        }
    }

    public static Vector2 OffscreenPos => new Vector2(Engine.Width / 2f - (Engine.Width / 8f), -81f);
    public static Vector2 OnscreenPos => new Vector2(Engine.Width / 2f - (Engine.Width / 8f), 40f);
}