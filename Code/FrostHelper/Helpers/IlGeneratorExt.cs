using FrostHelper.SessionExpressions;
using System.Globalization;
using System.Numerics;
using System.Reflection.Emit;
using OpCodes = System.Reflection.Emit.OpCodes;

namespace FrostHelper.Helpers;

internal static class IlGeneratorExt {
    private static readonly MethodInfo MethodCalcHexToColorInt = typeof(Calc).GetMethod(nameof(Calc.HexToColor), [ typeof(int) ])!;
    private static readonly MethodInfo MethodColorHelperGetColor = typeof(ColorHelper).GetMethod(nameof(ColorHelper.GetColor), [ typeof(string) ])!;
    
    extension(ILGenerator il) {
        public void EmitLoadConstAs(object value, Type targetType) {
            object? changedType;
            try {
                changedType = Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
            } catch (InvalidCastException) {
                // Not directly castable, we'll instead do the cast at runtime.
                il.EmitLoadConstAs(value, value.GetType());
                il.EmitConvertToInSessionExpression(value.GetType(), targetType);
                return;
            }

            switch (changedType) {
                case int i:
                    il.Emit(OpCodes.Ldc_I4, i);
                    break;
                case float f:
                    il.Emit(OpCodes.Ldc_R4, f);
                    break;
                case long i:
                    il.Emit(OpCodes.Ldc_I8, i);
                    break;
                case double f:
                    il.Emit(OpCodes.Ldc_R8, f);
                    break;
                case string s:
                    il.Emit(OpCodes.Ldstr, s);
                    break;
                default:
                    throw new Exception($"Cannot convert {value.GetType()} to {targetType}");
            }

            if (targetType == typeof(object) && changedType.GetType().IsValueType) {
                il.Emit(OpCodes.Box, changedType.GetType());
            }
        }

        public void EmitConvertToInSessionExpression(Type fromType, Type toType) {
            if (toType == fromType)
                return;
            
            if (fromType == typeof(bool)) {
                // In session expressions, booleans get coerced to 1/0.
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Ceq);
                fromType = typeof(int);
            }
            
            if (toType == fromType)
                return;

            if (toType == typeof(object)) {
                if (fromType.IsValueType) {
                    il.Emit(OpCodes.Box, fromType);
                }
                return;
            }
            
            if (fromType == typeof(bool)) {
                if (toType == typeof(int)) {
                    var label = il.DefineLabel();
                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(OpCodes.Brfalse, label);
                    il.Emit(OpCodes.Ldc_I4_1);
                    il.MarkLabel(label);
                    return;
                }
            }

            if (fromType == typeof(int)) {
                if (toType == typeof(float)) {
                    il.Emit(OpCodes.Conv_R4);
                    return;
                }
            }
            
            if (fromType == typeof(float)) {
                if (toType == typeof(int)) {
                    il.Emit(OpCodes.Conv_I4);
                    return;
                }
            }

            if (fromType == typeof(object)) {
                if (toType == typeof(bool)) {
                    il.Emit(OpCodes.Call, typeof(ConditionHelper.Condition).GetMethod(nameof(ConditionHelper.Condition.CoerceToBool))!);
                    return;
                }

                if (toType == typeof(float) || toType == typeof(int)) {
                    il.Emit(OpCodes.Call, typeof(ConditionHelper.Condition).GetMethod(nameof(ConditionHelper.Condition.CoerceToNumber))!.MakeGenericMethod(toType));
                    return;
                }
            }

            if (toType == typeof(bool)) {
                if (fromType == typeof(int)) {
                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(OpCodes.Cgt_Un);
                    return;
                }
                
                if (fromType == typeof(float)) {
                    il.Emit(OpCodes.Ldc_R4, 0f);
                    il.Emit(OpCodes.Ceq);
                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(OpCodes.Ceq);
                    return;
                }
            }

            if (toType == typeof(Color)) {
                if (fromType == typeof(int)) {
                    il.Emit(OpCodes.Call, MethodCalcHexToColorInt);
                    return;
                }
                
                if (fromType == typeof(float)) {
                    il.Emit(OpCodes.Conv_I4);
                    il.Emit(OpCodes.Call, MethodCalcHexToColorInt);
                    return;
                }
                
                if (fromType == typeof(string)) {
                    il.Emit(OpCodes.Call, MethodColorHelperGetColor);
                    return;
                }
            }

            if (fromType.IsAssignableTo(toType))
                return;
            
            throw new Exception($"Cannot convert {fromType} to {toType}");
        }

        public void EmitSwapOutCurrentCondition(ref LocalBuilder? tempOrigCond, ConditionCompilationCtx ctx, ConditionHelper.Condition next, FieldInfo fieldContainingNext) {
            if (!next.UsesCurrentConditionLocalInEmit)
                return;

            if (tempOrigCond is null) {
                tempOrigCond = il.DeclareLocal(typeof(ConditionHelper.Condition));
                il.Emit(OpCodes.Ldloc, ctx.CurrentCondition);
                il.Emit(OpCodes.Stloc, tempOrigCond);
            }
                
            il.Emit(OpCodes.Ldloc, tempOrigCond);
            il.Emit(OpCodes.Castclass, fieldContainingNext.DeclaringType!);
            il.Emit(OpCodes.Ldfld, fieldContainingNext);
            il.Emit(OpCodes.Stloc, ctx.CurrentCondition);
        }
        
        public void EmitSwapOutCurrentCondition(ref LocalBuilder? tempOrigCond, ConditionCompilationCtx ctx, ConditionHelper.Condition next, Action emitLoadCondition) {
            if (!next.UsesCurrentConditionLocalInEmit)
                return;

            if (tempOrigCond is null) {
                tempOrigCond = il.DeclareLocal(typeof(ConditionHelper.Condition));
                il.Emit(OpCodes.Ldloc, ctx.CurrentCondition);
                il.Emit(OpCodes.Stloc, tempOrigCond);
            }

            emitLoadCondition();
            il.Emit(OpCodes.Stloc, ctx.CurrentCondition);
        }

        public void EmitRevertCurrentCondition(LocalBuilder? tempOrigCond, ConditionCompilationCtx ctx) {
            if (tempOrigCond is not null) {
                il.Emit(OpCodes.Ldloc, tempOrigCond);
                il.Emit(OpCodes.Stloc, ctx.CurrentCondition);
            }
        }

        public void EmitLdlocOrLdloca(LocalBuilder local) {
            if (local.LocalType.IsValueType) {
                il.Emit(OpCodes.Ldloca, local);
                return;
            }
            
            il.Emit(OpCodes.Ldloc, local);
        }
    }
}