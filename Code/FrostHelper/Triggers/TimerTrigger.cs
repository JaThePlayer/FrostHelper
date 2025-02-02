using FrostHelper.Helpers;
using System.Globalization;

namespace FrostHelper.Triggers;

[Tracked(true)]
internal abstract class BaseTimerEntity : Trigger {
    public Color TextColor, IconColor;
    protected readonly MTexture? Icon;
    
    protected float Alpha = 1f;

    private float _timeLeft;

    internal float TimeLeft {
        get => _timeLeft;
        set {
            _timeLeft = value;
            UpdateTimeCounter();
        }
    }
    internal bool Started;
    protected Vector2 DrawPos;

    internal readonly string TimerId;

    private readonly CounterAccessor? _outputCounter;
    private readonly CounterAccessor.CounterTimeUnits _outputCounterUnit;
    
    protected virtual string GetText() => TimeSpan.FromSeconds(TimeLeft).ShortGameplayFormat();
    
    protected BaseTimerEntity(EntityData data, Vector2 offset) : base(data, offset)
    {
        Tag |= Tags.HUD;
        TimerId = data.Attr("timerId", "");
        Visible = data.Bool("visible", true);
        TextColor = data.GetColor("textColor", "ffffff");
        IconColor = data.GetColor("iconColor", "ffffff");
        
        var outputCounterName = data.Attr("outputCounter");
        if (!string.IsNullOrWhiteSpace(outputCounterName)) {
            _outputCounter = new CounterAccessor(outputCounterName);
            _outputCounterUnit = data.Enum("outputCounterUnit", CounterAccessor.CounterTimeUnits.Milliseconds);
        }
        
        if (data.Attr("icon", "frostHelper/time") is { } iconPath && !string.IsNullOrWhiteSpace(iconPath))
            Icon = GFX.Game[iconPath];
        
        DrawPos = new(Engine.Width / 2f, 0f);
    }

    public override void OnEnter(Player player) {
        base.OnEnter(player);
        StartIfNeeded();
    }

    protected void StartIfNeeded() {
        if (Started)
            return;

        Started = true;
        if (Visible) {
            var timers = Scene.Tracker.GetEntities<BaseTimerEntity>()
                .OfType<BaseTimerEntity>()
                .Where(t => t.Visible)
                .ToList();
            
            DrawPos.Y = TimerRenderHelper.Measure(GetText()).Y + (timers.Any() ? timers.Max(t => t.DrawPos.Y) : 0f);
        }
    }

    protected void RemoveIfNeeded() {
        if (!Started)
            return;

        var startAlpha = Alpha;
        var tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.Linear, duration: 1f, start: true);
        tween.OnComplete = (t) => RemoveSelf();
        tween.OnUpdate = (t) => Alpha = float.Max(0f, startAlpha - t.Eased);
        Add(tween);
    }
    
    public override void Render() {
        base.Render();

        if (!Started)
            return;

        var pos = DrawPos;
        var timeText = GetText();
        TimerRenderHelper.DrawTime(pos, timeText, TextColor, alpha: Alpha);

        if (Icon is { } icon) {
            var iconPos = new Vector2(pos.X - (TimerRenderHelper.GetTimeWidth(timeText) / 2), pos.Y - TimerRenderHelper.Measure(timeText).Y / 2f);

            icon.DrawJustified(iconPos, new(1f, 0.5f), IconColor * Alpha);
        }
    }

    protected void UpdateTimeCounter() {
        if (_outputCounter is { } counter && Scene is Level level) {
            counter.SetTime(level.Session, TimeSpan.FromSeconds(TimeLeft), _outputCounterUnit);
        }
    }
}

[CustomEntity("FrostHelper/Timer")]
[Tracked]
internal sealed class TimerEntity : BaseTimerEntity {
    internal readonly string Flag;

    public TimerEntity(EntityData data, Vector2 offset) : base(data, offset)
    {
        Flag = data.Attr("flag", "");
        TimeLeft = data.Float("time", 1f);
    }
    
    public override void Added(Scene scene) {
        base.Added(scene);

        SceneAs<Level>()?.Session.SetFlag(Flag, false);
        UpdateTimeCounter();
    }
    
    public override void Update() {
        base.Update();

        if (!Started)
            return;

        if (TimeLeft > 0) {
            TimeLeft -= Engine.DeltaTime;

            if (TimeLeft <= 0) {
                TimeLeft = 0;
                SceneAs<Level>()?.Session.SetFlag(Flag, true);
                RemoveIfNeeded();
            }
        }
    }
}

[CustomEntity("FrostHelper/IncrementingTimer")]
[Tracked]
internal sealed class IncrementingTimerEntity : BaseTimerEntity {
    internal readonly string RemoveFlag;
    internal readonly string StopFlag;
    private bool _removed;
    
    public IncrementingTimerEntity(EntityData data, Vector2 offset) : base(data, offset)
    {
        StopFlag = data.Attr("stopFlag", "");
        RemoveFlag = data.Attr("removeFlag", "");

        if (RemoveFlag == "") {
            RemoveFlag = StopFlag;
        }
    }

    public override void Added(Scene scene) {
        base.Added(scene);
        UpdateTimeCounter();
    }

