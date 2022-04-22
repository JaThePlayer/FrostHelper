using Celeste.Mod.Entities;
using FrostHelper.ModIntegration;

namespace FrostHelper {
    [CustomEntity("FrostHelper/EntityBatcher")]
    public class EntityBatcher : Entity {
        public Type[] Types;
        public string Shader;

        public List<Entity> AffectedEntities;

        public Dictionary<string, object> ShaderParameters;

        public bool MakeEntitiesInvisible;

        public int[]? DynamicDepthPossibleDepths = null;

        public string Flag;

        public bool FlagInverted;

        public bool IsEnabled() => Flag == string.Empty || (SceneAs<Level>().Session.GetFlag(Flag) ^ FlagInverted);

        private Scene _firstScene;

        public EntityBatcher(EntityData data, Vector2 offset) : base(data.Position + offset) {
            Flag = data.Attr("flag", string.Empty);
            FlagInverted = data.Bool("flagInverted", false);
            Shader = data.Attr("effect");
            Depth = data.Int("depth", Depths.Top);
            Types = FrostModule.GetTypes(data.Attr("types"));
            MakeEntitiesInvisible = data.Bool("makeEntitiesInvisible", true);
            Visible = true;

            string[] propertySplit = data.Attr("parameters", string.Empty).Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            ShaderParameters = new();

            foreach (var item in propertySplit) {
                int splitIndex = item.IndexOf(':');
                ShaderParameters[item.Substring(0, splitIndex)] = item.Substring(splitIndex+1);
            }

            var d = data.Attr("dynamicDepthBatchSplitField", string.Empty);
            if (d != string.Empty)
            DynamicDepthPossibleDepths = d.Split(',').Select(s => int.Parse(s)).ToArray();

            Add(new TransitionListener() { OnOut = (f) => {
                Visible = false; // make sure to not double down on shadering on transitions
            } });
        }

        public EntityBatcher(EntityBatcher from, int targetDepth) {
            Shader = from.Shader;
            ShaderParameters = from.ShaderParameters;
            Types = from.Types;
            Flag = from.Flag;
            FlagInverted = from.FlagInverted;

            Visible = true;

            Depth = targetDepth;
            DynamicDepthPossibleDepths = new int[] { targetDepth };
        }


        public override void Awake(Scene scene) {
            base.Awake(scene);
            if (_firstScene is null) {
                _firstScene = scene;
            }

            if (AffectedEntities is not null) {
                return;
            }
            AffectedEntities = new();

            if (DynamicDepthPossibleDepths != null && DynamicDepthPossibleDepths.Length > 1) {
                foreach (var depth in DynamicDepthPossibleDepths) {
                    var newBatcher = new EntityBatcher(this, depth);
                    newBatcher.Depth = depth;
                    scene.Add(newBatcher);
                }

                RemoveSelf();
            } else {
                foreach (var item in scene.Entities) {
                    if (Types.Contains(item.GetType())) {
                        AffectedEntities.Add(item);
                        if (MakeEntitiesInvisible) {
                            item.Visible = false;
                        }
                        
                    }
                }
            }
        }

        public override void Update() {
            base.Update();
            for (int i = AffectedEntities.Count - 1; i > -1; i--) {
                var item = AffectedEntities[i];
                if (item.Scene is null) {
                    //AffectedEntities.Remove(item);
                }
            }

            Visible = IsEnabled();
            if (MakeEntitiesInvisible) {
                foreach (var entity in AffectedEntities) {
                    entity.Visible = !Visible;
                }
            } 
        }

        public static GaussianBlur.Samples GetSamples(Dictionary<string, object> ShaderParameters) {
            var samples = ShaderParameters["samples"];
            GaussianBlur.Samples smpls;
            if (samples is string str) {
                Enum.TryParse(str, true, out smpls);
                ShaderParameters["samples"] = smpls;
            } else {
                smpls = (GaussianBlur.Samples) samples;
            }

            return smpls;
        }

        public static float GetFloatParam(string name, Dictionary<string, object> ShaderParameters) {
            var param = ShaderParameters[name];
            if (param is string str) {
                float p = str.ToSingle();
                ShaderParameters[name] = p;
                return p; 
            } else {
                return (float)param;
            }
        }

        private static RenderTarget2D maskTarget => GameplayBuffers.TempB;

        public static void RenderVanillaBlur(Dictionary<string, object> ShaderParameters) {
            GaussianBlur.Blur(maskTarget, temp, GameplayBuffers.Gameplay, GetFloatParam("fade", ShaderParameters), false, GetSamples(ShaderParameters), GetFloatParam("alpha", ShaderParameters));
            GameplayRenderer.Begin();
        }

        private static void DrawMask(Dictionary<string, object> ShaderParameters) {
            // step 1: draw mask
            Engine.Graphics.GraphicsDevice.SetRenderTarget(temp2);
            Engine.Graphics.GraphicsDevice.Clear(Color.Transparent);
            if (ShaderParameters.ContainsKey("renderBgStylegrounds")) {
                var lvl = FrostModule.GetCurrentLevel();
                lvl.Background.Render(lvl);
            }
            Draw.SpriteBatch.Begin();
            Draw.SpriteBatch.Draw(GameplayBuffers.Gameplay, Vector2.Zero, Color.White);
            Draw.SpriteBatch.End();

            if (ShaderParameters.ContainsKey("renderEntitiesToGameplay")) {
                RedrawEntities(ShaderParameters, true);
            }

            Engine.Graphics.GraphicsDevice.SetRenderTarget(null);
        }

