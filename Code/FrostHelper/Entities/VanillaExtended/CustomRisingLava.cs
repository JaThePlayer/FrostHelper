namespace FrostHelper;

[CustomEntity("FrostHelper/CustomRisingLava")]
[Tracked]
public sealed class CustomRisingLava : Entity {
    #region Hooks
    private static bool _hooksLoaded;
    private static ILHook? _onStay, _onLeave;
    

    [HookPreload]
    public static void LoadIfNeeded() {
        if (_hooksLoaded)
            return;
        _hooksLoaded = !_hooksLoaded;

        _onStay = EasierILHook.CreatePrefixHook(typeof(LavaBlockerTrigger), nameof(LavaBlockerTrigger.OnStay), OnLavaBlockerTriggerStay);
        _onLeave = EasierILHook.CreatePrefixHook(typeof(LavaBlockerTrigger), nameof(LavaBlockerTrigger.OnLeave), OnLavaBlockerTriggerLeave);
    }

    private static void OnLavaBlockerTriggerStay(LavaBlockerTrigger self) {
        if (self is not { enabled: true, Scene: { } scene })
            return;
        
        foreach (CustomRisingLava lava in scene.Tracker.SafeGetEntities<CustomRisingLava>())
            lava.waiting = true;
    }
    
    private static void OnLavaBlockerTriggerLeave(LavaBlockerTrigger self) {
        if (self is not { enabled: true, Scene: { } scene })
            return;
        
        foreach (CustomRisingLava lava in scene.Tracker.SafeGetEntities<CustomRisingLava>())
            lava.waiting = false;
    }
    
    [OnUnload]
    public static void Unload() {
        if (!_hooksLoaded)
            return;
        _hooksLoaded = !_hooksLoaded;
        
        _onStay?.Dispose();
        _onStay = null;
        _onLeave?.Dispose();
        _onLeave = null;

        //On.Celeste.Mod.Entities.LavaBlockerTrigger.OnStay -= LavaBlockerTrigger_OnStay;
        //On.Celeste.Mod.Entities.LavaBlockerTrigger.OnLeave -= LavaBlockerTrigger_OnLeave;
    }

    /*
    private static void LavaBlockerTrigger_OnLeave(On.Celeste.Mod.Entities.LavaBlockerTrigger.orig_OnLeave orig, Celeste.Mod.Entities.LavaBlockerTrigger self, Player player) {
        if (self.enabled && self.Scene is { } scene)
            foreach (CustomRisingLava lava in scene.Tracker.SafeGetEntities<CustomRisingLava>())
                if (lava != null)
                    lava.waiting = false;

        orig(self, player);
    }

    private static void LavaBlockerTrigger_OnStay(On.Celeste.Mod.Entities.LavaBlockerTrigger.orig_OnStay orig, Celeste.Mod.Entities.LavaBlockerTrigger self, Player player) {
        if (self.enabled && self.Scene is { } scene)
            foreach (CustomRisingLava lava in scene.Tracker.SafeGetEntities<CustomRisingLava>())
                if (lava != null)
                    lava.waiting = true;

        orig(self, player);
    }*/

    #endregion
    public bool ReverseCoreMode;
    public bool DoRubberbanding;

    public CustomRisingLava(bool intro, float speed, Color[] coldColors, Color[] hotColors, bool reverseCoreMode) {
        LoadIfNeeded();

        delay = 0f;
        this.intro = intro;
        Speed = speed;
        Depth = -1000000;
        Collider = new Hitbox(340f, 120f, 0f, 0f);
        Visible = false;
        Add(new PlayerCollider(OnPlayer, null, null));
        Add(new CoreModeListener(OnChangeMode));
        Add(loopSfx = new SoundSource());
        Add(bottomRect = new LavaRect(400f, 200f, 4));
        bottomRect.Position = new Vector2(-40f, 0f);
        bottomRect.OnlyMode = LavaRect.OnlyModes.OnlyTop;
        bottomRect.SmallWaveAmplitude = 2f;
        Cold = coldColors;
        Hot = hotColors;
        ReverseCoreMode = reverseCoreMode;
    }

    public static Color[] GetColors(EntityData data, bool cold) {
        Color[] colors = new Color[3];
        for (int i = 0; i < colors.Length; i++) {
            string path = (cold ? "cold" : "hot") + "Color" + (i + 1);
            colors[i] = ColorHelper.GetColor(data.Attr(path));
        }
        return colors;
    }

