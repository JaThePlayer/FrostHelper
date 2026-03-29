using FrostHelper.Backdrops;
using FrostHelper.EXPERIMENTAL;
using FrostHelper.Helpers;

namespace FrostHelper.Materials;

internal sealed class GradientMaterial(Gradient gradient, Gradient.Directions direction, bool loopX, bool loopY, int width, int height) : IMaterial {
    private VertexPositionColor[]? _vertexes;
    private int _vertexCount;
    private RenderTargetPoolRef? _texture;
    
    public void Dispose() {
        _texture?.Dispose();
        _texture = null;
    }
    
    public void Fill(Rectangle bounds, in RenderContext ctx) {
        _texture ??= PrepareTexture();
        
        Draw.SpriteBatch.Draw(_texture.Target,
            bounds,
            RectangleExt.CreateTruncating(ctx.RenderPosition, bounds.Width, bounds.Height),
            Color.White);
    }

    public void Fill(RenderTarget2D target, in RenderContext ctx) {
        _texture ??= PrepareTexture();
        
        using var finalBatch = new TemporarySpriteBatchBuilder()
            .WithTransformMatrix(Matrix.Identity)
            .WithSamplerState(SamplerState.LinearWrap)
            .WithRenderTarget(target)
            .Use();
        Draw.SpriteBatch.Draw(_texture.Target, 
            RectangleExt.CreateTruncating(0, 0, target.Width, target.Height),
            RectangleExt.CreateTruncating(ctx.RenderPosition, target.Width, target.Height),
            Color.White);
    }
    
    private RenderTargetPoolRef PrepareTexture() {
        var gd = Engine.Graphics.GraphicsDevice;
        gradient.GetVertexes(ref _vertexes, direction, default, loopX, loopY, out _vertexCount, width, height);

        var texture = RenderTargetPool.Get(width, height);
        using var batch = TemporarySpriteBatchBuilderExt.CreateDefault()
            .WithSamplerState(SamplerState.LinearWrap)
            .WithRenderTarget(texture.Target)
            .Use();
        gd.Clear(Color.Transparent);
        gd.SetRenderTarget(texture.Target);
        GFX.DrawVertices(Matrix.Identity, _vertexes, _vertexCount);

        return texture;
    }
}

[CustomEntity("FrostHelper/Materials/Gradient")]
internal sealed class GradientMaterialSource(EntityData data, Vector2 offset) : MaterialSource(data, offset) {
    private readonly Gradient _gradient = data.Parse("gradient", new Gradient());
    private readonly Gradient.Directions _direction = data.Enum("direction", Gradient.Directions.Horizontal);
    private readonly bool _loopX = data.Bool("loopX");
    private readonly bool _loopY = data.Bool("loopY");
    private readonly int _width = data.Int("gradientWidth", 320);
    private readonly int _height = data.Int("gradientHeight", 180);
    
    public override IMaterial CreateMaterial(MaterialManager manager) {
        return new GradientMaterial(_gradient, _direction, _loopX, _loopY, _width, _height);
    }
}
