namespace FrostHelper;

[CustomEntity("FrostHelper/ArbitraryBloom")]
public class ArbitraryBloom : Entity {
    public Vector3[] Fill;
    public float Alpha;

    public ArbitraryBloom(EntityData data, Vector2 offset) : base(data.Position + offset) {
        Fill = ArbitraryShapeEntityHelper.GetFillFromNodes(data, offset);
        Alpha = data.Float("alpha", 1f);
    }

    public override void Added(Scene scene) {
        base.Added(scene);

        var controller = ControllerHelper<ArbitraryBloomRenderer>.AddToSceneIfNeeded(scene);

        controller.Add(this);
    }

    public override void Removed(Scene scene) {
        base.Removed(scene);

        var controller = ControllerHelper<ArbitraryBloomRenderer>.AddToSceneIfNeeded(scene);

        controller.Remove(this);
    }
}

[Tracked]
public class ArbitraryBloomRenderer : Entity {
    private List<ArbitraryBloom> Blooms = new();
    private VertexPositionColor[] verts;

    public ArbitraryBloomRenderer() {
        Add(new CustomBloom(RenderBloom));
        verts = new VertexPositionColor[128];

        Tag = (Tags.Global | Tags.TransitionUpdate);
    }

    private void NextVertex(ref int index, Vector3 pos, float alpha) {
        if (index >= verts.Length) { 
            Array.Resize(ref verts, verts.Length + 128);
        }

        verts[index].Color.A = (byte)(alpha * 255f);
        verts[index].Position = pos;
        index++;
    }

    public void Add(ArbitraryBloom bloom) => Blooms.Add(bloom);
    public void Remove(ArbitraryBloom bloom) => Blooms.Remove(bloom);

    public void RenderBloom() {
        int index = 0;
        foreach (var bloom in Blooms) {
            var alpha = bloom.Alpha;
            foreach (var vert in bloom.Fill) {
                NextVertex(ref index, vert, alpha);
            }
        }
        var cam = SceneAs<Level>().Camera.Matrix;

        Draw.SpriteBatch.End();

        var target = RenderTargetHelper<ArbitraryBloomRenderer>.Get();
        var tempTarget = RenderTargetHelper<ArbitraryBloom>.Get();
        Engine.Instance.GraphicsDevice.SetRenderTarget(tempTarget);
        Engine.Instance.GraphicsDevice.Clear(Color.Transparent);
        Engine.Instance.GraphicsDevice.SetRenderTarget(target);
        Engine.Instance.GraphicsDevice.Clear(Color.Transparent);

        //Engine.Graphics.GraphicsDevice.Textures[0] = GFX.Game["util/lightbeam"].Texture.Texture;
        GFX.DrawVertices(cam, verts, index, null, BlendState.AlphaBlend);

        //Engine.Instance.GraphicsDevice.SetRenderTarget(GameplayBuffers.TempA);
        GaussianBlur.Blur(target, tempTarget, GameplayBuffers.TempA, 0f, false, GaussianBlur.Samples.Nine, 1f, GaussianBlur.Direction.Both, 1f);

        Engine.Instance.GraphicsDevice.SetRenderTarget(GameplayBuffers.TempA);

        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, cam);
    }
}