namespace FrostHelper.Helpers;

public static class RandomExt {
    /// <summary>
    /// Creates a random value out of two float values
    /// </summary>
    public static unsafe ulong SeededRandom(float x, float y) {
        ulong ix = *(uint*) &x;//Unsafe.As<float, uint>(ref x);
        ulong iy = *(uint*) &y;//Unsafe.As<float, uint>(ref y);

        return splitmix64(ix ^ iy << 32);
    }

    /// <summary>
    /// Creates a random value out of this Vector2
    /// </summary>
    public static ulong SeededRandom(this Vector2 pos) => SeededRandom(pos.X, pos.Y);

    /// <summary>
    /// Creates a random bool out of this vector
    /// </summary>
    public static bool SeededRandomBool(this Vector2 pos) => SeededRandom(pos.X, pos.Y) >= ulong.MaxValue / 2;

    /// <summary>
    /// Creates a random int out of this Vector2
    /// </summary>
    public static int SeededRandomExclusive(this Vector2 pos, int max) => (int) (SeededRandom(pos.X, pos.Y) % (ulong) max);

    /// <summary>
    /// Creates a random int out of this Vector2, between min and max (inclusive)
    /// </summary>
    public static int SeededRandomInclusive(this Vector2 pos, int min, int max) => min + (int) (SeededRandom(pos.X, pos.Y) % (ulong) (max - min + 1));

    public static T SeededRandomFrom<T>(this Vector2 pos, IReadOnlyList<T> values) {
        var len = values.Count;

        return values[pos.SeededRandomInclusive(0, len - 1)];
    }

    #region Splitmix64
    /*  Written in 2015 by Sebastiano Vigna (vigna@acm.org)

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
    static ulong splitmix64(ulong seed) {
        ulong z = seed += 0x9e3779b97f4a7c15;
        z = (z ^ z >> 30) * 0xbf58476d1ce4e5b9;
        z = (z ^ z >> 27) * 0x94d049bb133111eb;
        return z ^ z >> 31;
    }
    #endregion
}
