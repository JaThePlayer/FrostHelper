﻿using FrostHelper.Helpers;

namespace FrostHelper.Entities;

[Tracked]
internal sealed class CustomDustGraphic : DustGraphic {
    #region Hooks
    private static bool _hooksLoaded = false;

    internal static void LoadHooksIfNeeded() {
        if (_hooksLoaded) {
            return;
        }
        _hooksLoaded = true;

        On.Celeste.DustGraphic.Added += DustGraphic_Added;
        On.Celeste.DustEdges.BeforeRender += DustEdges_BeforeRender;
        IL.Celeste.DustGraphic.AddNode += DustGraphic_AddNode;
        IL.Celeste.DustGraphic.OnHitPlayer += DustGraphic_OnHitPlayer;
        IL.Celeste.DustGraphic.AddDustNodesIfInCamera += DustGraphic_AddDustNodesIfInCamera;
        IL.Celeste.DustGraphic.Eyeballs.Added += Eyeballs_Added;
        On.Celeste.DustGraphic.Eyeballs.Render += Eyeballs_Render;
        On.Celeste.DustEdges.Render += DustEdges_Render;
    }

    [OnUnload]
    internal static void UnloadHooks() {
        if (!_hooksLoaded) {
            return;
        }
        _hooksLoaded = false;

        On.Celeste.DustGraphic.Added -= DustGraphic_Added;
        On.Celeste.DustEdges.BeforeRender -= DustEdges_BeforeRender;
        IL.Celeste.DustGraphic.AddNode -= DustGraphic_AddNode;
        IL.Celeste.DustGraphic.OnHitPlayer -= DustGraphic_OnHitPlayer;
        IL.Celeste.DustGraphic.AddDustNodesIfInCamera -= DustGraphic_AddDustNodesIfInCamera;
        IL.Celeste.DustGraphic.Eyeballs.Added -= Eyeballs_Added;
        On.Celeste.DustGraphic.Eyeballs.Render -= Eyeballs_Render;
        On.Celeste.DustEdges.Render -= DustEdges_Render;
    }

    internal static void Track(DustEdges controller, CustomDustEdge edge) {
        var tracker = controller.GetOrCreateAttached<DustEdgesTracker>();

        if (tracker.EdgeColorCache.TryGetValue(edge.Graphic.EdgeColors, out var cache)) {
            cache.Add(edge);
        } else {
            tracker.EdgeColorCache[edge.Graphic.EdgeColors] = [edge];
        }
    }

    internal static void Untrack(DustEdges controller, CustomDustEdge edge) {
        var tracker = controller.GetOrCreateAttached<DustEdgesTracker>();

        if (tracker.EdgeColorCache.TryGetValue(edge.Graphic.EdgeColors, out var cache)) {
            cache.Remove(edge);

            if (cache.Count == 0)
                tracker.EdgeColorCache.Remove(edge.Graphic.EdgeColors);
        }
    }

    // support rainbow eyes, optimise
    private static void Eyeballs_Render(On.Celeste.DustGraphic.Eyeballs.orig_Render orig, Entity _self) {
        var self = ((Eyeballs) _self)!;

        if (self.Dust is not CustomDustGraphic custom) {
            orig(_self);
            return;
        }

        if (!custom.Visible || !custom.Entity.Visible) {
            return;
        }

        var left = custom.leftEyeVisible;
        var right = custom.rightEyeVisible;

        if (!left && !right)
            return;

        Vector2 perpendicularEyeDir = new Vector2(-custom.EyeDirection.Y, custom.EyeDirection.X).SafeNormalize() * 3f;
        var eyeDirTimesFive = custom.EyeDirection * 5f;

        var eyeTexture = custom.eyeTexture;
        var pos = custom.RenderPosition;
        var rainbow = custom.RainbowEyes;

        if (left) {
            var leftPos = pos + (eyeDirTimesFive + perpendicularEyeDir) * custom.Scale;

            eyeTexture.DrawCentered(leftPos, rainbow ? ColorHelper.GetHue(self.Scene, leftPos) : self.Color, custom.Scale);
        }

        if (right) {
            var rightPos = pos + (eyeDirTimesFive - perpendicularEyeDir) * custom.Scale;

            eyeTexture.DrawCentered(rightPos, rainbow ? ColorHelper.GetHue(self.Scene, rightPos) : self.Color, custom.Scale);
        }
    }

