using FrostHelper.ModIntegration;

namespace FrostHelper.ShaderImplementations;

public static class FunkyBloomShaderImpl {
    public static void Apply(RenderTarget2D colorMap, RenderTarget2D shatterMap, RenderTarget2D target, string effectName) {
        var eff = ShaderHelperIntegration.GetEffect(effectName);
        ShaderHelperIntegration.ApplyStandardParameters(eff);

        // apply the shader
        Draw.SpriteBatch.GraphicsDevice.SetRenderTarget(GameplayBuffers.TempB);

        Draw.SpriteBatch.GraphicsDevice.Textures[1] = shatterMap;
        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, eff);

        Draw.SpriteBatch.Draw(colorMap, Vector2.Zero, Color.White);

        Draw.SpriteBatch.End();


        // apply result to target
        Draw.SpriteBatch.GraphicsDevice.SetRenderTarget(target);
        Draw.SpriteBatch.GraphicsDevice.Textures[1] = null;
        Draw.SpriteBatch.Begin();

        Draw.SpriteBatch.Draw(GameplayBuffers.TempB, Vector2.Zero, Color.White);

        Draw.SpriteBatch.End();
    }
}


[CustomEntity("FrostHelper/FunkyBloomControlller")]
//[TrackedAs(typeof(ShaderController))]
public class FunkyBloomShaderController : Entity {

    public string ShaderName;

    public FunkyBloomShaderController(EntityData data, Vector2 offset) : base() {
        ShaderName = data.Attr("shaderName");

        Depth = int.MinValue;
    }

    public override void Render() {
        Apply(GameplayBuffers.Gameplay);
    }

    public void Apply(VirtualRenderTarget source) {
        ShatterMap ??= new RenderTarget2D(Draw.SpriteBatch.GraphicsDevice, GameplayBuffers.Gameplay.Width, GameplayBuffers.Gameplay.Height);

        GameplayRenderer.End();

        FunkyBloomShaderImpl.Apply(GameplayBuffers.Gameplay, ShatterMap, GameplayBuffers.Gameplay, ShaderName);

        GameplayRenderer.Begin();
    }

    public static RenderTarget2D ShatterMap;
}
