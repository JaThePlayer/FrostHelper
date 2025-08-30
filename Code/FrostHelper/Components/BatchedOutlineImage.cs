using System.Runtime.InteropServices;

namespace FrostHelper.Components;

[Tracked]
internal sealed class BatchedOutlineImage(Image image, Color color) : Component(false, false) {
    private BatchedOutlineRenderer? _renderer;

    internal Image Image => image;
    
    public override void EntityAwake() {
        base.EntityAwake();

        //Console.WriteLine("BatchedOutlineImage: awake");
        _renderer = ControllerHelper<BatchedOutlineRenderer>.AddToSceneIfNeeded(Scene,
            r => r.Depth == Entity.Depth && r.Color == color,
            () => new BatchedOutlineRenderer(Entity.Depth, color));

        _renderer.Images.Add(this);
    }

    public override void EntityRemoved(Scene scene) {
        base.EntityRemoved(scene);

        _renderer?.Images.Remove(this);
        _renderer = null;
    }

    public override void Removed(Entity entity) {
        base.Removed(entity);
        
        _renderer?.Images.Remove(this);
        _renderer = null;
    }
}

internal sealed class BatchedOutlineRenderer : Entity {
    private VirtualRenderTargetRef? _targetRef;
    
    public Color Color;

    internal readonly List<BatchedOutlineImage> Images = [];
    
    public BatchedOutlineRenderer(int depth, Color color) {
        Depth = depth;
        Color = color;
        
        Add(new BeforeRenderHook(BeforeRender));
    }

    private void BeforeRender() {
        _targetRef ??= RenderTargetHelper.RentFullScreenBufferRef();
        var target = _targetRef.Target;
        var b = Draw.SpriteBatch;
        var gd = b.GraphicsDevice;
        
        gd.SetRenderTarget(target);
        Engine.Instance.GraphicsDevice.Clear(Color.Transparent);

        GameplayRenderer.Begin();
        foreach (var img in CollectionsMarshal.AsSpan(Images)) {
            img.Image.Render();
        }
        GameplayRenderer.End();
        
        gd.SetRenderTarget(null);
    }

    public override void Render() {
        base.Render();

        if (_targetRef is not { Disposed: false, Target: { } target })
            return;
        
        var batch = Draw.SpriteBatch;
        var cam = SceneAs<Level>().Camera;
        
        var renderPos = cam.Position.Floor();
        var finalColor = Color;

        batch.Draw(target, renderPos - Vector2.UnitY, null, finalColor);
        batch.Draw(target, renderPos + Vector2.UnitY, null, finalColor);
        batch.Draw(target, renderPos - Vector2.UnitX, null, finalColor);
        batch.Draw(target, renderPos + Vector2.UnitX, null, finalColor);
        
        batch.Draw(target, renderPos, null, Color.White);
    }
}