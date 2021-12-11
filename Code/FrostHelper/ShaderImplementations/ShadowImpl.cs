using Celeste.Mod.Entities;
using FrostHelper.ModIntegration;

namespace FrostHelper.ShaderImplementations;

public static class ShadowImpl {
    public static void Apply(RenderTarget2D colorMap, RenderTarget2D shatterMap, RenderTarget2D target, string effectName) {
        var eff = ShaderHelperIntegration.GetEffect(effectName);
        ShaderHelperIntegration.ApplyStandardParameters(eff);

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


[CustomEntity("FrostHelper/ShadowController")]
[Tracked(true)]
public class ShadowController : ShaderController {

    public string ShaderName;

    public ShadowController(EntityData data, Vector2 offset) : base() {
        ShaderName = data.Attr("shaderName");

        Depth = int.MinValue;
        Verts.Initialize();
        AddVertex(new Vector3(0, 0, 0), new Vector3(0, 100, 0), new(40, 50, 0), AmbientColor, AmbientColor, RegularVertColor);
        AddVertex(new Vector3(0, 0, 0), new Vector3(55, 9, 0), new(40, 50, 0), RegularVertColor);
        AddVertex(new Vector3(0, 0, 0), new Vector3(55, 9, 0), new(90, 0, 0), RegularVertColor);
    }

    public override void Apply(VirtualRenderTarget source) {
        ShatterMap ??= new RenderTarget2D(Draw.SpriteBatch.GraphicsDevice, GameplayBuffers.Gameplay.Width, GameplayBuffers.Gameplay.Height);
        var gd = Draw.SpriteBatch.GraphicsDevice;
        // draw shatter map
        gd.SetRenderTarget(ShatterMap);
        gd.Clear(AmbientColor);

        GFX.DrawVertices(Matrix.Identity, Verts, VertexCount, null, null);

        ScreenShatterShaderImpl.Apply(source, ShatterMap, source, ShaderName);
    }

    public void AddVertex(Vector2 p1, Vector2 p2, Vector2 p3, Color c1, Color c2, Color c3) => AddVertex(new Vector3(p1, .0f), new Vector3(p2, .0f), new Vector3(p3, .0f), c1, c2, c3);
    public void AddVertex(Vector2 p1, Vector2 p2, Vector2 p3, Color c1) => AddVertex(new Vector3(p1, .0f), new Vector3(p2, .0f), new Vector3(p3, .0f), c1, c1, c1);
    public void AddVertex(Vector3 p1, Vector3 p2, Vector3 p3, Color c1) => AddVertex(p1, p2, p3, c1, c1, c1);
    public void AddVertex(Vector3 p1, Vector3 p2, Vector3 p3, Color c1, Color c2, Color c3) {
        Verts[VertexCount] = new(p1, c1);
        VertexCount++;

        Verts[VertexCount] = new(p2, c2);
        VertexCount++;

        Verts[VertexCount] = new(p3, c3);
        VertexCount++;
    }

    public static RasterizerState OutlineRasterizer = new() {
        FillMode = FillMode.WireFrame,
        CullMode = CullMode.None,
    };


    public static Color AmbientColor = new(0.3f,0.3f,0.3f,0.3f);
    public static Color RegularVertColor = new(190, 190, 190, 255);

    public int VertexCount = 0;
    public VertexPositionColor[] Verts = new VertexPositionColor[3 * 64];

    public static RenderTarget2D ShatterMap;

    public static void DrawVertices<T>(Matrix matrix, T[] vertices, int vertexCount, Effect effect = null, BlendState blendState = null, RasterizerState rasterizerState = null) where T : struct, IVertexType {
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

[CustomEntity("FrostHelper/ShadowTri")]
public class ShadowTri : Entity {
    Vector2[] Nodes;
    Color c1,c2,c3;


    public ShadowTri(EntityData data, Vector2 offset) : base(data.Position + offset) {
        Nodes = data.NodesOffset(data.Position + offset);
        c1 = data.GetColor("c1", "ffffff");
        c2 = data.GetColor("c2", "ffffff");
        c3 = data.GetColor("c3", "ffffff");
    }

    public override void Awake(Scene scene) {
        base.Awake(scene);

        Scene.Tracker.GetEntity<ShadowController>().AddVertex(Nodes[0], Nodes[1], Nodes[2], c1, c2, c3);
    }
}
