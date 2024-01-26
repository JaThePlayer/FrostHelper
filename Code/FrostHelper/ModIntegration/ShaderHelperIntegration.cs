//#define USE_SHADER_HELPER

using FrostHelper.Helpers;

namespace FrostHelper.ModIntegration;

/// <summary>
/// Acts like a reference to an effect. Safe between hot reloads.
/// </summary>
internal sealed class EffectRef {
    private Effect? effect;
    private readonly string key;

    public Effect Get() {
        if (effect is { IsDisposed: true }) {
            effect = null;
        }

        effect ??= ShaderHelperIntegration.GetEffect(key);

        return effect;
    }

    internal EffectRef(string key) {
        this.key = key;
    }

    internal EffectRef(Effect eff) {
        effect = eff;
    }

    private static EffectRef? _colorGrade;
    // provides an effect ref for alternative color grade - same as vanilla, but doesn't share global state with the vanilla colorgrades
    internal static EffectRef AltColorGrade => _colorGrade ??= new(GFX.FxColorGrading.Clone());
}

public static class ShaderHelperIntegration {
    public class MissingShaderException : Exception {
        private string id;
        public MissingShaderException(string id) : base() {
            this.id = id;
        }

        public override string Message => $"Shader not found: {id}";
    }

    public class NotLoadedException : Exception {
        public NotLoadedException() : base() {
        }

        public override string Message => "Shader Helper is not installed, but Frost Helper tried getting an effect!\nAdd ShaderHelper as a dependency in your everest.yaml!";
    }

    [OnLoad]
    public static void Load() {
        Everest.Content.OnUpdate += Content_OnUpdate;
    }

    [OnUnload]
    public static void Unload() {
        Everest.Content.OnUpdate -= Content_OnUpdate;
    }

    private static void Content_OnUpdate(ModAsset from, ModAsset to) {
        if (to.Format is "cso" or ".cso") {
            try {
                AssetReloadHelper.Do("Reloading Shader", () => {
                    var effectName = to.PathVirtual.Substring("Effects/".Length, to.PathVirtual.Length - ".cso".Length - "Effects/".Length);

                    if (FallbackEffectDict.TryGetValue(effectName, out var effect)) {
                        FallbackEffectDict.Remove(effectName);
                        if (!effect.IsDisposed)
                            effect.Dispose();
                    }

                    Logger.Log(LogLevel.Info, "FrostHelper.ShaderHelper", $"Reloaded {effectName}");
                }, () => {
                    FrostModule.TryGetCurrentLevel()?.Reload();
                });

            } catch (Exception e) {
                // there's a catch-all filter on Content.OnUpdate that completely ignores the exception,
                // would nice to actually see it though
                Logger.LogDetailed(e);
            }

        }
    }
#if USE_SHADER_HELPER
    [OnLoadContent]
    public static void TryFindShaderHelper() {
        EverestModuleMetadata shaderHelperMeta = new EverestModuleMetadata { Name = "ShaderHelper", VersionString = "0.0.3" };
        if (IntegrationUtils.TryGetModule(shaderHelperMeta, out EverestModule shaderHelperModule)) {
            module = shaderHelperModule;
            module_FX = shaderHelperModule.GetType().GetField("FX");
            ShaderHelperLoaded = true;
        }
    }


    private static bool _loaded;
    public static bool ShaderHelperLoaded {
        set => _loaded = value;
        get {
            if (!_loaded)
                TryFindShaderHelper();
            return _loaded;
        }
    }

    private static FieldInfo module_FX;
    private static EverestModule module;
#endif

    /// <summary>
    /// Used for caching if Shader Helper is not installed
    /// </summary>
    private static Dictionary<string, Effect> FallbackEffectDict = new();

    private static Dictionary<string, Effect> GetShaderHelperEffects() =>
#if USE_SHADER_HELPER
        ShaderHelperLoaded ? (module_FX.GetValue(module) as Dictionary<string, Effect>)! : FallbackEffectDict;
#else
        FallbackEffectDict;
#endif

    // exposed via the API
    public static Effect? TryGetEffect(string id) {
        id = id.Replace('\\', '/');

        if (GetShaderHelperEffects().TryGetValue(id, out var eff)) {
            return eff;
        }

        // try to load the effect if Shader Helper isn't installed or it didn't find it
        if (Everest.Content.TryGet($"Effects/{id}.cso", out var effectAsset, true)) {
            try {
                Effect effect = new Effect(Engine.Graphics.GraphicsDevice, effectAsset.Data);
                GetShaderHelperEffects().Add(id, effect);
                return effect;
            } catch (Exception ex) {
                Logger.Log(LogLevel.Error, "FrostHelper", "Failed to load the shader " + id);
                Logger.Log(LogLevel.Error, "FrostHelper", "Exception: \n" + ex.ToString());
            }
        }

        return null;
    }
    
    public static Effect GetEffect(string id) {
        if (TryGetEffect(id) is { } s)
            return s;

        NotifyAboutMissingShader(id);
        return GFX.FxTexture;
    }

