using FrostHelper.ModIntegration;
using FrostHelper.SessionExpressions;
using System.Diagnostics.CodeAnalysis;

namespace FrostHelper.Helpers;

internal sealed class EffectParams : IDetailedParsable<EffectParams> {
    private readonly List<Param> _params;
    
    public static EffectParams Empty { get; }= new([]);
    
    private EffectParams(List<Param> parameters) {
        _params = parameters;
    }

    public Effect ApplyTo(Session session, Effect effect) {
        var effectParams = effect.Parameters;
        foreach (var param in _params) {
            effectParams[param.Key].SetValueDispatched(param.Value.Get(session, null));
        }

        return effect;
    }
    
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, 
        [MaybeNullWhen(false)] out EffectParams result, [NotNullWhen(false)] out string? errorMessage) {
        var parser = new SpanParser(s);
        
        // Format:
        // key=value;key2=value2;...
        if (!parser.TryParseList<Param>(';', out var parameters, out errorMessage, provider)) {
            result = null;
            return false;
        }

        result = new EffectParams(parameters);
        return true;
    }

    private struct Param : IDetailedParsable<Param> {
        public required string Key { get; init; }
        
        public required ConditionHelper.Condition Value { get; init; }
        
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, 
            out Param result, [NotNullWhen(false)] out string? errorMessage) {
            var parser = new SpanParser(s);
            
            if (!parser.TryReadStrPair('=', out var keySpan, out var valueSpan)) {
                result = default;
                errorMessage = $"Failed to parse '{s}' as an effect parameter:\nExpected a `key=value` pair!";
                return false;
            }

            if (!ConditionHelper.TryCreate(valueSpan.ToString(), ExpressionContext.Default, out var value)) {
                result = default;
                errorMessage = $"Failed to parse '{s}' as an effect parameter:\nInvalid session expression!";
                return false;
            }

            errorMessage = null;
            result = new Param { Key = keySpan.ToString(), Value = value };
            return true;
        }
    }
}