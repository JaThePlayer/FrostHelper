namespace FrostHelper.ModIntegration;

public static class ShaderHelperIntegration {
    public class MissingShaderException : Exception {
        private string id;
        public MissingShaderException(string id) : base() {
            this.id = id;
        }

        public override string Message => $"Shader not found: {id}";
    }

    [OnLoadContent]
    public static void Load() {
        EverestModuleMetadata celesteTASMeta = new EverestModuleMetadata { Name = "ShaderHelper", VersionString = "0.0.3" };
        if (IntegrationUtils.TryGetModule(celesteTASMeta, out EverestModule shaderHelperModule)) {
            module = shaderHelperModule;
            module_FX = shaderHelperModule.GetType().GetField("FX");
            Loaded = true;
        }
    }

    public static bool Loaded;
    private static FieldInfo module_FX;
    private static EverestModule module;

    public static Effect GetEffect(string id) {
        if (Loaded)
            return (module_FX.GetValue(module) as Dictionary<string, Effect>)[id];

        return null;
    }

    public static Effect BeginGameplayRenderWithEffect(string id, bool endBatch) {
        if (!Loaded)
            return null;

        Effect effect = GetEffect(id);
        if (effect is null)
            throw new MissingShaderException(id);

        if (effect.Parameters["Time"] != null) {
            effect.Parameters["Time"].SetValue(Engine.Scene.TimeActive);
        }

        if (endBatch) {
            Draw.SpriteBatch.End();
        }

        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, effect, FrostModule.GetCurrentLevel().Camera.Matrix);

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
        eff.Parameters["Time"].SetValue(Engine.Scene.TimeActive);
        eff.Parameters["camPos"].SetValue(c.Position);

        Engine.Instance.GraphicsDevice.SetRenderTarget(GameplayBuffers.Gameplay);

        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, eff, Matrix.Identity);


        Draw.SpriteBatch.Draw(tempA, Vector2.Zero, Color.White);

        GameplayRenderer.End();
        GameplayRenderer.Begin();
    }

    public static void ApplyStandardParameters(Effect effect) {
        EffectParameter deltaParam = effect.Parameters["DeltaTime"];
        if (deltaParam != null)
            deltaParam.SetValue(Engine.DeltaTime);
        EffectParameter timeParam = effect.Parameters["Time"];
        if (timeParam != null)
            timeParam.SetValue(Engine.Scene.TimeActive);

        EffectParameter dimensionsParam = effect.Parameters["Dimensions"];
        if (dimensionsParam != null) {
            Vector2 value = new Vector2(Engine.Graphics.GraphicsDevice.Viewport.Width, Engine.Graphics.GraphicsDevice.Viewport.Height);
            dimensionsParam.SetValue(value);
        }
        EffectParameter camPosParam = effect.Parameters["CamPos"];
        if (camPosParam != null) {
            camPosParam.SetValue(FrostModule.GetCurrentLevel().Camera.Position);
        }

        EffectParameter coldCoreModeParam = effect.Parameters["ColdCoreMode"];
        if (coldCoreModeParam != null) {
            coldCoreModeParam.SetValue(FrostModule.GetCurrentLevel().CoreMode == Session.CoreModes.Cold);
        }
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

    public static void ApplyParametersFrom(this Effect shader, Dictionary<string, object> parameters) {
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
