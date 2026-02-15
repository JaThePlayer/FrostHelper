namespace FrostHelper.Helpers;

/// <summary>
/// Provides a <see cref="Random"/> instance that can be used for non-gameplay critical randomness in entity ctors without affecting TASes.
/// This is not seeded in any way, don't expect any sort of determinism from this!
/// </summary>
public static class VisualRandom {
    public static Random Instance { get; private set; } = new();
    
    /// <summary>
    /// Creates a random value out of two float values
    /// </summary>
    public static ulong AtPosition(float x, float y) {
        ulong ix = BitConverter.SingleToUInt32Bits(x);
        ulong iy = BitConverter.SingleToUInt32Bits(y);

        return Splitmix64(ix ^ iy << 32);
    }

    /// <summary>
    /// Creates a new Random instance, that's consistently seeded based on the given position.
    /// </summary>
    public static Random CreateAt(float x, float y) => new Random((int)AtPosition(x, y));
    
    public static float RangeInclusiveAtPos(Vector2 pos, float min, float max) {
        var rand = (float) AtPosition(pos.X, pos.Y);
        var ret = Calc.Map(rand, 0, ulong.MaxValue, min, max);

        return ret;
    }

    public static float RangeInclusiveAtPos(Vector2 pos, float max) => RangeInclusiveAtPos(pos, 0f, max);
    
    /*
     Written in 2015 by Sebastiano Vigna (vigna@acm.org)

       To the extent possible under law, the author has dedicated all copyright
       and related and neighboring rights to this software to the public domain
       worldwide. This software is distributed without any warranty.

       See <http://creativecommons.org/publicdomain/zero/1.0/>.

       This is a fixed-increment version of Java 8's SplittableRandom generator
       See http://dx.doi.org/10.1145/2714064.2660195 and
       http://docs.oracle.com/javase/8/docs/api/java/util/SplittableRandom.html

       It is a very fast generator passing BigCrush, and it can be useful if
       for some reason you absolutely want 64 bits of state.
    */
    private static ulong Splitmix64(ulong seed) {
        ulong z = seed += 0x9e3779b97f4a7c15;
        z = (z ^ z >> 30) * 0xbf58476d1ce4e5b9;
        z = (z ^ z >> 27) * 0x94d049bb133111eb;
        return z ^ z >> 31;
    }
}