    public override void Update() {
        base.Update();

        if (!Started || _removed)
            return;
        
        if (!SceneAs<Level>().Session.GetFlag(StopFlag)) {
            TimeLeft += Engine.DeltaTime;
        }

        if (SceneAs<Level>().Session.GetFlag(RemoveFlag)) {
            _removed = true;
            RemoveIfNeeded();
        }
    }
}


[CustomEntity("FrostHelper/CounterDisplay")]
[Tracked]
internal sealed class CounterDisplayEntity : BaseTimerEntity {
    internal readonly ConditionHelper.Condition RemoveFlag;
    internal readonly ConditionHelper.Condition VisibleFlag;
    
    private bool _removed;

    private bool _showOnRoomLoad;
    
    private readonly CounterExpression _counter;
    
    private object _lastValue = 0;
    private string _lastValueStr = "0";

    protected override string GetText() {
        if (Scene is Level level) {
            var valueObj = _counter.GetObject(level.Session);
            if (valueObj == _lastValue)
                return _lastValueStr;

            _lastValue = valueObj;

            if (valueObj is int i)
                valueObj = (float)i;
            
            if (valueObj is float value)
            {
                value = float.Round(value, 4);
                _lastValueStr = float.IsInteger(value) 
                    ? ((int)value).ToString(CultureInfo.InvariantCulture)
                    : value.ToString(CultureInfo.InvariantCulture);
            } else {
                _lastValueStr = valueObj.ToString()!;
            }
            

            return _lastValueStr;
        }

        return "";
    }

    public CounterDisplayEntity(EntityData data, Vector2 offset) : base(data, offset) {
        _counter = new(data.Attr("counter"));
        RemoveFlag = data.GetCondition("removeFlag", "");
        VisibleFlag = data.GetCondition("visibleFlag", "");

        _showOnRoomLoad = data.Bool("showOnRoomLoad", false);
    }

    private float GetTargetAlpha() {
        return VisibleFlag.Empty || VisibleFlag.Check() ? 1f : 0f;
    }

    public override void Added(Scene scene) {
        base.Added(scene);
        if (_showOnRoomLoad)
            StartIfNeeded();

        Alpha = GetTargetAlpha();
    }

    public override void Update() {
        base.Update();

        if (!Started || _removed)
            return;

        var session = SceneAs<Level>().Session;
        if (!RemoveFlag.Empty && RemoveFlag.Check(session)) {
            _removed = true;
            RemoveIfNeeded();
        } else {
            Alpha = Calc.Approach(Alpha, GetTargetAlpha(), Engine.DeltaTime);
        }
    }
}

static class TimerRenderHelper {
    static bool SizesCalculated = false;
    static float SpacerWidth;
    static float NumberWidth;

    private static void CalculateBaseSizes() {
        if (SizesCalculated)
            return;
        SizesCalculated = true;

        // compute the max size of a digit and separators in the English font, for the timer part.
        PixelFont font = Dialog.Languages["english"].Font;
        float fontFaceSize = Dialog.Languages["english"].FontFaceSize;
        PixelFontSize pixelFontSize = font.Get(fontFaceSize);
        for (int i = 0; i < 10; i++) {
            float digitWidth = pixelFontSize.Measure(i.ToString()).X;
            if (digitWidth > NumberWidth) {
                NumberWidth = digitWidth;
            }
        }
        SpacerWidth = pixelFontSize.Measure('.').X;
    }

    public static Vector2 Measure(string text) {
        var lang = Dialog.Languages["english"];
        var pixelFontSize = lang.Font.Get(lang.FontFaceSize);

        return pixelFontSize.Measure(text);
    }

    public static void DrawTime(Vector2 position, string timeString, Color color, float scale = 1f, float alpha = 1f) {
        CalculateBaseSizes();

        position -= GetTimeWidth(timeString) * Vector2.UnitX / 2;
        var lang = Dialog.Languages["english"];

        PixelFont font = lang.Font;
        float fontFaceSize = lang.FontFaceSize;
        float currentScale = scale;
        float currentX = position.X;
        float currentY = position.Y;
        color *= alpha;
        Color colorDoubleAlpha = color * alpha;
        Color outlineColor = Color.Black * alpha;

        foreach (char c in timeString) {
            if (c == '.') {
                currentScale = scale * 0.7f;
                currentY -= 5f * scale;
            }

            Color currentColor, currentOutlineColor;
            if (c == ':' || c == '.' || currentScale < scale) {
                currentColor = colorDoubleAlpha;
                currentOutlineColor = outlineColor * alpha;
            } else {
                currentColor = color;
                currentOutlineColor = outlineColor;
            }

            float advance = (((c == ':' || c == '.') ? SpacerWidth : NumberWidth) + 4f) * currentScale;
            font.DrawOutline(fontFaceSize, c.ToString(), new Vector2(currentX + advance / 2, currentY), new Vector2(0.5f, 1f), Vector2.One * currentScale, currentColor, 2f, outlineColor);
            currentX += advance;
        }
    }

    public static float GetTimeWidth(string timeString, float scale = 1f) {
        float currentScale = scale;
        float currentWidth = 0f;
        foreach (char c in timeString) {
            if (c == '.') {
                currentScale = scale * 0.7f;
            }
            currentWidth += (((c == ':' || c == '.') ? SpacerWidth : NumberWidth) + 4f) * currentScale;
        }
        return currentWidth;
    }
}