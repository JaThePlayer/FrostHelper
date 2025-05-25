namespace FrostHelper.Helpers;

internal readonly struct EquatableArray<T>(T[] backing) : IEquatable<EquatableArray<T>> {
    public T[] Backing => backing;
    
    public int Length => backing.Length;
    
    public T this[int index] => backing[index];
    
    public Span<T> AsSpan() => backing.AsSpan();
    
    public bool Equals(EquatableArray<T> other) => Backing.SequenceEqual(other.Backing);

    public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

    public override int GetHashCode() {
        var h = new HashCode();
        foreach (var x in Backing)
            h.Add(x);
        return h.ToHashCode();
    }
    
    public static implicit operator T[](EquatableArray<T> array) => array.Backing;
    public static implicit operator EquatableArray<T>(T[] array) => new(array);
}