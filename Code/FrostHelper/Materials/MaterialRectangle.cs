using FrostHelper.Helpers;

namespace FrostHelper.Materials;

[CustomEntity("FrostHelper/MaterialRectangle")]
internal sealed class MaterialRectangleEntity : Entity {
    public MaterialRectangleEntity(EntityData data, Vector2 offset) : base(data.Position + offset) {
        Depth = data.Int("depth");
        
        Add(new MaterialRectangle(data.Attr("name")) {
            Width = data.Width,
            Height = data.Height,
            Tint = data.GetColor("tint", "ffffff"),
        });
    }
}

/// <summary>
/// Renders a rectangle out of a given material.
/// </summary>
internal sealed class MaterialRectangle(string materialName) : Component(false, true) {
    public int Width { get; set; }

    public int Height { get; set; }
    
    public Vector2 Offset { get; set; }

    public Color Tint { get; set; } = Color.White;
    
    public Vector2 RenderPosition => (Entity?.Position ?? Vector2.Zero) + Offset;
    
    public Rectangle Bounds => RectangleExt.CreateTruncating(RenderPosition, Width, Height);
    
    public override void Render() {
        if (!MaterialManager.GetFor(Scene).TryGet(materialName, out var material))
            return;
        
        material.Fill(Bounds, RenderContext.CreateFor(RenderPosition, Scene));
        
        //Draw.HollowRect(Bounds, Tint);
    }
}