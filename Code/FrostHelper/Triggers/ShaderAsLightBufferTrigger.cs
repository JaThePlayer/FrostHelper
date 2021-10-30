#if SHADER_AS_LIGHT_BUFFER_TESTING
// there's some funky stuff that could be achieved with this
using Celeste;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod.Cil;
using System;

namespace FrostHelper.Triggers {
    public class ShaderAsLightBufferTrigger {

        [OnLoad]
        public static void Load() {
            IL.Celeste.LightingRenderer.BeforeRender += LightingRenderer_BeforeRender;
        }


        private static void LightingRenderer_BeforeRender(ILContext il) {
            var cursor = new ILCursor(il);

            //if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<LightingRenderer>("StartDrawingPrimitives"))) {
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<GraphicsDevice>("Clear"))) {
                cursor.EmitDelegate<Action>(() => {
                    Effect eff = ModIntegration.ShaderHelperIntegration.GetEffect("trippy");//"trippy" turbulenceFog
                    eff.Parameters["Time"].SetValue(Engine.Scene.TimeActive);

                    var c = FrostModule.GetCurrentLevel().Camera;

                    Engine.Graphics.GraphicsDevice.SetRenderTarget(TempLightBuffer);
                    Engine.Graphics.GraphicsDevice.Clear(Color.White);

                    Engine.Graphics.GraphicsDevice.SetRenderTarget(GameplayBuffers.LightBuffer);

                    /*
                    Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, eff);

                    Draw.SpriteBatch.Draw(TempLightBuffer, new Vector2(0f), Color.White);
                    Draw.SpriteBatch.End();

                    Engine.Graphics.GraphicsDevice.Textures[0] = GameplayBuffers.LightBuffer;

                    Engine.Graphics.GraphicsDevice.SetRenderTarget(GameplayBuffers.Light);*/

                    Engine.Graphics.GraphicsDevice.SetRenderTarget(GameplayBuffers.LightBuffer);
                    //Engine.Graphics.GraphicsDevice.Clear(Color.White);

                    Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, eff);

                    Draw.SpriteBatch.Draw(TempLightBuffer, new Vector2(0f), Color.White);
                    Draw.SpriteBatch.End();

                    Engine.Graphics.GraphicsDevice.SetRenderTarget(GameplayBuffers.Light);
                });
            }
        }

        [OnUnload]
        public static void Unload() {
            IL.Celeste.LightingRenderer.BeforeRender -= LightingRenderer_BeforeRender;
            TempLightBuffer?.Dispose();
        }


        private static VirtualRenderTarget Create(int width, int height) {
            VirtualRenderTarget virtualRenderTarget = VirtualContent.CreateRenderTarget("fh.gameplay-buffer-TempLightBuffer", width, height, false, true, 0);
            return virtualRenderTarget;
        }

        private static VirtualRenderTarget TempLightBuffer = Create(1024, 1024);
    }
}
#endif