namespace FrostHelper.Entities.VanillaExtended;

[CustomEntity("FrostHelper/RainbowSwitchGate")]
internal sealed class RainbowSwitchGate(EntityData data, Vector2 offset) : SwitchGate(data, offset) {
    public override void Render() {
        if (!CameraCullHelper.IsRectangleVisible(X, Y, Width, Height))
            return;
        
        float xSlices = Collider.Width / 8.0f - 1.0f;
        float ySlices = Collider.Height / 8.0f - 1.0f;
        var basePos = Position + Shake;
        ColorHelper.SetGetHueScene(Scene);
        for (int x = 0; x <= xSlices; ++x)
        {
            for (int y = 0; y <= ySlices; ++y) {
                var pos = basePos + new Vector2(x * 8, y * 8);
                nineSlice[x < xSlices ? Math.Min(x, 1) : 2, y < ySlices ? Math.Min(y, 1) : 2]
                    .Draw(pos, default, ColorHelper.GetHue(pos));
            }
                
        }
        
        icon.Position = iconOffset + Shake;
        icon.DrawOutline();
        icon.Render();
    }
}