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
    private static readonly MethodInfo MethodGetArgument =
        typeof(LambdaArgumentCondition).GetMethod(nameof(GetArgument), BindingFlags.NonPublic | BindingFlags.Instance)!;

    private object GetArgument() {
        return lambdaDefinition.Instance?.GetArgument(index) ?? Zero;
    }
    
    public override object Get(Session session, object? userdata) {
        return GetArgument();
    }

    internal override void Emit(ConditionCompilationCtx ctx, Type targetType) {
        ctx.EmitLoadCurrentCondition<LambdaArgumentCondition>();
        ctx.Il.Emit(OpCodes.Call, MethodGetArgument);
        if (ReturnType.IsValueType) {
            ctx.Il.Emit(OpCodes.Unbox_Any, ReturnType);
        }
        ctx.EmitConvertTo(ReturnType, targetType);
    }

    protected internal override Type ReturnType => lambdaDefinition.GetArgumentType(index);
}


internal sealed class LambdaDefinitionCondition(IList<string> argumentNames) : ConditionHelper.Condition {
    internal ConditionHelper.Condition Code { get; set; }
    
    internal static readonly MethodInfo MethodCodeGet =
        typeof(LambdaDefinitionCondition).GetProperty(nameof(Code), BindingFlags.NonPublic | BindingFlags.Instance)!.GetMethod!;

    public IList<string> ArgumentNames => argumentNames;

    public IList<Type?> ArgumentTypes { get; } = new Type[argumentNames.Count];

    public LambdaCondition Instance => field ??= new LambdaCondition(this);

    private static readonly MethodInfo MethodInstanceGet =
        typeof(LambdaDefinitionCondition).GetProperty(nameof(Instance))!.GetMethod!;
    
    public override object Get(Session session, object? userdata) {
        return Instance;
    }

    internal override void Emit(ConditionCompilationCtx ctx, Type targetType) {
        ctx.EmitLoadCurrentCondition<LambdaDefinitionCondition>();
        ctx.Il.Emit(OpCodes.Call, MethodInstanceGet);
        ctx.EmitConvertTo(typeof(LambdaCondition), targetType);
    }

    internal override bool UsesCurrentConditionLocalInEmit => true;

    protected internal override Type ReturnType => typeof(LambdaCondition);

    public Type GetArgumentType(int index) {
        return ArgumentTypes[index] ?? typeof(object);
    }
}

internal sealed class LambdaCondition(LambdaDefinitionCondition definition) : ConditionHelper.Condition {
    private readonly object[] _args = new object[definition.ArgumentNames.Count];
    private readonly LambdaDefinitionCondition _definition = definition;
    private static readonly FieldInfo FieldDefinition = typeof(LambdaCondition).GetField(nameof(_definition), BindingFlags.Instance | BindingFlags.NonPublic)!;

    public int ArgumentCount => _args.Length;
    
    public object GetArgument(int index) => _args[index];

    public void SetArgument(int index, object value) {
        if (index < 0 || index >= _args.Length) return;
        _args[index] = value;
    }

    public override object Get(Session session, object? userdata) {
        return _definition.Code.Get(session, userdata);
    }

    private static readonly MethodInfo MethodSetArgument = typeof(LambdaCondition).GetMethod(nameof(SetArgument))!;
    
    public void EmitSetArgument(ConditionCompilationCtx ctx) {
        ctx.Il.Emit(OpCodes.Callvirt, MethodSetArgument);
    }

    internal override void Emit(ConditionCompilationCtx ctx, Type targetType) {
        LocalBuilder? temp = null;
        ctx.EmitSwapOutCurrentCondition(ref temp, _definition.Code, () => {
            // CurrentField is this
            ctx.EmitLoadCurrentCondition<LambdaCondition>();
            ctx.Il.Emit(OpCodes.Ldfld, FieldDefinition);
            ctx.Il.Emit(OpCodes.Call, LambdaDefinitionCondition.MethodCodeGet);
        });
        _definition.Code.Emit(ctx, targetType);
        
        ctx.EmitRevertCurrentCondition(temp);
    }

    internal override bool UsesCurrentConditionLocalInEmit
        => _definition.Code.UsesCurrentConditionLocalInEmit;
}