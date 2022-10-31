namespace FrostHelper;

/// <summary>
/// Wire Lamps from A Christmas Night
/// </summary>
[CustomEntity("FrostHelper/WireLamps")]
public class WireLamps : Entity {
    internal MTexture? LampTexture;
    internal Sprite[]? Sprites;

    public float Wobbliness;

    private bool BeginNodeBroken;
    private float BeginNodeYSpeed;
    private bool EndNodeBroken;
    private float EndNodeYSpeed;

    // whether .Update was called
    private bool _updated;

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
        var attached = data.Bool("attached", false);

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

        if (attached) {
            var moveBegin = (Vector2 pos) => {
                Curve.Begin += pos;
            };
            var moveEnd = (Vector2 pos) => {
                Curve.End += pos;
            };

            StaticMover s1 = null!;
            Add(s1 = new StaticMover() {
                SolidChecker = (s) => s.CollideRect(Utils.CreateRect(Curve.Begin.X, Curve.Begin.Y, 1f, 1f)),
                OnMove = moveBegin,
                OnShake = moveBegin,
                OnDestroy = () => {
                    BeginNodeBroken = true;
                    s1.Platform.Collidable = false;

                    if (!_updated)
                        Raycast(ref Curve.Begin, ref BeginNodeYSpeed, ref BeginNodeBroken);
                },
                OnDisable = () => { }
            });

            StaticMover s2 = null!;
            Add(s2 = new StaticMover() {
                SolidChecker = (s) => s.CollideRect(Utils.CreateRect(Curve.End.X, Curve.End.Y, 1f, 1f)),
                OnMove = moveEnd,
                OnShake = moveEnd,
                OnDestroy = () => {
                    EndNodeBroken = true;
                    s2.Platform.Collidable = false;

                    if (!_updated)
                        Raycast(ref Curve.End, ref EndNodeYSpeed, ref EndNodeBroken);
                },
                OnDisable = () => { }
            });
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

    public override void Update() {
        _updated = true;
        base.Update();

        var bounds = (Scene as Level)!.Bounds;

        if (BeginNodeBroken) {
            HandleNodeMove(ref Curve.Begin, ref BeginNodeYSpeed, ref BeginNodeBroken, bounds);
        }
        if (EndNodeBroken) {
            HandleNodeMove(ref Curve.End, ref EndNodeYSpeed, ref EndNodeBroken, bounds);
        }
    }

    /// <summary>
    /// Returns whether a collision occured.
    /// </summary>
    /// <returns></returns>
    private bool HandleNodeMove(ref Vector2 pos, ref float speed, ref bool stateRet, Rectangle bounds) {
        var s = (Scene as Level)!;

        speed = Calc.Approach(speed, 160f, 500f * Engine.DeltaTime);

        if (s.CollideFirst<Solid>(pos) is { } solid) {
            if (solid is SolidTiles) {
                stateRet = false;
            }
            return true;
        } else {
            pos.Y += speed * Engine.DeltaTime;
        }

        if (pos.Y > bounds.Bottom + 16) {
            stateRet = false;
            return true;
        }

        return false;
    }

    private void Raycast(ref Vector2 pos, ref float speed, ref bool ret) {
        var bounds = (Scene as Level)!.Bounds;
        while (!HandleNodeMove(ref pos, ref speed, ref ret, bounds)) {}
    }

    public override void Render() {
        var level = SceneAs<Level>();

        var controlOffset = new Vector2(
            (float) Math.Sin(sineX + level.WindSineTimer * 2f),
            (float) Math.Sin(sineY + level.WindSineTimer * 2.8f)
        ) * 8f;

        Curve.Control = (Curve.Begin + Curve.End) / 2f + new Vector2(0f, 24f) + (controlOffset * Wobbliness);

        if (!CameraCullHelper.IsVisible(level.Camera.Position, Curve))
            return;

        var start = Curve.Begin;

        const int segments = 16;

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
