using FrostHelper.ModIntegration;

namespace FrostHelper.Backdrops;

/// <summary>
/// Shader wrapper which allows rendering stylegrounds with a colorgrade
/// </summary>
internal sealed class ColorgradeWrapper : ShaderWrapperBackdrop {
    private MTexture _colorGradeImage;
    
    public ColorgradeWrapper(BinaryPacker.Element child) : base(child, EffectRef.AltColorGrade) {
        _colorGradeImage = GFX.ColorGrades[child.Attr("colorgrade", "none")];
    }

    protected override void SetEffectParams(Effect effect) {
        base.SetEffectParams(effect);

        var prevColorGrading = GFX.FxColorGrading;
        GFX.FxColorGrading = effect;
        ColorGrade.Set(_colorGradeImage);

        GFX.FxColorGrading = prevColorGrading;
    }
}