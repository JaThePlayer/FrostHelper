namespace FrostHelper.Entities;

[CustomEntity("FrostHelper/Timer")]
[Tracked]
public class TimerEntity : Trigger {
    private readonly string Flag;
    private readonly float Time;

    private float TimeLeft;
    private bool Started;

    private float Alpha = 1f;

    private MTexture? Icon;

    public Vector2 DrawPos;

    public Color TextColor, IconColor;

    public static Vector2 OnscreenPos => new Vector2(Engine.Width / 2f, 0f);

    public TimerEntity(EntityData data, Vector2 offset) : base(data, offset) {
        Flag = data.Attr("flag", "");
        Time = data.Float("time", 1f);

        TimeLeft = Time;

        Tag |= Tags.HUD;

        Visible = true;

        if (data.Attr("icon", "frostHelper/time") is { } iconPath && !string.IsNullOrWhiteSpace(iconPath))
            Icon = GFX.Game[iconPath];

        TextColor = data.GetColor("textColor", "ffffff");
        IconColor = data.GetColor("iconColor", "ffffff");

        DrawPos = OnscreenPos;
    }

    public override void Added(Scene scene) {
        base.Added(scene);

        SceneAs<Level>()?.Session.SetFlag(Flag, false);
    }

    public override void OnEnter(Player player) {
        base.OnEnter(player);

        if (!Started) {
            Started = true;
            DrawPos.Y = Scene.Tracker.GetEntities<TimerEntity>().Max(t => ((TimerEntity)t).DrawPos.Y) + TimerRenderHelper.Measure(GetText()).Y;
        }

    }

    public override void Update() {
        base.Update();

        if (!Started)
            return;

        if (TimeLeft > 0) {
            TimeLeft -= Engine.DeltaTime;

            if (TimeLeft <= 0) {
                SceneAs<Level>()?.Session.SetFlag(Flag, true);

                var tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.Linear, duration: 1f, start: true);
                tween.OnComplete = (t) => RemoveSelf();
                tween.OnUpdate = (t) => Alpha = 1f - t.Eased;

                Add(tween);
            }
        }
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

    private string GetText() => TimeSpan.FromSeconds(TimeLeft).ShortGameplayFormat();
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

        PixelFont font = Dialog.Languages["english"].Font;
        float fontFaceSize = Dialog.Languages["english"].FontFaceSize;
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