    // main rendering method
    private static void DustEdges_BeforeRender(On.Celeste.DustEdges.orig_BeforeRender orig, DustEdges self) {
        orig(self);

        var edges = self.Scene.Tracker.GetComponents<CustomDustEdge>();
        if (edges.Count == 0)
            return;

        self.hasDust = true;

        var gd = Engine.Graphics.GraphicsDevice;
        var b = Draw.SpriteBatch;
        var gb = GameplayBuffers.Gameplay;

        Vector2 cam = self.FlooredCamera();
        GFX.FxDust.Parameters["noiseEase"].SetValue(self.noiseEase);
        GFX.FxDust.Parameters["noiseFromPos"].SetValue(self.noiseFromPos + new Vector2(cam.X / gb.Width, cam.Y / gb.Height));
        GFX.FxDust.Parameters["noiseToPos"].SetValue(self.noiseToPos + new Vector2(cam.X / gb.Width, cam.Y / gb.Height));
        GFX.FxDust.Parameters["pixel"].SetValue(new Vector2(0.003125f * 320f / gb.Width, 0.00555555569f * 180f / gb.Height));

        if (self.DustNoiseFrom == null || self.DustNoiseFrom.IsDisposed) {
            self.CreateTextures();
        }
        gd.Textures[1] = self.DustNoiseFrom!.Texture_Safe;
        gd.Textures[2] = self.DustNoiseTo.Texture_Safe;

        gd.SetRenderTarget(GameplayBuffers.ResortDust);
        gd.Clear(Color.Transparent);

        var customDustBuffer = RenderTargetHelper<CustomDustBunny>.Get();

        gd.SetRenderTarget(customDustBuffer);
        gd.Clear(Color.Transparent);

        var shaderColorParameter = GFX.FxDust.Parameters["colors"];

        if (self.TryGetAttached<DustEdgesTracker>() is not { } tracker) {
            tracker = self.GetOrCreateAttached<DustEdgesTracker>();
            
            // After a savestate load, the tracker goes null.
            // Even if it wasn't, it would point to invalid entities
            // Let's re-track everything...
            foreach (var bunny in self.Scene.Tracker.SafeGetEntities<CustomDustBunny>()) {
                Track(self, bunny.Get<CustomDustEdge>());
            }
        }
        var colorGroups = tracker.EdgeColorCache;

        foreach (var group in colorGroups) {
            if (group.Value.Count == 0)
                continue;

            gd.SetRenderTarget(GameplayBuffers.TempA);
            gd.Clear(Color.Transparent);
            b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, (self.Scene as Level)!.Camera.Matrix);

            var anyRendered = false;
            foreach (CustomDustEdge edge in group.Value) {
                if (edge.Visible && edge.Entity.Visible) {
                    anyRendered |= edge.RenderDust();
                }
            }

            b.End();

            if (!anyRendered)
                continue;

            // render the raw dust bunnies to a different buffer to not have to re-render them later.
            gd.SetRenderTarget(customDustBuffer);
            b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);
            b.Draw(GameplayBuffers.TempA, Vector2.Zero, Color.White);
            b.End();

            gd.SetRenderTarget(GameplayBuffers.ResortDust);

            shaderColorParameter.SetValue(group.Key.Colors);

