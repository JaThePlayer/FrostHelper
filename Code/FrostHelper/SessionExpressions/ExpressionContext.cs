using FrostHelper.Helpers;
using System.Diagnostics.CodeAnalysis;

namespace FrostHelper.SessionExpressions;

public interface IExpressionContext {
    public bool TryGetSimpleCommand(string name, [NotNullWhen(true)] out ConditionHelper.Condition? command);
    
    public bool TryGetFunctionCommand(string name, [NotNullWhen(true)] out FunctionCommandFactory? factory);
}

/// <summary>
/// Context that can be used when parsing session expressions, allowing you to provide additional commands and functions
/// </summary>
public sealed class ExpressionContext(
    Dictionary<string, ConditionHelper.Condition> simpleCommands,
    Dictionary<string, FunctionCommandFactory> functions) : IExpressionContext {
    public IReadOnlyDictionary<string, ConditionHelper.Condition> SimpleCommands => simpleCommands;
    
    public IReadOnlyDictionary<string, FunctionCommandFactory> FunctionCommands => functions;

    public static ExpressionContext Default { get; } = new([], []);

    public ExpressionContext CloneWith(Dictionary<string, ConditionHelper.Condition> simpleCommands,
        Dictionary<string, FunctionCommandFactory> functions) {
        var newCommands = new Dictionary<string, ConditionHelper.Condition>(SimpleCommands);
        var newFunctions = new Dictionary<string, FunctionCommandFactory>(FunctionCommands);
        
        newCommands.AddRange(simpleCommands);
        newFunctions.AddRange(functions);
        
        return new ExpressionContext(newCommands, newFunctions);
    }

    public bool TryGetSimpleCommand(string name, [NotNullWhen(true)] out ConditionHelper.Condition? command) {
        return SimpleCommands.TryGetValue(name, out command);
    }

    public bool TryGetFunctionCommand(string name, [NotNullWhen(true)] out FunctionCommandFactory? factory) {
        return FunctionCommands.TryGetValue(name, out factory);
    }
}

internal sealed class LambdaContext(IExpressionContext source, LambdaDefinitionCondition lambdaDefinition) : IExpressionContext {
    public bool TryGetSimpleCommand(string name, [NotNullWhen(true)] out ConditionHelper.Condition? command) {
        var argIdx = lambdaDefinition.ArgumentNames.IndexOf(name);
        if (argIdx < 0) {
            return source.TryGetSimpleCommand(name, out command);
        }

        command = new LambdaArgumentCondition(lambdaDefinition, argIdx);
        return true;
    }

    public bool TryGetFunctionCommand(string name, [NotNullWhen(true)] out FunctionCommandFactory? factory) {
        return source.TryGetFunctionCommand(name, out factory);
    }
}

internal sealed class LambdaArgumentCondition(LambdaDefinitionCondition lambdaDefinition, int index) : ConditionHelper.Condition {
    public override object Get(Session session, object? userdata) {
        return lambdaDefinition.Instance?.GetArgument(index) ?? Zero;
    }
}


internal sealed class LambdaDefinitionCondition(IList<string> argumentNames) : ConditionHelper.Condition {
    internal ConditionHelper.Condition Code { get; set; }

    public IList<string> ArgumentNames => argumentNames;
    
    public LambdaCondition? Instance { get; private set; }
    
    public override object Get(Session session, object? userdata) {
        return Instance ??= new LambdaCondition(this);
    }
}

internal sealed class LambdaCondition(LambdaDefinitionCondition definition) : ConditionHelper.Condition {
    private readonly object[] _args = new object[definition.ArgumentNames.Count];

    public int ArgumentCount => _args.Length;
    
    public object GetArgument(int index) => _args[index];

    public void SetArgument(int index, object value) {
        if (index < 0 || index >= _args.Length) return;
        _args[index] = value;
    }

    public override object Get(Session session, object? userdata) {
        return definition.Code.Get(session, userdata);
    }
}