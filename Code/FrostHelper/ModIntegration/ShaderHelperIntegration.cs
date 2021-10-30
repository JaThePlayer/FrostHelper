using Celeste;
using Celeste.Mod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace FrostHelper.ModIntegration {
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
    }
}
