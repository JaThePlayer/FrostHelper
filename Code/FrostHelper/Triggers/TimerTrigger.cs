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
        set => SetTimeLeft(value, updateCounters: true);
    }

    internal void SetTimeLeft(float time, bool updateCounters) {
        _timeLeft = time;
        if (updateCounters) {
            UpdateTimeCounter();
        }
    }
    
    internal bool Started;
    protected Vector2 DrawPos;

    internal readonly string TimerId;

    private readonly CounterAccessor? _outputCounter;
    private readonly CounterAccessor.CounterTimeUnits _outputCounterUnit;

    private float? _capturedTimeLeft;

    internal readonly bool SavePb;
    private bool _pbExistedBeforeStart;

    private const float PbTextScale = 0.6f;

    private bool ShouldDisplayPb(out float pb) {
        pb = 0f;
        if (SavePb && FrostModule.SaveData.GetTimerBestInCurrentMap(TimerId) is { } actualPb) {
            pb = actualPb;
            return true;
        }

        return false;
    }
    
    protected virtual string GetText() => TimeSpan.FromSeconds(_capturedTimeLeft ?? TimeLeft).ShortGameplayFormat();
    
    protected BaseTimerEntity(EntityData data, Vector2 offset) : base(data, offset)
    {
        Tag |= Tags.HUD;
        TimerId = data.Attr("timerId", "");
        Visible = data.Bool("visible", true);
        TextColor = data.GetColor("textColor", "ffffff");
        IconColor = data.GetColor("iconColor", "ffffff");
        SavePb = data.Bool("savePb");
        
        var outputCounterName = data.Attr("outputCounter");
        if (!string.IsNullOrWhiteSpace(outputCounterName)) {
            _outputCounter = new CounterAccessor(outputCounterName);
            _outputCounterUnit = data.Enum("outputCounterUnit", CounterAccessor.CounterTimeUnits.Milliseconds);
        }
        
        if (data.Attr("icon", "frostHelper/time") is { } iconPath && !string.IsNullOrWhiteSpace(iconPath))
            Icon = GFX.Game[iconPath];
        
        ResetDrawPos();
    }

    protected void ResetDrawPos() {
        DrawPos = new(Engine.Width / 2f, 0f);
        _capturedTimeLeft = null;
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
            _capturedTimeLeft = null;
            Alpha = 1f;
            Components.RemoveAll<Tween>();
            ResetDrawPos();
            
            var timers = Scene.Tracker.SafeGetEntities<BaseTimerEntity>()
                .OfType<BaseTimerEntity>()
                .Where(t => t.Visible)
                .ToList();

            var textHeight = TimerRenderHelper.Measure(GetText()).Y;
            DrawPos.Y = textHeight + (timers.Count != 0 ? timers.Max(t => t.DrawPos.Y + (t.ShouldDisplayPb(out _) ? textHeight * PbTextScale : 0f)) : 0f);
            
            _pbExistedBeforeStart = ShouldDisplayPb(out _);
        }
    }

    protected void RemoveIfNeeded() {
        if (!Started)
            return;

        _capturedTimeLeft = TimeLeft;
        var startAlpha = Alpha;
        var tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.Linear, duration: 1f, start: true);
        tween.OnComplete = (t) => ResetDrawPos();
        tween.OnUpdate = (t) => Alpha = float.Max(0f, startAlpha - t.Eased);
        Add(tween);
    }

    internal virtual bool Reset(bool resetCounters, bool savePb) => false;

    internal virtual void SavePbToFile() { }

    public override void Render() {
        base.Render();

        if (!Started && _capturedTimeLeft is null)
            return;

        var pos = DrawPos;
        var timeText = GetText();
        var textSize = TimerRenderHelper.Measure(timeText);
        TimerRenderHelper.DrawTime(pos, timeText, TextColor, alpha: Alpha);

        if (Icon is { } icon) {
            var iconPos = new Vector2(pos.X - (TimerRenderHelper.GetTimeWidth(timeText) / 2), pos.Y - textSize.Y / 2f);

            icon.DrawJustified(iconPos, new(1f, 0.5f), IconColor * Alpha);
        }

        if (_pbExistedBeforeStart && ShouldDisplayPb(out var pb)) {
            var pbDialog = Dialog.Clean("FH_PB");
            var pbTextOffset = TimerRenderHelper.GetTimeWidth($"{pbDialog}: ") * Vector2.UnitX / 2 * PbTextScale;
            TimerRenderHelper.DrawTime(pos + pbTextOffset + textSize.YComp() * PbTextScale, $"{pbDialog}: {TimeSpan.FromSeconds(pb).ShortGameplayFormat()}", TextColor, PbTextScale, alpha: Alpha);
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
    private readonly string _flag;
    private readonly bool _oneUse;
    private readonly float _startTime;

    public TimerEntity(EntityData data, Vector2 offset) : base(data, offset)
    {
        _flag = data.Attr("flag", "");
        TimeLeft = _startTime = data.Float("time", 1f);
        _oneUse = data.Bool("once", true);
    }
    
    public override void Added(Scene scene) {
        base.Added(scene);

        SceneAs<Level>()?.Session.SetFlag(_flag, false);
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
                SceneAs<Level>()?.Session.SetFlag(_flag, true);
                RemoveIfNeeded();
            }
        }
    }

    internal override bool Reset(bool resetCounters, bool savePb) {
        if (!Started)
            return false;
        if (savePb)
            SavePbToFile();

        RemoveIfNeeded();
        Started = false;
        SetTimeLeft(_startTime, resetCounters);
        return true;
    }
    
    internal override void SavePbToFile() {
        if (!SavePb)
            return;

        if (FrostModule.SaveData.GetTimerBestInCurrentMap(TimerId) is not { } prevPb || TimeLeft > prevPb)
            FrostModule.SaveData.SetTimerBestInCurrentMap(TimerId, TimeLeft);
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

        if (RemoveFlag == "")
            RemoveFlag = StopFlag;
    }

    public override void Added(Scene scene) {
        base.Added(scene);
        UpdateTimeCounter();
    }

    public override void Update() {
        base.Update();

        if (!Started || _removed)
            return;
        
        if (!SceneAs<Level>().Session.GetFlag(StopFlag))
            TimeLeft += Engine.DeltaTime;

        if (SceneAs<Level>().Session.GetFlag(RemoveFlag)) {
            SavePbToFile();
            
            _removed = true;
            RemoveIfNeeded();
        }
    }

    internal override void SavePbToFile() {
        if (!SavePb)
            return;

        if (FrostModule.SaveData.GetTimerBestInCurrentMap(TimerId) is not { } prevPb || TimeLeft < prevPb)
            FrostModule.SaveData.SetTimerBestInCurrentMap(TimerId, TimeLeft);
    }

    internal override bool Reset(bool resetCounters, bool savePb) {
        if (!Started)
            return false;
        
        if (savePb)
            SavePbToFile();

        RemoveIfNeeded();
        Started = false;
        SetTimeLeft(0f, resetCounters);
        return true;
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

            switch (valueObj)
            {
                case int i:
                    _lastValueStr = i.ToString(CultureInfo.InvariantCulture);
                    break;
                case float value:
                    value = float.Round(value, 4);
                    _lastValueStr = float.IsInteger(value) 
                        ? ((int)value).ToString(CultureInfo.InvariantCulture)
                        : value.ToString(CultureInfo.InvariantCulture);
                    break;
                default:
                    _lastValueStr = valueObj.ToString()!;
                    break;
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

[CustomEntity("FrostHelper/TimerReset")]
[Tracked]
internal sealed class TimerReset(EntityData data, Vector2 offset) : Trigger(data, offset) {
    private readonly string _timerId = data.Attr("timerId", "");
    private readonly bool _oneUse = data.Bool("oneUse", false);
    private readonly bool _resetCounters = data.Bool("resetCounters", false);
    private readonly bool _savePb = data.Bool("savePb", false);
    
    public override void OnEnter(Player player) {
        base.OnEnter(player);

        var anyReset = false;
        foreach (BaseTimerEntity timer in Scene.Tracker.SafeGetEntities<BaseTimerEntity>())
            if (timer.TimerId == _timerId)
                anyReset |= timer.Reset(_resetCounters, _savePb);

        if (anyReset && _oneUse)
            RemoveSelf();
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
            if (c == '.')
                currentScale = scale * 0.7f;
            currentWidth += (((c == ':' || c == '.') ? SpacerWidth : NumberWidth) + 4f) * currentScale;
        }
        return currentWidth;
    }
}