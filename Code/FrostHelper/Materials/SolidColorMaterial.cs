using FrostHelper.EXPERIMENTAL;

namespace FrostHelper.Materials;

internal sealed class SolidColorMaterial(Color color) : IMaterial {
    private Texture2D? _texture2D;
    
    public Texture2D GetTexture() {
        if (_texture2D is null) {
            _texture2D = new Texture2D(Engine.Graphics.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            _texture2D.SetData([ color ]);
        }

        return _texture2D;
    }

    public void Dispose() {
        var texture = _texture2D;
        _texture2D = null;
        texture?.Dispose();
    }

    public void Fill(Rectangle bounds, in RenderContext ctx) {
        var b = Draw.SpriteBatch;
        b.Draw(GetTexture(), bounds, Color.White);
    }

    public void Fill(RenderTarget2D target, in RenderContext ctx) {
        var b = Draw.SpriteBatch;

        using var finalBatch = new TemporarySpriteBatchBuilder()
            .WithTransformMatrix(Matrix.Identity)
            .WithRenderTarget(target)
            .Use();
        b.Draw(GetTexture(), new Rectangle(0, 0, target.Width, target.Height), Color.White);
    }
}

[CustomEntity("FrostHelper/Materials/SolidColor")]
internal sealed class SolidColorMaterialSource(EntityData data, Vector2 offset) : MaterialSource(data, offset) {
    private readonly Color _color = data.GetColor("color", "ffffff");
    
    public override IMaterial CreateMaterial(MaterialManager manager) {
        return new SolidColorMaterial(_color);
    }
}
