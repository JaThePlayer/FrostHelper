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
    
    public Res<T> Read<T>(IFormatProvider? format = null) where T : ISpanParsable<T>
        => ReadSlice<T>(format, Remaining.Length);

    public bool TryRead<T>(out T parsed, IFormatProvider? format = null) where T : ISpanParsable<T>
        => Read<T>(format).TryUnpack(out parsed);
    
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

    public ParserRes SliceUntil(char until)
    {
        var rem = Remaining;
        if (rem.Length == 0)
        {
            return new(new(), false);
        }
        
        var len = rem.IndexOf(until);
        ParserRes ret;
        
        if (len == -1)
        {
            ret = new ParserRes(new(Remaining), true);
            Remaining = Remaining[..0];
        }
        else
        {
            ret = new ParserRes(new(Remaining[..len]), true);
            Remaining = Remaining[(len+1)..]; // +1 to skip past the 'until' character
        }
        

        return ret;
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
    
    public T Or(T def) => success ? val : def;

    public bool TryUnpack(out T ret)
    {
        ret = ValOrDef;
        return success;
    }

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
    
    
    public SpanParser Or(SpanParser def) => IsSuccess ? ValOrDef : def;
    
    public bool TryUnpack(out SpanParser ret)
    {
        ret = ValOrDef;
        return IsSuccess;
    }
    
    public static implicit operator SpanParser(ParserRes res) => res.IsSuccess ? res.ValOrDef : throw new SpanParseFailException();
}

internal class SpanParseFailException : Exception
{
    public override string Message => "Tried to get value from Res<T> when its IsSuccess property was false.";
}