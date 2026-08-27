using FrostHelper.Helpers;
using System.Diagnostics.CodeAnalysis;

namespace FrostHelper.SessionExpressions;

public interface IExpressionContext {
    public bool TryGetSimpleCommand(string name, [NotNullWhen(true)] out ConditionHelper.Condition? command);
    
    public bool TryGetFunctionCommand(string name, [NotNullWhen(true)] out FunctionCommand? factory);
}

/// <summary>
/// Context that can be used when parsing session expressions, allowing you to provide additional commands and functions
/// </summary>
internal sealed class ExpressionContext(
    Dictionary<string, ConditionHelper.Condition> simpleCommands,
    Dictionary<string, FunctionCommand> functions) : IExpressionContext {
    public IReadOnlyDictionary<string, ConditionHelper.Condition> SimpleCommands => simpleCommands;
    
    public IReadOnlyDictionary<string, FunctionCommand> FunctionCommands => functions;

    public static ExpressionContext Default { get; } = new([], []);

    public ExpressionContext CloneWith(Dictionary<string, ConditionHelper.Condition> simpleCommands,
        Dictionary<string, FunctionCommand> functions) {
        var newCommands = new Dictionary<string, ConditionHelper.Condition>(SimpleCommands);
        var newFunctions = new Dictionary<string, FunctionCommand>(FunctionCommands);
        
        newCommands.AddRange(simpleCommands);
        newFunctions.AddRange(functions);
        
        return new ExpressionContext(newCommands, newFunctions);
    }

    public bool TryGetSimpleCommand(string name, [NotNullWhen(true)] out ConditionHelper.Condition? command) {
        return SimpleCommands.TryGetValue(name, out command);
    }

    public bool TryGetFunctionCommand(string name, [NotNullWhen(true)] out FunctionCommand? factory) {
        return FunctionCommands.TryGetValue(name, out factory);
    }
}