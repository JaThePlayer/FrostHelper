using FrostHelper.Helpers;
using System.Reflection.Emit;
using System.Threading;
using OpCodes = System.Reflection.Emit.OpCodes;

namespace FrostHelper.SessionExpressions;

internal sealed class ConditionCompilationCtx {
    public int SessionArgId => 0;
    public int UserdataArgId => 1;
    public required LocalBuilder CurrentCondition { get; set; }
    
    public required ILGenerator Il { get; init; }

    public void EmitLoadSession() {
        Il.Emit(OpCodes.Ldarg, SessionArgId);
    }
    
    public void EmitLoadCurrentCondition<T>() {
        Il.Emit(OpCodes.Ldloc, CurrentCondition);
        Il.Emit(OpCodes.Castclass, typeof(T));
    }
    
    public void EmitLoadUserdata<T>() {
        Il.Emit(OpCodes.Ldarg, UserdataArgId);
        Il.Emit(OpCodes.Castclass, typeof(T));
    }

    public void EmitConvertTo(Type fromType, Type toType) {
        Il.EmitConvertToInSessionExpression(fromType, toType);
    }
}

public class CompiledSessionExpression<T>(ConditionHelper.Condition basedOn) {
    private static int _compiledAmt;

    private Func<Session, object?, ConditionHelper.Condition, T>? _compiled;

    internal DynamicMethodDefinition? CompiledMethod { get; private set; }
    
    public T Get(Session session, object? userdata) {
        _compiled ??= Jit();
        
        return _compiled(session, userdata, basedOn);
    }

    internal Func<Session, object?, ConditionHelper.Condition, T> Jit() {
        DynamicMethodDefinition method = new DynamicMethodDefinition(
            $"FrostHelper.<CompiledSessionExpression.{Interlocked.Increment(ref _compiledAmt)}>",
            typeof(T),
            [ typeof(Session), typeof(object), typeof(ConditionHelper.Condition) ]);
        
        var il = method.GetILGenerator();

        var ctx = new ConditionCompilationCtx {
            CurrentCondition = il.DeclareLocal(typeof(ConditionHelper.Condition)),
            Il = il,
        };

        if (basedOn.UsesCurrentConditionLocalInEmit) {
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Stloc, ctx.CurrentCondition);
        }
        
        basedOn.Emit(ctx, typeof(T));
        il.Emit(OpCodes.Ret);

        _compiled = method.Generate().CreateDelegate<Func<Session, object?, ConditionHelper.Condition, T>>();
        CompiledMethod = method;
        
        return _compiled;
    }
}
