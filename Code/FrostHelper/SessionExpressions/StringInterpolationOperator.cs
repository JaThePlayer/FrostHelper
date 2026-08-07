using FrostHelper.Helpers;
using System.Reflection.Emit;
using OpCodes = System.Reflection.Emit.OpCodes;

namespace FrostHelper.SessionExpressions;

internal sealed class StringInterpolationOperator(List<ConditionHelper.Condition> args) : ConditionHelper.Condition {
    private ConditionHelper.Condition GetArg(int index) => args[index];

    private static readonly MethodInfo MethodGetArg =
        typeof(StringInterpolationOperator).GetMethod(nameof(GetArg), BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static readonly MethodInfo MethodInterpolatorHandlerAppendLiteralString
        = typeof(Interpolator.Handler).GetMethod(nameof(Interpolator.Handler.AppendLiteral),
            BindingFlags.Instance | BindingFlags.Public, [typeof(string)])!;

    private static readonly MethodInfo MethodInterpolatorHandlerAppendFormattedObject
        = typeof(Interpolator.Handler).GetMethod(nameof(Interpolator.Handler.AppendFormatted),
            BindingFlags.Instance | BindingFlags.Public, [typeof(object)])!;

    private static readonly MethodInfo MethodInterpolatorHandlerAppendFormattedT_ISpanFormattable
        = typeof(Interpolator.Handler).GetMethod(nameof(Interpolator.Handler.AppendFormatted), 1,
            BindingFlags.Instance | BindingFlags.Public, null, [Type.MakeGenericMethodParameter(0)], null)!;

    private static readonly MethodInfo MethodInterpolatorHandlerResultToString
        = typeof(Interpolator.Handler).GetMethod(nameof(Interpolator.Handler.ResultToString),
            BindingFlags.Instance | BindingFlags.Public)!;


    public override object Get(Session session, object? userdata) {
        Interpolator.Handler handler = new Interpolator.Handler(0, args.Count, Interpolator.Shared);
        foreach (var arg in args) {
            var obj = arg.Get(session, userdata);
            if (obj is string str)
                handler.AppendLiteral(str);
            else
                handler.AppendFormatted(obj);
        }

        return handler.ResultToString();
    }

    internal override void Emit(ConditionCompilationCtx ctx, Type targetType) {
        var il = ctx.Il;
        var handlerLocal = il.DeclareLocal(typeof(Interpolator.Handler));
        LocalBuilder? tempLocal = null;
        il.Emit(OpCodes.Ldloca, handlerLocal);
        il.Emit(OpCodes.Ldc_I4_0); // literal length
        il.Emit(OpCodes.Ldc_I4, args.Count); // formatted length
        il.Emit(OpCodes.Call, typeof(Interpolator).GetProperty(nameof(Interpolator.Shared))!.GetMethod!);
        il.Emit(OpCodes.Call,
            typeof(Interpolator.Handler).GetConstructor([typeof(int), typeof(int), typeof(Interpolator)])!);

        var argI = 0;
        foreach (var arg in args) {
            il.EmitSwapOutCurrentCondition(ref tempLocal, ctx, arg, () => {
                il.Emit(OpCodes.Ldc_I4, argI);
                il.Emit(OpCodes.Call, MethodGetArg);
            });

            il.Emit(OpCodes.Ldloca, handlerLocal);

            var argType = arg.ReturnType ?? typeof(object);
            arg.Emit(ctx, argType);
            if (argType == typeof(string)) {
                il.Emit(OpCodes.Call, MethodInterpolatorHandlerAppendLiteralString);
            } else if (argType.IsAssignableTo(typeof(ISpanFormattable))) {
                il.Emit(OpCodes.Call,
                    MethodInterpolatorHandlerAppendFormattedT_ISpanFormattable.MakeGenericMethod(argType));
            } else {
                if (argType.IsValueType)
                    il.Emit(OpCodes.Box, argType);
                il.Emit(OpCodes.Call, MethodInterpolatorHandlerAppendFormattedObject);
            }

            argI++;
        }

        il.EmitRevertCurrentCondition(tempLocal, ctx);

        il.Emit(OpCodes.Ldloca, handlerLocal);
        il.Emit(OpCodes.Call, MethodInterpolatorHandlerResultToString);
        il.EmitConvertToInSessionExpression(typeof(string), targetType);
    }

    internal override bool UsesCurrentConditionLocalInEmit { get; }
        = args.Any(a => a.UsesCurrentConditionLocalInEmit);

    protected internal override Type ReturnType => typeof(string);

    protected override IEnumerable<object> GetArgsForDebugPrint() => args;
}
