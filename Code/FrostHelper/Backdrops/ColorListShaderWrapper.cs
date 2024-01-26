namespace FrostHelper.Backdrops;

/// <summary>
/// Shader wrapper accepting a list of colors as a uniform
/// </summary>
internal class ColorListShaderWrapper : ShaderWrapperBackdrop {
    private readonly Vector4[] _colors;
    
    public ColorListShaderWrapper(BinaryPacker.Element child) : base(child) {
        _colors = ColorHelper.GetColors(child.Attr("colors", "ffffff")).Select(c => c.ToVector4()).ToArray();
    }

    protected override void SetEffectParams(Effect effect) {
        base.SetEffectParams(effect);

        effect.Parameters["Colors"].SetValue(_colors);
    }
}