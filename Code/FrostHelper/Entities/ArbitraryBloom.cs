﻿using System.Runtime.CompilerServices;

namespace FrostHelper;

[CustomEntity("FrostHelper/ArbitraryBloom")]
internal sealed class ArbitraryBloomEntity : Entity {
    public ArbitraryBloom Bloom { get; }

    public ArbitraryBloomEntity(EntityData data, Vector2 offset) : base(data.Position + offset) {
        Bloom = new(data.Float("alpha", 1f), ArbitraryShapeEntityHelper.GetFillFromNodes(data, offset));
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

    public bool Visible;

    public ArbitraryBloom(float alpha, Vector3[] fill) {
        Alpha = alpha;
        Fill = fill;

        Visible = true;
    }
}

[Tracked]
internal sealed class ArbitraryBloomRenderer : Entity {
    private List<ArbitraryBloom> Blooms = new();
    private VertexPositionColor[] verts;

    public ArbitraryBloomRenderer() {
        Add(new CustomBloom(RenderBloom));
        verts = new VertexPositionColor[128];

        Tag = (Tags.Global | Tags.TransitionUpdate);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void NextVertex(ref int index, Vector3 pos, float alpha) {
        if (index >= verts.Length) {
            Array.Resize(ref verts, verts.Length + 128);
        }

        verts[index].Color.A = (byte) (alpha * 255f);
        verts[index].Position = pos;
        index++;
    }

    public void Add(ArbitraryBloom bloom) => Blooms.Add(bloom);
    public void Remove(ArbitraryBloom bloom) => Blooms.Remove(bloom);

    public void RenderBloom() {
        int index = 0;
        // todo: cache?
        foreach (var bloom in Blooms) {
            var alpha = bloom.Alpha;
            if (bloom.Visible && bloom.Alpha > 0) {
                foreach (var vert in bloom.Fill) {
                    NextVertex(ref index, vert, alpha);
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

        GFX.DrawVertices(cam, verts, index, null, BlendState.AlphaBlend);

        GaussianBlur.Blur(target, tempTarget, GameplayBuffers.TempA, 0f, false, GaussianBlur.Samples.Nine, 1f, GaussianBlur.Direction.Both, 1f);

        // reset stuff back to what it was previously
        Engine.Instance.GraphicsDevice.SetRenderTarget(GameplayBuffers.TempA);
        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, cam);
    }
}