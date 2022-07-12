namespace FrostTempleHelper.Entities.azcplo1k;

[CustomEntity("noperture/portalSurface")]
class uadzca : Solid {
    public string ColorStr;
    public Color Color;

    public static Dictionary<string, Color> Colors = new Dictionary<string, Color>() {
        ["Purple"] = new Color(1f, 0.3f, 1f, 1f),
        ["Blue"] = new Color(0.3f, 0.3f, 1f, 1f),
        ["Red"] = new Color(1.0f, 0.3f, 0.3f, 1.0f),
        ["Yellow"] = new Color(1.0f, 1.0f, 0.3f, 1.0f),
        ["Green"] = new Color(0.3f, 1.0f, 0.3f, 1.0f),
    };

    public uadzca(EntityData data, Vector2 offset) : base(data.Position + offset, data.Width, data.Height, true) {
        ColorStr = data.Attr("color", "Blue");
        Color = Colors[ColorStr];
    }

    public override void Render() {
        base.Render();
        Draw.Rect(Collider, Color);
    }
}
