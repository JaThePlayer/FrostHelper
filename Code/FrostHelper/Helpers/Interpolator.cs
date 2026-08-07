using System.Buffers;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Unicode;

namespace FrostHelper.Helpers;

/// <summary>
/// Provides a shared buffer for string interpolation, allowing for 0-allocation string interpolation into a ReadOnlySpan.
/// </summary>
public sealed class Interpolator {
    [ThreadStatic]
    private static Interpolator _shared;
    
    private char[] _buffer = new char[128];
    
    private byte[] _bufferU8 = new byte[128];

    public char[] Buffer => _buffer;
    
    public byte[] BufferU8 => _bufferU8;

    private int _startIndex;

    /// <summary>
    /// If false, subsequent calls to Interpolate will overwrite the buffer returned from the previous call.
    /// </summary>
    public bool ManualClear { get; set; } = false;

    /// <summary>
    /// Returns a ThreadStatic shared buffer
    /// </summary>
    public static Interpolator Shared => _shared ??= new();
    
    /// <summary>
    /// Returns a ThreadStatic, manually cleared, shared buffer
    /// </summary>
    public static Interpolator SharedManualClear => _shared ??= new() {
        ManualClear = true
    };

    /// <summary>
    /// Interpolates into a temporary buffer that will be overwritten the next time this method is called.
    /// </summary>
    public static ReadOnlySpan<char> Temp(Handler h)
        => Shared.Utf16(h);
    
    /// <summary>
    /// Interpolates into a temporary buffer that will be overwritten the next time this method is called.
    /// </summary>
    public static ReadOnlySpan<byte> TempU8(HandlerU8 h)
        => Shared.Utf8(h);
    
    /// <summary>
    /// Interpolates into a temporary buffer that will be overwritten the next time this method is called.
    /// </summary>
    public static ReadOnlySpan<byte> TempU8(ReadOnlySpan<char> h)
        => Shared.Utf8($"{h}");
    
    /// <summary>
    /// Interpolates into a buffer that will only be overwritten the next time <see cref="ClearPreserved"/> is called.
    /// </summary>
    public static ReadOnlySpan<char> Preserved(Handler h)
        => SharedManualClear.Utf16(h);

    public static void ClearPreserved() {
        SharedManualClear.Clear();
    }

    public ReadOnlySpan<char> Utf16([InterpolatedStringHandlerArgument("")] Handler h)
    {
        _buffer = h.Data;
        if (ManualClear) {
            // Move the start index further so that later interpolation steps don't overwrite existing ones
            _startIndex += h.Result.Length;
        }
        
        return h.Result;
    }
    
    public Span<char> Utf16Mutable([InterpolatedStringHandlerArgument("")] Handler h)
    {
        _buffer = h.Data;
        if (ManualClear) {
            // Move the start index further so that later interpolation steps don't overwrite existing ones
            _startIndex += h.Result.Length;
        }
        
        return h.Result;
    }
    
    public ReadOnlySpan<byte> Utf8([InterpolatedStringHandlerArgument("")] HandlerU8 h)
    {
        // Make sure the buffer ends with a null terminator, as some imgui functions expect it.
        if (h.Result is not [.., (byte)'\0'])
            h.AppendLiteral("\0"u8);
        
        _bufferU8 = h.Data;
        if (ManualClear) {
            // Move the start index further so that later interpolation steps don't overwrite existing ones
            _startIndex += h.Result.Length;
        }
        
        // Chop off the null terminator from the returned span though.
        return h.Result[..^1];
    }
    
    public ReadOnlySpan<byte> Utf8(ReadOnlySpan<char> utf16) {
        return Utf8($"{utf16}");
    }
    
    public Span<char> Clone(ReadOnlySpan<char> str) {
        if (str.Length == 0)
            return [];
        
        if (_buffer.Length < str.Length + _startIndex) {
            Array.Resize(ref _buffer, Math.Max(_buffer.Length * 3 / 2, str.Length + _startIndex));
        }
        
        var b = _buffer.AsSpan()[_startIndex..];
        str.CopyTo(b);
        if (ManualClear) {
            // Move the start index further so that later interpolation steps don't overwrite existing ones
            _startIndex += str.Length;
        }

        return b[..str.Length];
    }
    
    public Span<byte> Clone(ReadOnlySpan<byte> str) {
        if (str.Length == 0)
            return [];
        
        if (_bufferU8.Length < str.Length + _startIndex) {
            Array.Resize(ref _bufferU8, Math.Max(_bufferU8.Length * 3 / 2, str.Length + _startIndex));
        }
        
        var b = _bufferU8.AsSpan()[_startIndex..];
        str.CopyTo(b);
        if (ManualClear) {
            // Move the start index further so that later interpolation steps don't overwrite existing ones
            _startIndex += str.Length;
        }

        return b[..str.Length];
    }

