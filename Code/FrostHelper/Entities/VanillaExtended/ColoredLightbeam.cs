namespace FrostHelper;

[CustomEntity("FrostHelper/ColoredLightbeam")]
public class ColoredLightbeam : LightBeam {
    private static FieldInfo LightBeam_color = typeof(LightBeam).GetField("color", BindingFlags.NonPublic | BindingFlags.Instance);

    public float ParallaxAmount;

    public ColoredLightbeam(EntityData data, Vector2 offset) : base(data, offset) {
        LightBeam_color.SetValue(this, ColorHelper.GetColor(data.Attr("color", "ccffff")));
        ParallaxAmount = data.Float("parallaxAmount", 0f);
    }

    public override void Render() {
        Vector2 oldPosition = Position;
        if (ParallaxAmount != 0f) {
            Vector2 camera = (Scene as Level)!.Camera.Position + new Vector2(160f, 90f);
            Position += (Position - camera) * ParallaxAmount;
        }
        base.Render();
        Position = oldPosition;
    }
}
