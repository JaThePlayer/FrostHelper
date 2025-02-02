namespace FrostHelper;

[CustomEntity("FrostHelper/BloomColorTrigger")]
public class BloomColorTrigger : Trigger {
    public Color Color;

    public BloomColorTrigger(EntityData data, Vector2 offset) : base(data, offset) {
        Color = data.GetColor("color", "ffffff");
    }

    public override void OnEnter(Player player) {
        base.OnEnter(player);

        API.API.SetBloomColor(Color);
    }
}

[CustomEntity("FrostHelper/BloomColorFadeTrigger")]
public class BloomColorFadeTrigger : Trigger {
    public BloomColorFadeTrigger(EntityData data, Vector2 offset) : base(data, offset) {
        From = data.GetColor("bloomAddFrom", "ffffff");
        To = data.GetColor("bloomAddTo", "ff00ff");
        PositionMode = data.Enum("positionMode", PositionModes.NoEffect);
    }

    public override void OnStay(Player player) {
        var lerped = Color.Lerp(From, To, MathHelper.Clamp(GetPositionLerp(player, PositionMode), 0f, 1f));

        API.API.SetBloomColor(lerped);
    }

    public Color From;

    public Color To;

    public PositionModes PositionMode;
}

[CustomEntity("FrostHelper/BloomColorPulseTrigger")]
public class BloomColorPulseTrigger : Trigger {
    public Color From;
    public Color To;

    public float Duration;
    public Ease.Easer Easer;
    public Tween.TweenMode TweenMode;

    public BloomColorPulseTrigger(EntityData data, Vector2 offset) : base(data, offset) {
        From = data.GetColor("bloomAddFrom", "ffffff");
        To = data.GetColor("bloomAddTo", "ff00ff");
        Duration = data.Float("duration", 0.4f);
        Easer = data.Easing("easing", Ease.Linear);
        TweenMode = data.TweenMode("tweenMode", Tween.TweenMode.YoyoOneshot);
    }

    public override void OnEnter(Player player) {
        base.OnEnter(player);

        var tween = Tween.Create(TweenMode, Easer, Duration);
        tween.OnUpdate = (t) => {
            var lerped = Color.Lerp(From, To, MathHelper.Clamp(t.Eased, 0f, 1f));

            API.API.SetBloomColor(lerped);
        };

        tween.Start();
        
        var holder = ControllerHelper<PulseHolder>.AddToSceneIfNeeded(Scene);
        holder.Add(tween);
    }

    [Tracked]
    private sealed class PulseHolder : Entity {
        public PulseHolder() {
            Tag = Tags.Global;
            Active = true;
            Visible = false;
        }
    }
}


[CustomEntity("FrostHelper/RainbowBloomTrigger")]
public class RainbowBloomTrigger : Trigger {
    public bool Enable;

    public RainbowBloomTrigger(EntityData data, Vector2 offset) : base(data, offset) {
        Enable = data.Bool("enable", true);
    }

    [OnLoad]
    public static void Load() {
        BloomColorChange.ColorManipulator += RainbowManipulator;
    }

    [OnUnload]
    public static void Unload() {
#pragma warning disable CS8601 // Possible null reference assignment... what
        BloomColorChange.ColorManipulator -= RainbowManipulator;
#pragma warning restore CS8601
    }

    private static Color RainbowManipulator(Color c) => FrostModule.Session.RainbowBloom ? ColorHelper.GetHue(Engine.Scene, new()) : c;

    public override void OnEnter(Player player) {
        base.OnEnter(player);

        FrostModule.Session.RainbowBloom = Enable;
    }
}