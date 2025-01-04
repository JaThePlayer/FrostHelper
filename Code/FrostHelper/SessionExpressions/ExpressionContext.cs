using FrostHelper.Helpers;

namespace FrostHelper.SessionExpressions;

/// <summary>
/// Context that can be used when parsing session expressions, allowing you to provide additional commands and functions
/// </summary>
public sealed class ExpressionContext(
    Dictionary<string, ConditionHelper.Condition> simpleCommands,
    Dictionary<string, FunctionCommandFactory> functions) {
    public IReadOnlyDictionary<string, ConditionHelper.Condition> SimpleCommands => simpleCommands;
    
    public IReadOnlyDictionary<string, FunctionCommandFactory> FunctionCommands => functions;

    public static ExpressionContext Default { get; } = new([], []);
}