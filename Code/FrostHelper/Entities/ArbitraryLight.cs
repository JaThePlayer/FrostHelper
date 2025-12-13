using FrostHelper.Helpers;
using System.Runtime.CompilerServices;
using LightingRenderer = Celeste.LightingRenderer;

namespace FrostHelper.Entities;

[CustomEntity("FrostHelper/ArbitraryLight")]
internal sealed class ArbitraryLightEntity : Entity {
    public ArbitraryLightEntity(EntityData data, Vector2 offset) : base(data.Position + offset) {
        Add(new ArbitraryLight(data, offset));
        Visible = true;
        Active = true;
    }
}


[TrackedAs(typeof(VertexLight))]
internal sealed class ArbitraryLight : VertexLight {
    #region Hooks
    private static bool _hooksLoaded;

    public static void LoadHooksIfNeeded() {
        if (_hooksLoaded)
            return;
        _hooksLoaded = true;
        
        On.Celeste.LightingRenderer.DrawLight += LightingRendererOnDrawLight;
        IL.Celeste.LightingRenderer.BeforeRender += LightingRendererOnBeforeRender;
        IL.Celeste.LightingRenderer.DrawLightOccluders += LightingRendererOnDrawLightOccluders;
    }

    [OnUnload]
    public static void UnloadHooks() {
        if (!_hooksLoaded)
            return;
        _hooksLoaded = false;
        
        On.Celeste.LightingRenderer.DrawLight -= LightingRendererOnDrawLight;
        IL.Celeste.LightingRenderer.BeforeRender -= LightingRendererOnBeforeRender;
        IL.Celeste.LightingRenderer.DrawLightOccluders -= LightingRendererOnDrawLightOccluders;
    }
    