            b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, GFX.FxDust, Matrix.Identity);
            b.Draw(GameplayBuffers.TempA, Vector2.Zero, Color.White);
            b.End();
        }
    }

    // render the new buffer with all the sprites
    private static void DustEdges_Render(On.Celeste.DustEdges.orig_Render orig, DustEdges self) {
        orig(self);

        if (self.hasDust && self.Scene.Tracker.SafeGetEntity<CustomDustBunny>() is { }) {
            Vector2 position = self.FlooredCamera();
            Draw.SpriteBatch.Draw(RenderTargetHelper<CustomDustBunny>.Get(), position, Color.White);
        }
    }

    private static void DustGraphic_Added(On.Celeste.DustGraphic.orig_Added orig, DustGraphic self, Entity entity) {
        orig(self, entity);

        if (self is not CustomDustGraphic custom) {
            return;
        }

        entity.Remove(entity.Get<DustEdge>());

        if (/*custom.HasEdges*/ true) {
            var edge = new CustomDustEdge(custom, custom.RenderForEdges);
            entity.Add(edge);

            if (FrostModule.GetCurrentLevel().Tracker.GetEntity<DustEdges>() is { } edges)
                Track(edges, edge);
            else
                Engine.Scene.OnEndOfFrame += () => Track(FrostModule.GetCurrentLevel().Tracker.GetEntity<DustEdges>(), edge);
        }
    }

    // allow changing textures
    private static void DustGraphic_AddNode(ILContext il) {
        var cursor = new ILCursor(il);

        if (cursor.SeekLoadString("danger/dustcreature/base")) {
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitCall(GetBaseTexture);
        }

        if (cursor.SeekLoadString("danger/dustcreature/overlay")) {
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitCall(GetOverlayTexture);
        }
    }

    // allow changing textures
    private static void DustGraphic_OnHitPlayer(ILContext il) {
        var cursor = new ILCursor(il);

        if (cursor.SeekLoadString("danger/dustcreature/deadEyes")) {
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitCall(GetDeadEyesTexture);
        }
    }

    // allow changing textures
    private static void DustGraphic_AddDustNodesIfInCamera(ILContext il) {
        var cursor = new ILCursor(il);

        if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<DustStyles.DustStyle>(nameof(DustStyles.DustStyle.EyeTextures)))) {
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitCall(GetEyesTexture);
        }
    }

    // allow changing eye color
    private static void Eyeballs_Added(ILContext il) {
        var cursor = new ILCursor(il);

        if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<DustStyles.DustStyle>(nameof(DustStyles.DustStyle.EyeColor)))) {
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitCall(GetEyeColor);
        }
    }

    private static string GetBaseTexture(string orig, DustGraphic graphic) => graphic switch {
        CustomDustGraphic custom => custom.Graphics.Base,
        _ => orig,
    };

    private static string GetOverlayTexture(string orig, DustGraphic graphic) => graphic switch {
        CustomDustGraphic custom => custom.Graphics.Overlay,
        _ => orig,
    };

    private static string GetDeadEyesTexture(string orig, DustGraphic graphic) => graphic switch {
        CustomDustGraphic custom => custom.Graphics.DeadEyes,
        _ => orig,
    };

    private static string GetEyesTexture(string orig, DustGraphic graphic) => graphic switch {
        CustomDustGraphic custom => custom.Graphics.Eyes,
        _ => orig,
    };

    private static Color GetEyeColor(Color orig, Eyeballs eyeball) => eyeball.Dust switch {
        CustomDustGraphic custom => custom.EyeColor,
        _ => orig,
    };

    #endregion
    public readonly DustEdgeColors EdgeColors;

    public readonly DustGraphicsDirectory Graphics;

    public Color Color;
    public Color EyeColor;
    public bool Rainbow, RainbowEyes;

    //private bool InViewThisFrame;
    private readonly Texture2D CenterTexture;

    public CustomDustGraphic(DustEdgeColors edgeColors, DustGraphicsDirectory graphics, Color color, Color eyeColor, bool ignoreSolids, bool autoControlEyes = false, bool autoExpandDust = false) 
        : base(ignoreSolids, autoControlEyes, autoExpandDust) {
        EdgeColors = edgeColors;
        Graphics = graphics;
        Color = color;
        EyeColor = eyeColor;

        center = Calc.Random.Choose(GFX.Game.GetAtlasSubtextures(Graphics.Center));
        CenterTexture = center.Texture.Texture;
    }

    /*
    public override void Update() {
        base.Update();

        Visible = InViewThisFrame = InView;
    }*/

    public bool RenderForEdges() {
        if (!InView) {
            return false;
        }

        Vector2 renderPosition = RenderPosition;
        float scale = Scale;

        var b = Draw.SpriteBatch;

        foreach (Node node in nodes) {
            if (node.Enabled) {
                var pos = renderPosition + node.Angle * scale;
                var color = Rainbow ? ColorHelper.GetHue(Scene, pos) : Color;

                node.Base.DrawCentered(pos, color, scale, node.Rotation);
                //var nbase = node.Base;
                //b.Draw(nbase.Texture.Texture, pos, nbase.ClipRect, color, node.Rotation, nbase.Center, scale, SpriteEffects.None, 0f);

                node.Overlay.DrawCentered(pos, color, scale, -node.Rotation);
                //var noverlay = node.Overlay;
                //b.Draw(noverlay.Texture.Texture, pos, noverlay.ClipRect, color, -node.Rotation, noverlay.Center, scale, SpriteEffects.None, 0f);
            }
        }
        //center.DrawCentered(renderPosition, Rainbow ? ColorHelper.GetHue(Scene, renderPosition) : Color, scale, timer);
        var centerScaleFix = center.ScaleFix;
        b.Draw(CenterTexture, renderPosition, center.ClipRect, Rainbow ? ColorHelper.GetHue(Scene, renderPosition) : Color, timer, (center.Center - center.DrawOffset) / centerScaleFix, scale * centerScaleFix, SpriteEffects.None, 0f);
        return true;
    }

    // DustEdges now renders dust bunnies into a render target, no need to re-render this here
    public sealed override void Render() {
    }
}