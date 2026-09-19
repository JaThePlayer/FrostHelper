using Celeste.Mod.Core;
using FrostHelper.Helpers;
using FrostHelper.ModIntegration;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;
using OpCodes = System.Reflection.Emit.OpCodes;

namespace FrostHelper.SessionExpressions;

internal sealed class ConditionCompilationCtx {
    private const int SessionArgId = 0;
    private const int UserdataArgId = 1;
    
    public required LocalBuilder CurrentCondition { get; init; }
    
    public required ILGenerator Il { get; init; }

    public void EmitLoadSession() {
        Il.Emit(OpCodes.Ldarg, SessionArgId);
    }
    
    public void EmitLoadCurrentCondition() {
        Il.Emit(OpCodes.Ldloc, CurrentCondition);
    }
    
    public void EmitLoadCurrentCondition<T>() {
        Il.Emit(OpCodes.Ldloc, CurrentCondition);
        Il.Emit(OpCodes.Castclass, typeof(T));
    }
    
    public void EmitLoadUserdata() {
        Il.Emit(OpCodes.Ldarg, UserdataArgId);
    }
    
    public void EmitLoadUserdata<T>() {
        Il.Emit(OpCodes.Ldarg, UserdataArgId);
        Il.Emit(OpCodes.Castclass, typeof(T));
    }

    public void EmitConvertTo(Type fromType, Type toType) {
        Il.EmitConvertToInSessionExpression(fromType, toType);
    }

    public void EmitSwapOutCurrentCondition(ref LocalBuilder? oldConditionTempLocal, ConditionHelper.Condition conditionToSwapTo, FieldInfo fieldStoringConditionToSwapTo ) {
        Il.EmitSwapOutCurrentCondition(ref oldConditionTempLocal, this, conditionToSwapTo, fieldStoringConditionToSwapTo);
    }
    
    public void EmitSwapOutCurrentCondition(ref LocalBuilder? oldConditionTempLocal, ConditionHelper.Condition conditionToSwapTo, Action emitLoadConditionToSwapTo) {
        Il.EmitSwapOutCurrentCondition(ref oldConditionTempLocal, this, conditionToSwapTo, emitLoadConditionToSwapTo);
    }

    public void EmitRevertCurrentCondition(LocalBuilder? oldConditionTempLocal) {
        Il.EmitRevertCurrentCondition(oldConditionTempLocal, this);
    }

    private static readonly MethodInfo MethodEngineSceneGet = typeof(Engine).GetProperty("Scene")!.GetMethod!;
    public void EmitLoadScene() {
        Il.Emit(OpCodes.Call, MethodEngineSceneGet);
    }
    
    private static readonly FieldInfo FieldSettingsInstance = typeof(Settings).GetField(nameof(Settings.Instance))!;
    public void EmitLoadSettings() {
        Il.Emit(OpCodes.Ldsfld, FieldSettingsInstance);
    }
    
    private static readonly PropertyInfo PropertyCoreModuleSettingsInstance = typeof(CoreModule).GetProperty(nameof(CoreModule.Settings))!;
    public void EmitLoadCoreModuleSettings() {
        Il.Emit(OpCodes.Call, PropertyCoreModuleSettingsInstance.GetMethod!);
    }
}

internal static class CompiledCondition {
    private static readonly Dictionary<Type, (MethodInfo GetForMethod, Type DelegateType, MethodInfo GetMethod)> Cache = [];
    
    /// <summary>
    /// Returns a Func{Session, object, returnType} that invokes a compiled session expression.
    /// </summary>
    public static Delegate GetDelegateFor(ConditionHelper.Condition condition, Type returnType) {
        lock (Cache) {
            if (!Cache.TryGetValue(returnType, out var result)) {
                var t = typeof(CompiledCondition<>).MakeGenericType(returnType);
                result.GetForMethod = t.GetMethod(nameof(CompiledCondition<>.GetFor))!;
                result.DelegateType = typeof(Func<,,>).MakeGenericType(typeof(Session), typeof(object), returnType);
                result.GetMethod = t.GetMethod(nameof(CompiledCondition<>.Get))!;
                Cache[returnType] = result;
            }

            var compiledCondition = result.GetForMethod.Invoke(null, [condition]);

            return Delegate.CreateDelegate(result.DelegateType, compiledCondition, result.GetMethod);
        }
    }
}

internal sealed class CompiledCondition<T> : ISavestatePersisted, IDisposable {
    private static readonly ConditionalWeakTable<ConditionHelper.Condition, CompiledCondition<T>> Cache = new();
    private static int _compiledAmt;

    public static CompiledCondition<T> GetFor(ConditionHelper.Condition condition) {
        return Cache.GetValue(condition, static c => new CompiledCondition<T>(c));
    }
    
    private CompiledCondition(ConditionHelper.Condition basedOn) {
        SourceCondition = basedOn;
    }

    private Func<Session, object?, ConditionHelper.Condition, T>? _compiled;

    private bool _attemptedToCompile;

    internal ConditionHelper.Condition SourceCondition { get; }

    internal DynamicMethodDefinition? CompiledMethod { get; private set; }
    
    internal Exception? CompilationException { get; private set; }
    
    public T Get(Session session, object? userdata) {
        if (!_attemptedToCompile) {
            _attemptedToCompile = true;
            try {
                _compiled = Jit();
            } catch (Exception ex) {
                CompilationException = ex;
                Logger.Error("FrostHelper.CompiledCondition", $"Failed to compile session expression '{SourceCondition.SourceText}', falling back to interpreter: {ex}");
            }
        }

        return _compiled is null
            ? SourceCondition.Get<T>(session, userdata)
            : _compiled(session, userdata, SourceCondition);
    }

    public TOther GetOther<TOther>(Session session, object? userdata) {
        var orig = Get(session, userdata);
        
        return ConditionHelper.Condition.Coerce<TOther>(orig!);
    }

    internal Func<Session, object?, ConditionHelper.Condition, T>? Jit() {
        DynamicMethodDefinition method = new DynamicMethodDefinition(
            $"FrostHelper.<CompiledCondition.{typeof(T)}.{Interlocked.Increment(ref _compiledAmt)}>",
            typeof(T),
            [ typeof(Session), typeof(object), typeof(ConditionHelper.Condition) ]);
        
        var il = method.GetILGenerator();

        var ctx = new ConditionCompilationCtx {
            CurrentCondition = il.DeclareLocal(typeof(ConditionHelper.Condition)),
            Il = il,
        };

        if (SourceCondition.UsesCurrentConditionLocalInEmit) {
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Stloc, ctx.CurrentCondition);
        }

        SourceCondition.Emit(ctx, typeof(T));
        
        il.Emit(OpCodes.Ret);

        _compiled = method.Generate().CreateDelegate<Func<Session, object?, ConditionHelper.Condition, T>>();
        CompiledMethod = method;
        
        return _compiled;
    }

    ~CompiledCondition() {
        Dispose();
    }
    
    public void Dispose() {
        _compiled = null;
        _attemptedToCompile = false;
        CompiledMethod?.Dispose();
        CompiledMethod = null;
        GC.SuppressFinalize(this);
    }
}
