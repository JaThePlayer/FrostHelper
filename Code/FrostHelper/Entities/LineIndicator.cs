namespace FrostHelper;

[CustomEntity("FrostHelper/LineIndicator")]
public class LineIndicator : Entity {
    public List<Vector2> Nodes;
    public Color Color;

    private float wobbleEase;
    private float wobbleFrom;
    private float wobbleTo;

    public LineIndicator(EntityData data, Vector2 offset) : base(data.Position + offset) {
        Nodes = data.NodesOffset(-data.Position).ToList();
        Nodes.Insert(0, Vector2.Zero);
        Color = data.GetColor("color", "ffffff");
    }

    public override void Update() {
        base.Update();

        wobbleEase += Engine.DeltaTime * 2f;
        if (wobbleEase > 1f) {
            wobbleEase = 0f;
            wobbleFrom = wobbleTo;
            wobbleTo = Calc.Random.NextFloat(6.28318548f);
        }
    }

    public override void Render() {
        base.Render();

        for (int i = 1; i < Nodes.Count; i++) {
            Draw.Rect(Nodes[i] + Position - Vector2.UnitY, 3f, 3f, Color);
            //Draw.Line(Nodes[i - 1] + Position, Nodes[i] + Position, Color);
            //Vector2 angle = Calc.AngleToVector(Calc.Angle(Nodes[i - 1], Nodes[i]), 1f);
            WobbleLine(Nodes[i - 1] + Position, Nodes[i] + Position, 0f);
        }
    }

    private float LineAmplitude(float seed, float index) {
        return (float) (Math.Sin(seed + index / 16f + Math.Sin(seed * 2f + index / 32f) * 6.2831854820251465) + 1.0) * 1.5f;
    }

    private float Lerp(float a, float b, float percent) {
        return a + (b - a) * percent;
    }

    private void WobbleLine(Vector2 from, Vector2 to, float offset) {
        float num = Vector2.Distance(from, to);//(to - from).Length();
        Vector2 normalizedDist = Vector2.Normalize(to - from);
        Vector2 perpendicularDist = new Vector2(normalizedDist.Y, -normalizedDist.X);
        Color color = Color;

        float scaleFactor = 0f;
        int pixelsPerLine = 16;
        int startOffset = -1;
        while (startOffset < num + 4f) {
            float newLerp = Lerp(LineAmplitude(wobbleFrom + offset, startOffset), LineAmplitude(wobbleTo + offset, startOffset), wobbleEase);
            if (startOffset + pixelsPerLine >= num) {
                newLerp = 0f;
            }
            float num5 = Math.Min(pixelsPerLine, num - 2f - startOffset);
            Vector2 p1 = from + normalizedDist * startOffset + perpendicularDist * scaleFactor;
            Vector2 p2 = from + normalizedDist * (startOffset + num5) + perpendicularDist * newLerp;
            //Draw.Line(vector3 - vector2, vector4 - vector2, color2);
            //Draw.Line(vector3 - vector2 * 2f, vector4 - vector2 * 2f, color2);
            Draw.Line(p1, p2, color);
            scaleFactor = newLerp;
            startOffset += pixelsPerLine;
        }
    }
}
