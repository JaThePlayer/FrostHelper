using Celeste.Mod.Entities;
using FrostHelper.ModIntegration;

namespace FrostHelper.ShaderImplementations;

public static class BlurMaskImpl {
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


[CustomEntity("FrostHelper/BlurMaskControlller")]
[Tracked(true)]
//[TrackedAs(typeof(ShaderController))]
public class BlurMaskController : ShaderController {

    /*
     * 1. Draw blured gameplay to TempA
     * 2. Draw target entities to TempB
     * 3. Use shader: if TempB pixel != transparent => return TempA pixel
     * */

    public string ShaderName;
    public Dictionary<string, string> ShaderParameters;

    public BlurMaskController(EntityData data, Vector2 offset) : base() {
        ShaderName = data.Attr("shaderName");
        ShaderParameters = data.GetDictionary("parameters");
        Depth = int.MinValue;

        //Types = FrostModule.GetTypes(data.Attr("types")); ;
    }

    public override void Apply(VirtualRenderTarget source) {
        ShatterMap ??= new RenderTarget2D(Draw.SpriteBatch.GraphicsDevice, GameplayBuffers.Gameplay.Width, GameplayBuffers.Gameplay.Height);

        // draw mask
        Engine.Graphics.GraphicsDevice.SetRenderTarget(GameplayBuffers.TempB);
        Engine.Graphics.GraphicsDevice.Clear(Color.Transparent);
        GameplayRenderer.Begin();

        //foreach (var item in AffectedEntities) {
        //    item?.Render();
        //}

        Draw.SpriteBatch.End();

        var shader = ShaderHelperIntegration.GetEffect(ShaderName);
        shader.ApplyParametersFrom(ShaderParameters);

        BetterShaderTrigger.SimpleApply(GameplayBuffers.TempB, GameplayBuffers.Gameplay, shader);
        Engine.Graphics.GraphicsDevice.SetRenderTarget(GameplayBuffers.Gameplay);

        BlurMaskImpl.Apply(source, ShatterMap, source, ShaderName);
    }




    public static RenderTarget2D ShatterMap;
}
