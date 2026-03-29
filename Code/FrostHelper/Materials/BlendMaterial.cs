namespace FrostHelper.Materials;

internal sealed class BlendMaterial(IReadOnlyList<IMaterial> materials) : IMaterial {
    public void Dispose() {
        
    }

    public void Fill(Rectangle bounds, in RenderContext ctx) {
        foreach (var mat in materials) {
            mat.Fill(bounds, ctx);
        }
    }
}

[CustomEntity("FrostHelper/Materials/Blend")]
internal sealed class BlendMaterialSource(EntityData data, Vector2 offset) : MaterialSource(data, offset) {
    private readonly string[] _names = data.Attr("toBlend").Split(',');
    
    public override IMaterial CreateMaterial(MaterialManager manager) {
        var materials = _names.SelectNotNull(x => manager.TryGet(x, out var mat) ? mat : null);

        return new BlendMaterial(materials.ToList());
    }
}