    // Get rid of occluder handling
    private static void LightingRendererOnDrawLightOccluders(ILContext il) {
        var cursor = new ILCursor(il);
        
        var vertexLightLoc = il.Body.Variables.First(v => v.Index == 6);
        
        ILLabel? dontRenderLabel = null;
        
        // - if (vertexLight != null && vertexLight.Dirty)
        // + if (vertexLight != null && vertexLight.Dirty && vertexLight is not ArbitraryLight)
        if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<VertexLight>(nameof(Dirty)))) {
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchBrfalse(out dontRenderLabel))) {
                cursor.Emit(OpCodes.Ldloc, vertexLightLoc);
                cursor.EmitCall(IsArbitraryLight);
                cursor.Emit(OpCodes.Brtrue, dontRenderLabel!);
            }
        }
    }

    // Handle rendering
    private static void LightingRendererOnDrawLight(On.Celeste.LightingRenderer.orig_DrawLight orig, LightingRenderer self, int index, Vector2 position, Color color, float radius) {
        if (index >= self.lights.Length) {
            orig(self, index, position, color, radius);
            return;
        }
        var light = self.lights[index];

        if (light is not ArbitraryLight arbLight) {
            orig(self, index, position, color, radius);
            return;
        }
        
        Color mask = self.GetMask(index, 1f, 0f);
        Vector3 center = self.GetCenter(index);
        
        radius = arbLight.Radius;
        var lightPos = arbLight.Position + arbLight.Entity.Position;
        
        foreach (var vRelative in arbLight.Fill) {
            if (self.indexCount >= self.indices.Length)
                return;
            self.indices[self.indexCount++] = self.vertexCount;

            if (self.vertexCount >= self.resultVerts.Length)
                return;

            var v = new Vector3(vRelative.X + lightPos.X, vRelative.Y + lightPos.Y, vRelative.Z);
            ref var vert = ref self.resultVerts[self.vertexCount++];
            vert.Position = v;
            vert.Color = color;
            vert.Mask = mask;

            if (new Vector2(v.X, v.Y) != arbLight.Center) {
                vert.Texcoord = 
                    new Vector2(center.X + radius, center.Y + radius) / 1024f; 
            } else {
                vert.Texcoord = 
                    new Vector2(center.X, center.Y) / 1024f; 
            }
        }
    }
    
    // Update bounds checks
    private static void LightingRendererOnBeforeRender(ILContext il) {
        var cursor = new ILCursor(il);

        var vertexLightLoc = il.Body.Variables.First(v => v.Index == 4);
        
        // Replace bounds checks:
        // - if ((component.Entity == null || !component.Entity.Visible || !component.Visible || (double) component.Alpha <= 0.0 || component.Color.A <= (byte) 0 || (double) component.Center.X + (double) component.EndRadius <= (double) camera.X || (double) component.Center.Y + (double) component.EndRadius <= (double) camera.Y || (double) component.Center.X - (double) component.EndRadius >= (double) camera.X + 320.0 ? 0 : ((double) component.Center.Y - (double) component.EndRadius < (double) camera.Y + 180.0 ? 1 : 0)) != 0)
        // + if ((component.Entity == null || !component.Entity.Visible || ((IsVisibleArbitraryLight(component) || !component.Visible || (double) component.Alpha <= 0.0 || component.Color.A <= (byte) 0 || (double) component.Center.X + (double) component.EndRadius <= (double) camera.X || (double) component.Center.Y + (double) component.EndRadius <= (double) camera.Y || (double) component.Center.X - (double) component.EndRadius >= (double) camera.X + 320.0 ? 0 : ((double) component.Center.Y - (double) component.EndRadius < (double) camera.Y + 180.0 ? 1 : 0)) != 0))
        // first, find the end of this long if chain:
        ILLabel? dontRenderLabel = null;
        if (cursor.TryGotoNext(MoveType.Before, instr => instr.MatchLdfld<VertexLight>(nameof(VertexLight.Index)))) {
            if (cursor.TryGotoPrev(MoveType.After, instr => instr.MatchBrfalse(out dontRenderLabel))) {
                var renderLabel = cursor.MarkLabel();
                cursor.Index = 0;
                
                // now, find the Visible check
                if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<Entity>(nameof(Entity.Visible)))) {
                    if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchBrfalse(out _))) {
                        cursor.Emit(OpCodes.Ldloc, vertexLightLoc);
                        cursor.EmitCall(IsVisibleArbitraryLight);
                        cursor.Emit(OpCodes.Brtrue, renderLabel);
                        
                        cursor.Emit(OpCodes.Ldloc, vertexLightLoc);
                        cursor.EmitCall(IsArbitraryLight);
                        cursor.Emit(OpCodes.Brtrue, dontRenderLabel!);
                    }
                }
            }
        }
        
        // get rid of InSolid checks
        // - if (vertexLight.LastPosition != vertexLight.Position || vertexLight.LastEntityPosition != vertexLight.Entity.Position || vertexLight.Dirty)
        // + if (vertexLight.LastPosition != vertexLight.Position || vertexLight.LastEntityPosition != vertexLight.Entity.Position || vertexLight.Dirty) && vertexLight is not ArbitraryLight)
        if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<VertexLight>(nameof(Dirty)))) {
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchBrfalse(out dontRenderLabel))) {
                cursor.Emit(OpCodes.Ldloc, vertexLightLoc);
                cursor.EmitCall(IsArbitraryLight);
                cursor.Emit(OpCodes.Brtrue, dontRenderLabel!);
            }
        }
    }

    private static bool IsVisibleArbitraryLight(VertexLight light) {
        if (light is not ArbitraryLight arbitraryLight || Engine.Scene is not Level level)
            return false;

        return CameraCullHelper.IsRectangleVisible(arbitraryLight.Bounds.MovedBy(light.Position + light.Entity.Position), 4f, level.Camera)
               && arbitraryLight.Condition.Check();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsArbitraryLight(VertexLight light) => light is ArbitraryLight;
    
    #endregion
    
    public readonly Vector3[] Fill;

    public Rectangle Bounds;

    public readonly float Radius;

    private readonly ArbitraryBloom? _bloom;

    public readonly ConditionHelper.Condition Condition;
    
    public ArbitraryLight(EntityData data, Vector2 offset) : this(data.Position + offset,
        data.GetColor("color", "ffffff"), data.Float("alpha", 1f), data.Int("startFade", 16), data.Int("endFade", 32),
        data.NodesOffset(offset), data.Bool("connectFirstAndLastNode", true), data.Float("radius"), data.Float("bloomAlpha", 0f),
        data.GetCondition("flag")) {
    }

    public ArbitraryLight(Vector2 position, Color color, float alpha, int startFade, int endFade,
                          Vector2[] nodes, bool connectFirstAndLastNode, float radius, float bloomAlpha,
                          ConditionHelper.Condition condition) : base(Vector2.Zero, color, alpha, startFade, endFade) {
        LoadHooksIfNeeded();
        
        Condition = condition;
        
        var fill = new Vector3[nodes.Length * 3 - (connectFirstAndLastNode ? 0 : 3)];
        var fi = 0;
        for (int i = 0; i < nodes.Length - 1; i++) {
            fi++; // skip 1 vertex as it's placed on 0,0,0 which is default(Vector3)
            fill[fi++] = new(nodes[i] - position, 0f);
            fill[fi++] = new(nodes[i + 1] - position, 0f);
        }
        
        if (connectFirstAndLastNode) {
            fi++; // skip 1 vertex as it's placed on 0,0,0 which is default(Vector3)
            fill[fi++] = new(nodes[0] - position, 0f);
            fill[fi++] = new(nodes[^1] - position, 0f);
        }

        Fill = fill;
        Bounds = RectangleExt.FromPointsFromXY(fill);
        Radius = radius;

        if (bloomAlpha > 0f) {
            _bloom = new ArbitraryBloom(bloomAlpha, Fill, () => Position + Entity.Position);
        }
    }
    

    public override void EntityAdded(Scene scene) {
        base.EntityAdded(scene);

        if (_bloom is { } bloom) {
            var controller = ControllerHelper<ArbitraryBloomRenderer>.AddToSceneIfNeeded(scene);
            controller.Add(bloom);
        }
    }
    
    public override void EntityRemoved(Scene scene) {
        base.EntityRemoved(scene);

        if (_bloom is { } bloom) {
            var controller = ControllerHelper<ArbitraryBloomRenderer>.AddToSceneIfNeeded(scene);
            controller.Remove(bloom);
        }
    }

    /*
    public override void DebugRender(Camera camera) {
        base.DebugRender(camera);
        if (Fill is null)
            return;
        
        var lightPos = Position + Entity.Position;
        Draw.HollowRect(Bounds.MovedBy(lightPos), Color.Pink);
        for (int i = 0; i < Fill.Length - 2; i += 3) {
            var vert1 = Fill[i].AddXY(lightPos);
            var vert2 = Fill[i + 1].AddXY(lightPos);
            var vert3 = Fill[i + 2].AddXY(lightPos);
            
            Draw.Line(new Vector2(vert1.X, vert1.Y), new Vector2(vert2.X, vert2.Y), Color.Pink);
            Draw.Line(new Vector2(vert2.X, vert2.Y), new Vector2(vert3.X, vert3.Y), Color.Pink);
            Draw.Line(new Vector2(vert1.X, vert1.Y), new Vector2(vert3.X, vert3.Y), Color.Pink);
        }
    }
    */
}
