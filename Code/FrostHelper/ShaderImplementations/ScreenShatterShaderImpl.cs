using Celeste.Mod.Entities;
using FrostHelper.ModIntegration;

namespace FrostHelper.ShaderImplementations;

public static class ScreenShatterShaderImpl {
    public static void Apply(RenderTarget2D colorMap, RenderTarget2D shatterMap, RenderTarget2D target, string effectName) {
        var eff = ShaderHelperIntegration.GetEffect(effectName);
        ShaderHelperIntegration.ApplyStandardParameters(eff, camera: null);

        // apply the shader
        Draw.SpriteBatch.GraphicsDevice.SetRenderTarget(GameplayBuffers.TempB);
        Draw.SpriteBatch.GraphicsDevice.Clear(Color.Transparent);

        Draw.SpriteBatch.GraphicsDevice.Textures[1] = shatterMap;
        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, eff);

        Draw.SpriteBatch.Draw(colorMap, Vector2.Zero, Color.White);

        Draw.SpriteBatch.End();


        // apply result to target
        Draw.SpriteBatch.GraphicsDevice.SetRenderTarget(target);
        Draw.SpriteBatch.GraphicsDevice.Textures[1] = null;
        Draw.SpriteBatch.Begin();
        Draw.SpriteBatch.GraphicsDevice.Clear(Color.Transparent);

        Draw.SpriteBatch.Draw(GameplayBuffers.TempB, Vector2.Zero, Color.White);

        Draw.SpriteBatch.End();
    }
}


[CustomEntity("FrostHelper/ScreenShatterControlller")]
[Tracked(true)]
//[TrackedAs(typeof(ShaderController))]
public class ScreenShatterShaderController : ShaderController {

    public string ShaderName;

    public ScreenShatterShaderController(EntityData data, Vector2 offset) : base() {
        ShaderName = data.Attr("shaderName");

        Depth = int.MinValue;
        ShatterVerts.Initialize();
        Verts.Initialize();
        AddVertex(new(0, 0, 0), new(0, 100, 0), new(40, 50, 0), new(3, 5), RegularVertColor);
        AddVertex(new(0, 0, 0), new(55, 9, 0),  new(40, 50, 0), new(7, 3), RegularVertColor);
        AddVertex(new(0, 0, 0), new(55, 9, 0),  new(90, 0, 0),  new(3, 2), RegularVertColor);
    }

    public override void Apply(VirtualRenderTarget source) {
        ShatterMap ??= new RenderTarget2D(Draw.SpriteBatch.GraphicsDevice, GameplayBuffers.Gameplay.Width, GameplayBuffers.Gameplay.Height);

        // draw shatter map
        Draw.SpriteBatch.GraphicsDevice.SetRenderTarget(ShatterMap);
        GFX.DrawVertices(Matrix.Identity, ShatterVerts, VertexCount, null, null);

        ScreenShatterShaderImpl.Apply(source, ShatterMap, source, ShaderName);

        var prevRasterizer = Engine.Graphics.GraphicsDevice.RasterizerState;
        DrawVertices(Matrix.Identity, Verts, VertexCount, null, null, OutlineRasterizer);
        Engine.Graphics.GraphicsDevice.RasterizerState = prevRasterizer;
    }

    public void AddVertex(Vector3 p1, Vector3 p2, Vector3 p3, Point shatterOffset, Color color) {
        Color shatterColor = new(shatterOffset.X, shatterOffset.Y, 0);

        ShatterVerts[VertexCount] = new(p1, shatterColor);
        Verts[VertexCount] = new(p1, color);
        VertexCount++;

        ShatterVerts[VertexCount] = new(p2, shatterColor);
        Verts[VertexCount] = new(p2, color);
        VertexCount++;

        ShatterVerts[VertexCount] = new(p3, shatterColor);
        Verts[VertexCount] = new(p3, color);
        VertexCount++;
    }

    public static RasterizerState OutlineRasterizer = new() {
        FillMode = FillMode.WireFrame,
        CullMode = CullMode.None,
    };


    public static Color RegularVertColor = new(190, 190, 190, 255);

    public int VertexCount = 0;
    public VertexPositionColor[] ShatterVerts = new VertexPositionColor[3 * 64];
    public VertexPositionColor[] Verts = new VertexPositionColor[3 * 64];

    public static RenderTarget2D ShatterMap;

    public static void DrawVertices<T>(Matrix matrix, T[] vertices, int vertexCount, Effect? effect = null, BlendState? blendState = null, RasterizerState? rasterizerState = null) where T : struct, IVertexType {
        effect ??= GFX.FxPrimitive;
        blendState ??= BlendState.AlphaBlend;
        rasterizerState ??= RasterizerState.CullNone;

        Vector2 vector = new Vector2(Engine.Graphics.GraphicsDevice.Viewport.Width, Engine.Graphics.GraphicsDevice.Viewport.Height);
        matrix *= Matrix.CreateScale(1f / vector.X * 2f, -(1f / vector.Y) * 2f, 1f);
        matrix *= Matrix.CreateTranslation(-1f, 1f, 0f);
        Engine.Instance.GraphicsDevice.RasterizerState = rasterizerState;
        Engine.Instance.GraphicsDevice.BlendState = blendState;
        effect.Parameters["World"].SetValue(matrix);
        foreach (EffectPass effectPass in effect.CurrentTechnique.Passes) {
            effectPass.Apply();
            Engine.Instance.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, vertexCount / 3);
        }
    }
}
