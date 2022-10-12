namespace FrostHelper;

/// <summary>
/// Wire Lamps from A Christmas Night
/// </summary>
[CustomEntity("FrostHelper/WireLamps")]
public class WireLamps : Entity {
    internal MTexture? LampTexture;
    internal Sprite[]? Sprites;

    public float Wobbliness;

    public WireLamps(EntityData data, Vector2 offset) : base(data.Position + offset) {
        Vector2 to = data.Nodes[0] + offset;
        Curve = new SimpleCurve(Position, to, Vector2.Zero);
        Depth = (data.Bool("above", false) ? -8500 : 2000) - 1;
        Random random = new Random((int) Math.Min(Position.X, to.X));
        Color[] colors = data.GetColors("colors", defaultColors);
        Color = data.GetColor("wireColor", "595866");
        sineX = random.NextFloat(4f);
        sineY = random.NextFloat(4f);

        Wobbliness = data.Float("wobbliness", 1.0f);

        lights = new VertexLight[data.Int("lightCount", 3)];
        var lightAlpha = data.Float("lightAlpha", 1f);
        var lightStartFade = data.Int("lightStartFade", 8);
        var lightEndFade = data.Int("lightEndFade", 16);

        var spritePath = data.Attr("lampSprite", "objects/FrostHelper/wireLamp");
        var animated = !GFX.Game.Has(spritePath);

        if (animated)
            Sprites = new Sprite[lights.Length];
        else
            LampTexture = GFX.Game[spritePath];

        for (int i = 0; i < lights.Length; i++) {
            lights[i] = new VertexLight(colors[random.Next(0, colors.Length)], lightAlpha, lightStartFade, lightEndFade);
            Add(lights[i]);

            if (animated) {
                var sprite = new Sprite(GFX.Game, spritePath) {
                    Color = lights[i].Color
                };
                sprite.AddLoop("i", "", data.Float("frameDelay", .5f));
                sprite.Play("i", randomizeFrame: true);
                sprite.CenterOrigin();

                Add(Sprites![i] = sprite);
            }
        }
    }

    static Color[] defaultColors = new Color[]
    {
        Color.Red,
        Color.Yellow,
        Color.Blue,
        Color.Green,
        Color.Orange
    };

    public override void Render() {
        var level = SceneAs<Level>();

        var controlOffset = new Vector2(
            (float) Math.Sin(sineX + level.WindSineTimer * 2f),
            (float) Math.Sin(sineY + level.WindSineTimer * 2.8f)
        ) * 8f;

        Curve.Control = (Curve.Begin + Curve.End) / 2f + new Vector2(0f, 24f) + (controlOffset * Wobbliness);
        var start = Curve.Begin;

        const int segments = 16;

        if (!CameraCullHelper.IsRectVisible(level.Camera.Position, Curve.Begin, Curve.End))
            return;

        for (int i = 1; i <= segments; i++) {
            float percent = i / (float) segments;

            Vector2 point = Curve.GetPoint(percent);
            Draw.Line(start, point, Color);
            start = point;
        }

        for (int i = 1; i <= lights.Length; i++) {
            float percent = i / (lights.Length + 1f);
            Vector2 point = Curve.GetPoint(percent);

            lights[i - 1].Position = point - Position;

            if (LampTexture is { }) {
                LampTexture.DrawCentered(point, getColor(i));
            } else {
                Sprites![i - 1].Position = lights[i - 1].Position;
            }
        }

        base.Render();
    }

    public VertexLight[] lights;

    public Color Color;

    public SimpleCurve Curve;

    private float sineX;

    private float sineY;

    Color getColor(int i) {
        return lights[(i - 1) % lights.Length].Color;
    }
}
