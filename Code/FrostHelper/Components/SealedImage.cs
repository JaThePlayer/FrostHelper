namespace FrostHelper.Components;

/// <summary>
/// Same as Image, but sealed for perf
/// </summary>
internal sealed class SealedImage : Image {
    public SealedImage(MTexture texture) : base(texture)
    {
    }

    public SealedImage(MTexture texture, bool active) : base(texture, active)
    {
    }
}