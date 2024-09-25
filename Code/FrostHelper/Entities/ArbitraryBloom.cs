using FrostHelper.Helpers;
using System.Runtime.CompilerServices;

namespace FrostHelper;

[CustomEntity("FrostHelper/ArbitraryBloom")]
internal sealed class ArbitraryBloomEntity : Entity {
    public ArbitraryBloom Bloom { get; }

    public ArbitraryBloomEntity(EntityData data, Vector2 offset) : base(data.Position + offset) {
        Bloom = new(data.Float("alpha", 1f), ArbitraryShapeEntityHelper.GetFillFromNodes(data, -(data.Position + offset)), () => Position);
    }

    public override void Added(Scene scene) {
        base.Added(scene);

        var controller = ControllerHelper<ArbitraryBloomRenderer>.AddToSceneIfNeeded(scene);

        controller.Add(Bloom);
    }

    public override void Removed(Scene scene) {
        base.Removed(scene);

        var controller = ControllerHelper<ArbitraryBloomRenderer>.AddToSceneIfNeeded(scene);

        controller.Remove(Bloom);
    }
}

internal sealed class ArbitraryBloom {
    public readonly Vector3[] Fill;
    public readonly float Alpha;
    internal readonly Func<Vector2> PosGetter;

    public readonly Rectangle Bounds;

    public bool Visible;


    public Vector2 ParentPos => PosGetter();

    public ArbitraryBloom(float alpha, Vector3[] fill, Func<Vector2> positionGetter) {
        Alpha = alpha;
        Fill = fill;
        PosGetter = positionGetter;
        Bounds = RectangleExt.FromPointsFromXY(fill);
        Visible = true;
    }
}

[Tracked]
internal sealed class ArbitraryBloomRenderer : Entity {
    private readonly List<ArbitraryBloom> _blooms = [];
    private VertexPositionColor[] _verts;

    public ArbitraryBloomRenderer() {
        Add(new CustomBloom(RenderBloom));
        _verts = new VertexPositionColor[128];

        Tag = (Tags.Global | Tags.TransitionUpdate);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void NextVertex(ref int index, Vector3 pos, float alpha) {
        if (index >= _verts.Length) {
            Array.Resize(ref _verts, _verts.Length + 128);
        }

        _verts[index].Color.A = (byte) (alpha * 255f);
        _verts[index].Position = pos;
        index++;
    }

    public void Add(ArbitraryBloom bloom) => _blooms.Add(bloom);
    public void Remove(ArbitraryBloom bloom) => _blooms.Remove(bloom);

    public void RenderBloom() {
        int index = 0;
        // todo: cache?
        foreach (var bloom in _blooms) {
            var alpha = bloom.Alpha;
            if (bloom is { Visible: true, Alpha: > 0, ParentPos: var bloomPos } && CameraCullHelper.IsRectangleVisible(bloom.Bounds.MovedBy(bloomPos))) {
                foreach (var vert in bloom.Fill) {
                    NextVertex(ref index, vert.AddXY(bloomPos), alpha);
                }
            }
        }

        if (index < 3) {
            return;
        }

        var cam = SceneAs<Level>().Camera.Matrix;

        Draw.SpriteBatch.End();

        var target = RenderTargetHelper<ArbitraryBloomRenderer>.Get();
        var tempTarget = RenderTargetHelper<ArbitraryBloom>.Get();
        Engine.Instance.GraphicsDevice.SetRenderTarget(tempTarget);
        Engine.Instance.GraphicsDevice.Clear(Color.Transparent);
        Engine.Instance.GraphicsDevice.SetRenderTarget(target);
        Engine.Instance.GraphicsDevice.Clear(Color.Transparent);

        GFX.DrawVertices(cam, _verts, index, null, BlendState.AlphaBlend);

        GaussianBlur.Blur(target, tempTarget, GameplayBuffers.TempA, 0f, false, GaussianBlur.Samples.Nine, 1f, GaussianBlur.Direction.Both, 1f);

        // reset stuff back to what it was previously
        Engine.Instance.GraphicsDevice.SetRenderTarget(GameplayBuffers.TempA);
        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, cam);
    }
}