    /// <summary>
    /// If <see cref="ManualClear"/> is enabled, clears the buffer so that further interpolations start writing from the 0th index.
    /// </summary>
    public void Clear() {
        _startIndex = 0;
    }
    
    [InterpolatedStringHandler]
    public ref struct HandlerU8 {
        //private Utf8.TryWriteInterpolatedStringHandler _innerHandler;
        
        public HandlerU8(int literalLength, int formattedCount, Interpolator buffer) {
            Data = buffer._bufferU8;
            _startIndex = buffer._startIndex;
            //_innerHandler = new(literalLength, formattedCount, Data, null, out var shouldAppend);
        }

        public HandlerU8(int literalLength, int formattedCount) : this(literalLength, formattedCount, Shared) { }

        private int _len;
        private readonly int _startIndex;
        internal byte[] Data;
    
        public readonly int CapacityLeft => Data.Length - (_startIndex + _len);
        
        private Span<byte> RemainingSpan() => Data.AsSpan(_startIndex + _len);

        private void Expand(int newSize)
        {
            Array.Resize(ref Data, newSize);
        }
        
        public ReadOnlySpan<byte> Result => Data.AsSpan(_startIndex, _len);

        public int Length => _len;
        
        public void AppendLiteral(ReadOnlySpan<byte> data)
        {
            
            if (data.Length > CapacityLeft)
            {
                Expand(Data.Length + data.Length);
            }
            
            data.CopyTo(RemainingSpan());
            _len += data.Length;
        }
        
        public void AppendLiteral(ReadOnlySpan<char> data) {
            int written;
            while (System.Text.Unicode.Utf8.FromUtf16(data, RemainingSpan(), out _, out written) is OperationStatus.DestinationTooSmall) {
                Expand(Data.Length * 2);
            }
            _len += written;
        }
        
        public void AppendFormatted(ReadOnlySpan<byte> str)
        {
            AppendLiteral(str);
        }
        
        public void AppendFormatted(ReadOnlySpan<char> str)
        {
            AppendLiteral(str);
        }
        
        public void AppendFormatted<T2>(T2 v) where T2 : IUtf8SpanFormattable
        {
            int written;
            while (!v.TryFormat(RemainingSpan(), out written, "", null))
            {
                Expand(Data.Length * 2);
            }
            
            _len += written;
        }
    }
    
    [InterpolatedStringHandler]
    public ref struct Handler {
        public IFormatProvider? FormatProvider { get; set; } = CultureInfo.InvariantCulture;
        
        public Handler(int literalLength, int formattedCount, Interpolator buffer) {
            Data = buffer._buffer;
            _startIndex = buffer._startIndex;
        }

        public Handler(int literalLength, int formattedCount) : this(literalLength, formattedCount, Shared) { }

        private int _len;
        private readonly int _startIndex;
        internal char[] Data;
    
        public readonly int CapacityLeft => Data.Length - (_startIndex + _len);
        
        private Span<char> RemainingSpan() => Data.AsSpan(_startIndex + _len);

        private void Expand(int newSize)
        {
            Array.Resize(ref Data, newSize);
        }
        
        public Span<char> Result => Data.AsSpan(_startIndex, _len);

        public string ResultToString() => Result.ToString();

        public int Length => _len;
        
        public void AppendLiteral(ReadOnlySpan<char> data)
        {
            if (data.Length > CapacityLeft)
            {
                Expand(Data.Length + data.Length);
            }
            
            data.CopyTo(RemainingSpan());
            _len += data.Length;
        }

        public void AppendLiteral(string data) {
            AppendLiteral(data.AsSpan());
        }

        // Just to get rid of the implicit conversion marker in Rider
        public void AppendFormatted(string data)
            => AppendLiteral(data.AsSpan());
        
        public void AppendFormatted(ReadOnlySpan<char> str)
        {
            AppendLiteral(str);
        }
        
        public void AppendFormatted(ReadOnlySpan<byte> str)
        {
            int written;
            while (System.Text.Unicode.Utf8.ToUtf16(str, RemainingSpan(), out _, out written) is OperationStatus.DestinationTooSmall) {
                Expand(Data.Length * 2);
            }
            _len += written;
        }
        
        public void AppendFormatted<T2>(T2 v) where T2 : ISpanFormattable
        {
            int written;
            while (!v.TryFormat(RemainingSpan(), out written, "", FormatProvider))
            {
                Expand(Data.Length * 2);
            }
            
            _len += written;
        }
        
        public void AppendFormatted(object v) {
            if (v is IFormattable formattable) {
                AppendLiteral(formattable.ToString(null, FormatProvider));
                return;
            }
            
            AppendLiteral(v.ToString());
        }
    }
}
