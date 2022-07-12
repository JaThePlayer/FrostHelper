namespace FrostHelper;

public abstract class IndicatorEntity : Entity {
    public Image Image;
    public Color Color;
    public Color OutlineColor;

    public IndicatorEntity(EntityData data, Vector2 offset) : base(data.Position + offset) {
        Image = new Image(GFX.Game[data.Attr("spritePath")]).CenterOrigin();

        Color = data.GetColor("color", "ffffff");
        OutlineColor = data.GetColor("outlineColor", "000000");

        Image.Color = Color;

        Add(Image);
    }

    public override void Render() {
        if (OutlineColor != Color.Transparent) {
            Image.DrawOutline(OutlineColor);
        }

        base.Render();
    }
}

[CustomEntity("FrostHelper/PufferIndicator")]
public class PufferIndicator : IndicatorEntity {
    public PufferIndicator(EntityData data, Vector2 offset) : base(data, offset) {
    }

    public override void Update() {
        base.Update();

        var collisionRect = Utils.CreateRect(Image.X + X, Image.Y + Y, Image.Width, Image.Height);

        // unfortunately vanilla puffers are not tracked
        foreach (var e in Scene.Entities) {
            if (typeof(Puffer).IsAssignableFrom(e.GetType()) && Collide.CheckRect(e, collisionRect)) {
                RemoveSelf();
                return;
            }
        }
    }
}
