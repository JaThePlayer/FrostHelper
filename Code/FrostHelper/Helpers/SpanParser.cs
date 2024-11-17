using System.Globalization;
using System.Runtime.CompilerServices;

namespace FrostHelper.Helpers;

internal ref struct SpanParser(ReadOnlySpan<char> input)
{
    private ReadOnlySpan<char> _remaining = input;
    
    public ReadOnlySpan<char> Remaining => _remaining;
    
    public bool IsEmpty => _remaining.IsEmpty;

    private Res<T> ReadSlice<T>(IFormatProvider? format, int len) where T : ISpanParsable<T>
    {
        var success = T.TryParse(_remaining[..len], format ?? CultureInfo.InvariantCulture, out var ret);

        //Remaining = Remaining.Length >= len ? Remaining[len..] : Remaining[(len + 1)..];
        _remaining = _remaining[len..];
        
        if (!success)
        {
            return new(default!, false);
        }

        return new Res<T>(ret!, true);
    }
    
    private ReadOnlySpan<char> ReadSliceStr(int len)
    {
        var ret = _remaining[..len];
        _remaining = _remaining[len..];

        return ret;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Res<T> Read<T>(IFormatProvider? format = null) where T : ISpanParsable<T>
        => ReadSlice<T>(format, _remaining.Length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryRead<T>(out T parsed, IFormatProvider? format = null) where T : ISpanParsable<T>
        => Read<T>(format).TryUnpack(out parsed);
    
    /// <summary>
    /// Reads the remaining span to completion, returning that span.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<char> ReadStr()
        => ReadSliceStr(_remaining.Length);

    public Res<T> ReadUntil<T>(char until, IFormatProvider? format = null) where T : ISpanParsable<T>
    {
        var rem = _remaining;
        var len = rem.IndexOf(until);
        if (len == -1)
            return ReadSlice<T>(format, rem.Length);
        
        var ret = ReadSlice<T>(format, len);
        _remaining = _remaining[1..];

        return ret;
    }
    
    public ReadOnlySpan<char> ReadStrUntil(char until)
    {
        var rem = _remaining;
        var len = rem.IndexOf(until);
        if (len == -1)
            return ReadSliceStr(rem.Length);
        
        var ret = ReadSliceStr(len);
        _remaining = _remaining[1..];

        return ret;
    }
    
    public ReadOnlySpan<char> ReadStrUntilAny(ReadOnlySpan<char> until)
    {
        var rem = _remaining;
        var len = rem.IndexOfAny(until);
        if (len == -1)
            return ReadSliceStr(rem.Length);
        
        var ret = ReadSliceStr(len);
        _remaining = _remaining[1..];

        return ret;
    }

    /// <summary>
    /// Returns a new parser, which contains a slice of the span from the current location to the location of the next <paramref name="until"/> character.
    /// </summary>
    public ParserRes SliceUntil(char until)
    {
        var rem = _remaining;
        if (rem.Length == 0)
            return ParserRes.Error();
        
        var len = rem.IndexOf(until);
        if (len < 0)
        {
            // read until the end
            _remaining = ReadOnlySpan<char>.Empty;
            return ParserRes.Ok(new(rem));
        }
        
        _remaining = rem[(len+1)..]; // +1 to skip past the 'until' character
        return ParserRes.Ok(new(rem[..len]));
    }
    
    /// <summary>
    /// Returns a new parser, which contains a slice of the span from the current location to the location of the next <paramref name="until"/> character.
    /// </summary>
    public ParserRes SliceUntilAny(ReadOnlySpan<char> until, out char splitChar) {
        splitChar = '\0';
        var rem = _remaining;
        if (rem.Length == 0)
            return ParserRes.Error();
        
        var len = rem.IndexOfAny(until);
        if (len < 0)
        {
            // read until the end
            _remaining = ReadOnlySpan<char>.Empty;
            return ParserRes.Ok(new(rem));
        }

        splitChar = rem[len];
        _remaining = rem[(len+1)..]; // +1 to skip past the 'until' character
        return ParserRes.Ok(new(rem[..len]));
    }
    
    public ParserRes SliceUntilAnyOutsideBrackets(ReadOnlySpan<char> until, out char splitChar, int skipFirst = 0) {
        splitChar = '\0';
        var rem = _remaining[skipFirst..];
        if (rem.Length == 0)
            return ParserRes.Error();

        var untilWithBrackets = $"{until}()";
        var bracketDepth = 0;

        var skipped = 0;
        
        while (true)
        {
            var len = rem.IndexOfAny(untilWithBrackets);
            if (len < 0)
            {
                if (bracketDepth == 0) {
                    // read until the end
                    var result = _remaining;
                    _remaining = ReadOnlySpan<char>.Empty;
                    return ParserRes.Ok(new(result));
                }
                
                return ParserRes.Error();
            }

            if (rem[len] == '(')
            {
                bracketDepth++;
            } else if (rem[len] == ')')
            {
                bracketDepth--;
            }
            else
            {
                if (bracketDepth <= 0)
                {
                    splitChar = rem[len];

                    var result = _remaining[..(len+skipped)];
        
                    _remaining = rem[(len+1)..]; // +1 to skip past the 'until' character
                    return ParserRes.Ok(new(result));
                }
            }

            skipped += len + 1;
            rem = rem[(len + 1)..];
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool StartsWith(ReadOnlySpan<char> prefix)
        => _remaining.StartsWith(prefix);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool EndsWith(ReadOnlySpan<char> prefix)
        => _remaining.EndsWith(prefix);

    public void TrimStart()
    {
        _remaining = _remaining.TrimStart();
    }
    
    public void TrimEnd()
    {
        _remaining = _remaining.TrimEnd();
    }

    public void Skip(int chars) {
        _remaining = _remaining[chars..];
    }
    
    public void SkipEnd(int chars) {
        _remaining = _remaining[..^chars];
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