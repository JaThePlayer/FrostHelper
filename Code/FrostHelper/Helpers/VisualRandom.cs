namespace FrostHelper.Helpers;

/// <summary>
/// Provides a <see cref="Random"/> instance that can be used for non-gameplay critical randomness in entity ctors without affecting TASes.
/// This is not seeded in any way, don't expect any sort of determinism from this!
/// </summary>
public static class VisualRandom {
    public static Random Instance { get; private set; } = new();
}
