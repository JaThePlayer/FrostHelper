using System.Globalization;
using System.Runtime.CompilerServices;

namespace FrostHelper.Helpers;

internal ref struct SpanParser(ReadOnlySpan<char> input)
{
    private ReadOnlySpan<char> Remaining = input;

    private Res<T> ReadSlice<T>(IFormatProvider? format, int len) where T : ISpanParsable<T>
    {
        var success = T.TryParse(Remaining[..len], format ?? CultureInfo.InvariantCulture, out var ret);

        //Remaining = Remaining.Length >= len ? Remaining[len..] : Remaining[(len + 1)..];
        Remaining = Remaining[len..];
        
        if (!success)
        {
            return new(default!, false);
        }

        return new Res<T>(ret!, true);
    }
    
    private ReadOnlySpan<char> ReadSliceStr(int len)
    {
        var ret = Remaining[..len];
        Remaining = Remaining[len..];

        return ret;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Res<T> Read<T>(IFormatProvider? format = null) where T : ISpanParsable<T>
        => ReadSlice<T>(format, Remaining.Length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryRead<T>(out T parsed, IFormatProvider? format = null) where T : ISpanParsable<T>
        => Read<T>(format).TryUnpack(out parsed);
    
    /// <summary>
    /// Reads the remaining span to completion, returning that span.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<char> ReadStr()
        => ReadSliceStr(Remaining.Length);

    public Res<T> ReadUntil<T>(char until, IFormatProvider? format = null) where T : ISpanParsable<T>
    {
        var rem = Remaining;
        var len = rem.IndexOf(until);
        if (len == -1)
            return ReadSlice<T>(format, rem.Length);
        
        var ret = ReadSlice<T>(format, len);
        Remaining = Remaining[1..];

        return ret;
    }
    
    public ReadOnlySpan<char> ReadStrUntil(char until)
    {
        var rem = Remaining;
        var len = rem.IndexOf(until);
        if (len == -1)
            return ReadSliceStr(rem.Length);
        
        var ret = ReadSliceStr(len);
        Remaining = Remaining[1..];

        return ret;
    }

    /// <summary>
    /// Returns a new parser, which contains a slice of the span from the current location to the location of the next <paramref name="until"/> character.
    /// </summary>
    public ParserRes SliceUntil(char until)
    {
        var rem = Remaining;
        if (rem.Length == 0)
            return ParserRes.Error();
        
        var len = rem.IndexOf(until);
        if (len < 0)
        {
            // read until the end
            Remaining = ReadOnlySpan<char>.Empty;
            return ParserRes.Ok(new(rem));
        }
        
        Remaining = rem[(len+1)..]; // +1 to skip past the 'until' character
        return ParserRes.Ok(new(rem[..len]));
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool StartsWith(ReadOnlySpan<char> prefix)
        => Remaining.StartsWith(prefix);

    public void TrimStart()
    {
        Remaining = Remaining.TrimStart();
    }
}

internal readonly struct Res<T>(T val, bool success)
{
    public bool IsSuccess => success;
    
    private T ValOrDef => val;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Or(T def) => success ? val : def;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryUnpack(out T ret)
    {
        ret = ValOrDef;
        return success;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator T(Res<T> res) => res.IsSuccess ? res.ValOrDef : throw new SpanParseFailException();
}

// ': allow ref' when :(
internal readonly ref struct RosRes
{
    public RosRes(ReadOnlySpan<char> val, bool success)
    {
        ValOrDef = val;
        IsSuccess = success;
    }
    
    public bool IsSuccess { get; }
    
    private ReadOnlySpan<char> ValOrDef { get; }
    
    
    public ReadOnlySpan<char> Or(ReadOnlySpan<char> def) => IsSuccess ? ValOrDef : def;

    public static implicit operator ReadOnlySpan<char>(RosRes res) => res.IsSuccess ? res.ValOrDef : throw new SpanParseFailException();
}

// ': allow ref' when :(
internal readonly ref struct ParserRes
{
    public ParserRes(SpanParser val, bool success)
    {
        ValOrDef = val;
        IsSuccess = success;
    }
    
    public bool IsSuccess { get; }
    
    private SpanParser ValOrDef { get; }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SpanParser Or(SpanParser def) => IsSuccess ? ValOrDef : def;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryUnpack(out SpanParser ret)
    {
        ret = ValOrDef;
        return IsSuccess;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator SpanParser(ParserRes res) => res.IsSuccess ? res.ValOrDef : throw new SpanParseFailException();
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ParserRes Error() => default;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ParserRes Ok(SpanParser parser) => new ParserRes(parser, true);
}

internal class SpanParseFailException : Exception
{
    public override string Message => "Tried to get value from Res<T> when its IsSuccess property was false.";
}