    internal static EffectRef GetEffectRef(string id)
        => new(id);

    internal static void NotifyAboutMissingShader(string id) {
        NotificationHelper.Notify($"Shader not found: {id}");
    }

    public static Effect BeginGameplayRenderWithEffect(string id, bool endBatch) {
        Effect effect = GetEffect(id);
        var cam = GameplayRenderer.instance.Camera;

        ApplyStandardParameters(effect, cam);

        if (endBatch) {
            Draw.SpriteBatch.End();
        }

        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, effect, cam.Matrix);

        return effect;
    }

    public static void DrawWithEffect(string id, Action drawFunc, bool endBatch) {
        /*
        Engine.Instance.GraphicsDevice.SetRenderTarget(tempA);
        BeginGameplayRenderWithEffect(id, endBatch);

        drawFunc();

        Draw.SpriteBatch.End();
        if (endBatch)
        {
            GameplayRenderer.Begin();
        }*/
        Camera c = FrostModule.GetCurrentLevel().Camera;
        VirtualRenderTarget tempA = GameplayBuffers.TempA;
        GameplayRenderer.End();
        Engine.Instance.GraphicsDevice.SetRenderTarget(tempA);
        int s = GameplayBuffers.Gameplay.Width / 320;
        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, c.Matrix * Matrix.CreateScale(s));

        Engine.Instance.GraphicsDevice.Clear(Color.Transparent);

        drawFunc();

        GameplayRenderer.End();

        Effect eff = GetEffect(id);
        ApplyStandardParameters(eff);

        Engine.Instance.GraphicsDevice.SetRenderTarget(GameplayBuffers.Gameplay);

        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, eff, Matrix.Identity);


        Draw.SpriteBatch.Draw(tempA, Vector2.Zero, Color.White);

        GameplayRenderer.End();
        GameplayRenderer.Begin();
    }

    public static Effect ApplyStandardParameters(this Effect effect, Camera? camera = null)
        => ApplyStandardParameters(effect, camera?.Matrix);

    // exposed via the API
    public static Effect ApplyStandardParameters(this Effect effect, Matrix? camera) {
        var level = FrostModule.GetCurrentLevel() ?? throw new Exception("Not in a level when applying shader parameters! How did you...");
        var parameters = effect.Parameters;

        parameters["DeltaTime"]?.SetValue(Engine.DeltaTime);
        parameters["Time"]?.SetValue(Engine.Scene.TimeActive);
        parameters["Dimensions"]?.SetValue(new Vector2(320, 180) * HDlesteCompat.Scale);
        parameters["CamPos"]?.SetValue(level.Camera.Position);
        parameters["ColdCoreMode"]?.SetValue(level.CoreMode == Session.CoreModes.Cold);

        Viewport viewport = Engine.Graphics.GraphicsDevice.Viewport;

        Matrix projection = Matrix.CreateOrthographicOffCenter(0, viewport.Width, viewport.Height, 0, 0, 1);
        // from communal helper
        Matrix halfPixelOffset = FrostModule.Framework is FrameworkType.FNA
            ? Matrix.Identity
            : Matrix.CreateTranslation(-0.5f, -0.5f, 0f);

        parameters["TransformMatrix"]?.SetValue(halfPixelOffset * projection);

        parameters["ViewMatrix"]?.SetValue(camera ?? Matrix.Identity);
        parameters["Photosensitive"]?.SetValue(Settings.Instance.DisableFlashes);

        return effect;
    }

    public static void ApplyParametersFrom(this Effect shader, Dictionary<string, string> parameters) {
        foreach (var item in parameters) {
            var prop = shader.Parameters[item.Key];
            if (prop is null) {
                throw new Exception($"Shader doesn't have a {item.Key} property!");
            }


            switch (prop.ParameterType) {
                case EffectParameterType.Bool:
                    prop.SetValue(Convert.ToBoolean(item.Value));
                    break;
                case EffectParameterType.Single:
                    prop.SetValue(item.Value.ToSingle());
                    break;
                default:
                    throw new Exception($"Entity Batcher doesn't know how to set a parameter of type {prop.ParameterType} for property {item.Key}");
            }
        }
    }

    public static void ApplyParametersFrom(this Effect shader, Dictionary<string, object> parameters, bool throwOnMissingProp = true) {
        foreach (var item in parameters) {
            var prop = shader.Parameters[item.Key];
            if (prop is null) {
                if (throwOnMissingProp)
                    throw new Exception($"Shader doesn't have a {item.Key} property!");
                else
                    continue;
            }


            switch (prop.ParameterType) {
                case EffectParameterType.Bool:
                    prop.SetValue(Convert.ToBoolean(item.Value));
                    break;
                case EffectParameterType.Single:
                    switch (item.Value) {
                        case string str:
                            prop.SetValue(str.ToSingle());
                            break;
                        case float f:
                            prop.SetValue(f);
                            break;
                    }

                    break;
                default:
                    throw new Exception($"Entity Batcher doesn't know how to set a parameter of type {prop.ParameterType} for property {item.Key}");
            }
        }
    }
}
