namespace FrostHelper;

public class Rainbowifier : Component {
    public Rainbowifier() : base(false, true) { }

    public override void Render() {
        ColorHelper.SetGetHueScene(Scene);
        foreach (var component in Entity.Components) {
            if (component is Image img) {
                img.Color = ColorHelper.GetHue(img.RenderPosition);
            }
        }
    }
}
