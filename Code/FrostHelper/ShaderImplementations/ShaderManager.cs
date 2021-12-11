namespace FrostHelper.ShaderImplementations;

public class ShaderManager {
    [OnLoad]
    public static void Load() {
        On.Celeste.Glitch.Apply += Apply_HOOK;
    }

    [OnUnload]
    public static void Unload() {
        On.Celeste.Glitch.Apply -= Apply_HOOK;
    }

    public static void Apply_HOOK(On.Celeste.Glitch.orig_Apply orig, VirtualRenderTarget source, float timer, float seed, float amplitude) {
        orig(source, timer, seed, amplitude);
        /*
        foreach (var trigger in FrostModule.GetCurrentLevel().Tracker.GetEntities<BetterShaderTrigger>()) {
            if (trigger is BetterShaderTrigger s && s.Activated)
                foreach (var item in (trigger as BetterShaderTrigger).Effects) {
                    Apply(source, source, ShaderHelperIntegration.GetEffect(item));

                }
            return;
        }*/

        if (FrostModule.GetCurrentLevel().Tracker.Entities.TryGetValue(typeof(ShaderController), out var entities))
            foreach (var item in entities) {
                (item as ShaderController).Apply(source);
            }
        //foreach (var item in FrostModule.GetCurrentLevel().Tracker.Entities[typeof(IShaderController)]) {

        //}


    }
}
