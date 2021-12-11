namespace FrostHelper.ShaderImplementations;

[Tracked(true)]
public abstract class ShaderController : Entity {

    public abstract void Apply(VirtualRenderTarget source);
}
