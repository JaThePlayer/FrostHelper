namespace FrostHelper;

[CustomEntity("FrostHelper/ColoredLightbeam")]
public sealed class ColoredLightbeam : LightBeam {
    public float ParallaxAmount;

    public ColoredLightbeam(EntityData data, Vector2 offset) : base(data, offset) {
        color = ColorHelper.GetColor(data.Attr("color", "ccffff"));
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