    public CustomRisingLava(EntityData data, Vector2 offset) : this(data.Bool("intro", false), data.Float("speed", -30f), GetColors(data, true), GetColors(data, false), data.Bool("reverseCoreMode", false)) {
        DoRubberbanding = data.Bool("doRubberbanding", true);
    }

    public override void Added(Scene scene) {
        base.Added(scene);
        X = SceneAs<Level>().Bounds.Left - 10;
        Y = SceneAs<Level>().Bounds.Bottom + 16;
        iceMode = SceneAs<Level>().Session.CoreMode == Session.CoreModes.Cold;
        if (ReverseCoreMode)
            iceMode = !iceMode;
        loopSfx.Play("event:/game/09_core/rising_threat", "room_state", iceMode ? 1 : 0);
        loopSfx.Position = new Vector2(Width / 2f, 0f);
        lerp = iceMode ? 1 : 0;
        UpdateColors();
    }

    public override void Awake(Scene scene) {
        base.Awake(scene);
        Player player = Scene.Tracker.GetEntity<Player>();

        CustomRisingLavaStartHeightTrigger trigger;
        if ((trigger = player.CollideFirst<CustomRisingLavaStartHeightTrigger>()) != null) {
            Y = trigger.Node.Y;
        }

        if (intro) {
            waiting = true;
            Visible = true;
            UpdateColors();
        } else if (player != null && player.JustRespawned) {
            waiting = true;
        }
    }

    private void OnChangeMode(Session.CoreModes mode) {
        iceMode = mode == Session.CoreModes.Cold;
        if (ReverseCoreMode)
            iceMode = !iceMode;
        loopSfx.Param("room_state", iceMode ? 1 : 0);
    }

    private void OnPlayer(Player player) {
        if (SaveData.Instance.Assists.Invincible) {
            if (delay <= 0f) {
                float from = Y;
                float to = Y + 48f;
                player.Speed.Y = -200f;
                player.RefillDash();
                Tween.Set(this, Tween.TweenMode.Oneshot, 0.4f, Ease.CubeOut, t => Y = MathHelper.Lerp(from, to, t.Eased), null);
                delay = 0.5f;
                loopSfx.Param("rising", 0f);
                Audio.Play("event:/game/general/assist_screenbottom", player.Position);
            }
        } else {
            player.Die(-Vector2.UnitY, false, true);
        }
    }

    public override void Update() {
        delay -= Engine.DeltaTime;
        X = SceneAs<Level>().Camera.X;
        Player entity = Scene.Tracker.GetEntity<Player>();
        base.Update();
        Visible = true;
        if (waiting) {
            loopSfx.Param("rising", 0f);

            if (!intro && entity != null && entity.JustRespawned) {
                Y = Calc.Approach(Y, entity.Y + 32f, 32f * Engine.DeltaTime);
            }

            if ((!iceMode || !intro) && (entity == null || !entity.JustRespawned)) {
                waiting = false;
            }
        } else {
            float ySpeed = 1f;
            if (DoRubberbanding) {
                float yOffset = SceneAs<Level>().Camera.Bottom - 12f;
                if (Top > yOffset + 96f) {
                    Top = yOffset + 96f;
                }
                if (Top > yOffset) {
                    ySpeed = Calc.ClampedMap(Top - yOffset, 0f, 96f, 1f, 2f);
                } else {
                    ySpeed = Calc.ClampedMap(yOffset - Top, 0f, 32f, 1f, 0.5f);
                }
            }

            if (delay <= 0f) {
                loopSfx.Param("rising", 1f);
                Y += Speed * ySpeed * Engine.DeltaTime;
            }
        }
        UpdateColors();
    }

    private void UpdateColors() {
        lerp = Calc.Approach(lerp, iceMode ? 1 : 0, Engine.DeltaTime * 4f);
        bottomRect.SurfaceColor = Color.Lerp(Hot[0], Cold[0], lerp);
        bottomRect.EdgeColor = Color.Lerp(Hot[1], Cold[1], lerp);
        bottomRect.CenterColor = Color.Lerp(Hot[2], Cold[2], lerp);
        bottomRect.Spikey = lerp * 5f;
        bottomRect.UpdateMultiplier = (1f - lerp) * 2f;
        bottomRect.Fade = iceMode ? 128 : 32;
    }

    Color[] Hot;

    Color[] Cold;

    private float Speed = -30f;

    private bool intro;

    private bool iceMode;

    public bool waiting;

    private float lerp;

    private LavaRect bottomRect;

    private float delay;

    private SoundSource loopSfx;
}
