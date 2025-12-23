namespace FrostHelper.Helpers;

/// <summary>
/// Helper to make nullable structs work well in generic methods not constrained to structs.
/// </summary>
public readonly struct Maybe<T> {
    public bool HasValue { get; }
    
    public T Value => HasValue ? field : throw new InvalidOperationException();

    public Maybe(T value) {
        Value = value ?? throw new ArgumentNullException(nameof(value));
        HasValue = true;
    }
    
    public Maybe() {
        Value = default!;
        HasValue = false;
    }
    
    public static implicit operator Maybe<T>(T value) => new Maybe<T>(value);
}