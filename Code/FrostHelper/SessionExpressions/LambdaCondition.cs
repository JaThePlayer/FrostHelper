using FrostHelper.Helpers;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Emit;
using OpCodes = System.Reflection.Emit.OpCodes;

namespace FrostHelper.SessionExpressions;

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

    private static readonly MethodInfo MethodInstanceGet =
        typeof(LambdaDefinitionCondition).GetProperty(nameof(Instance))!.GetMethod!;
    
    public override object Get(Session session, object? userdata) {
        return Instance ??= new LambdaCondition(this);
    }

    internal override void Emit(ConditionCompilationCtx ctx, Type targetType) {
        Instance ??= new LambdaCondition(this);
        
        ctx.EmitLoadCurrentCondition<LambdaDefinitionCondition>();
        ctx.Il.Emit(OpCodes.Call, MethodInstanceGet);
        ctx.EmitConvertTo(typeof(LambdaCondition), targetType);
    }

    internal override bool UsesCurrentConditionLocalInEmit => true;

    protected internal override Type ReturnType => typeof(LambdaCondition);
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