﻿namespace FrostHelper.Effects;

internal sealed class EntityBackdrop : Backdrop {
    public int Layer;
    public bool AddParallax, MakeUncollidable;

    public EntityBackdrop(BinaryPacker.Element child) {
        Layer = child.AttrInt("layer");
        AddParallax = child.AttrBool("addParallax");
        MakeUncollidable = child.AttrBool("makeUncollidable");

        EXPERIMENTAL.LayerHelper.Load();
    }

    public Vector2 RenderPositionAtCamera(Vector2 position, Vector2 camera) {
        Vector2 value = position - camera;
        Vector2 vector = Vector2.Zero;
            vector -= value * Scroll;
        return position + vector;
    }

    public override void Render(Scene scene) {
        base.Render(scene);
        var l = (scene as Level)!;
        Renderer.EndSpritebatch();
        GameplayRenderer.Begin();

        Vector2 camPos = (l.Camera.Position).Floor();
        foreach (var item in EXPERIMENTAL.LayerHelper.GetEntitiesOnLayer(Layer)) {
            if (item.Scene is not null) {
                var lastPos = item.Position;
                item.Visible = true;

                if (AddParallax)
                    item.Position = RenderPositionAtCamera(item.Position, camPos + new Vector2(160f, 90f));//+= (item.Position - l.LevelOffset - camPos * Scroll).Floor();
                //item.Position += camPos;
                //item.Position = Vector2.Transform(item.Position, l.Camera.Matrix);
                item.Render();
                item.Position = lastPos;
                item.Visible = false; // make the entity invisible so it doesn't render normally
                if (MakeUncollidable)
                    item.Collidable = false;

                item.Get<DisplacementRenderHook>()?.RemoveSelf();
                item.Get<CustomBloom>()?.RemoveSelf();
                item.Get<BloomPoint>()?.RemoveSelf();
            }

        }
        GameplayRenderer.End();
        Renderer.StartSpritebatch(BlendState.AlphaBlend);

    }
}