        private static void RedrawEntities(Dictionary<string, object> ShaderParameters, bool force = false) {
            if (force || ShaderParameters.ContainsKey("rerenderEntities")) {
                Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, null);
                Draw.SpriteBatch.Draw(maskTarget, Vector2.Zero, Color.White);
                Draw.SpriteBatch.End();
            }
        }
        
        private static Effect GetMaskShader(Dictionary<string, object> shaderParameters) {
            var shader = ShaderHelperIntegration.GetEffect(shaderParameters["maskShader"] as string ?? throw new Exception("Mask shaders need a 'maskShader' parameter!"));
            shader.ApplyParametersFrom(shaderParameters, false);
            return shader;
        }


        public static void RenderMask(Dictionary<string, object> shaderParameters) {
            DrawMask(shaderParameters);

            // step 2: render blur
            //GaussianBlur.Blur(temp2, temp, GameplayBuffers.TempA, GetFloatParam("fade", ShaderParameters), false, GetSamples(ShaderParameters), 1f, GaussianBlur.Direction.Both, GetFloatParam("alpha", ShaderParameters));
            var shader = GetMaskShader(shaderParameters);

            // step 3: shader
            Engine.Instance.GraphicsDevice.Textures[1] = maskTarget; // t1 -> mask
            BetterShaderTrigger.SimpleApply(temp2, GameplayBuffers.Gameplay, shader);

            // step 4: redraw the entities that were used for the mask

            RedrawEntities(shaderParameters);
            GameplayRenderer.Begin();
        }

        public static void RenderBlurMask(Dictionary<string, object> shaderParameters) {
            DrawMask(shaderParameters);

            // step 2: render blur
            GaussianBlur.Blur(temp2, temp, GameplayBuffers.TempA, GetFloatParam("fade", shaderParameters), false, GetSamples(shaderParameters), 1f, GaussianBlur.Direction.Both, GetFloatParam("alpha", shaderParameters));
            var shader = GetMaskShader(shaderParameters);

            // step 3: shader
            Engine.Instance.GraphicsDevice.Textures[1] = maskTarget; // t1 -> mask
            BetterShaderTrigger.SimpleApply(GameplayBuffers.TempA, GameplayBuffers.Gameplay, shader);

            // step 4: redraw the entities that were used for the mask

            RedrawEntities(shaderParameters);
            GameplayRenderer.Begin();
        }

        public static void Apply(List<Entity> AffectedEntities, string Shader, Dictionary<string, object> ShaderParameters, int? requiredDepth) {
            Draw.SpriteBatch.End();

            if (temp is null || temp.Width != GameplayBuffers.Gameplay.Width) {
                temp?.Dispose();
                temp2?.Dispose();

                temp = VirtualContent.CreateRenderTarget("fh.entitybatcher.Temp", GameplayBuffers.Gameplay.Width, GameplayBuffers.Gameplay.Height, false, false);
                temp2 = VirtualContent.CreateRenderTarget("fh.entitybatcher.Temp2", GameplayBuffers.Gameplay.Width, GameplayBuffers.Gameplay.Height, false, false);
                //maskTarget = VirtualContent.CreateRenderTarget("fh.entitybatcher.maskTarget", GameplayBuffers.Gameplay.Width, GameplayBuffers.Gameplay.Height, false, false);
            }

            Engine.Graphics.GraphicsDevice.SetRenderTarget(maskTarget);
            Engine.Graphics.GraphicsDevice.Clear(Color.Transparent);

            GameplayRenderer.Begin();

            foreach (var item in AffectedEntities) {
                if (item is not null && item.Scene is not null && (requiredDepth is null || item.Depth == requiredDepth)) {
                    var v = item.Visible;
                    item.Visible = true;
                    item.Render();
                    item.Visible = v;
                }

            }

            GameplayRenderer.End();

            switch (Shader) {
                case "mask":
                    RenderMask(ShaderParameters);
                    return;
                case "blurMask":
                    RenderBlurMask(ShaderParameters);
                    return;
                case "vanilla.gaussianBlur":
                    RenderVanillaBlur(ShaderParameters);
                    return;
            }

            var shader = ShaderHelperIntegration.GetEffect(Shader);
            shader.ApplyParametersFrom(ShaderParameters);

            BetterShaderTrigger.SimpleApply(maskTarget, GameplayBuffers.Gameplay, shader);
            Engine.Graphics.GraphicsDevice.SetRenderTarget(GameplayBuffers.Gameplay);
            GameplayRenderer.Begin();
        }

        public override void Render() {
            if (Engine.Scene != _firstScene) {
                return;
            }

            if (DynamicDepthPossibleDepths is not null) {
                Apply(AffectedEntities, Shader, ShaderParameters, Depth);
            } else {
                Apply(AffectedEntities, Shader, ShaderParameters, null);
            }
            
        }

        static VirtualRenderTarget temp;
        static VirtualRenderTarget temp2;
    }
}
