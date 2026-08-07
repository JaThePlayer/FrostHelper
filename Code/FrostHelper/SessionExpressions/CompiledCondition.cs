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

internal sealed class CompiledCondition<T> : ISavestatePersisted, IDisposable {
    private static readonly ConditionalWeakTable<ConditionHelper.Condition, CompiledCondition<T>> Cache = new();
    private static int _compiledAmt;

    public static CompiledCondition<T> GetFor(ConditionHelper.Condition condition) {
        return Cache.GetValue(condition, static c => new CompiledCondition<T>(c));
    }
    
    private CompiledCondition(ConditionHelper.Condition basedOn) {
        _basedOn = basedOn;
    }

    private readonly ConditionHelper.Condition _basedOn;

    private Func<Session, object?, ConditionHelper.Condition, T>? _compiled;

    private bool _attemptedToCompile;
    
    internal DynamicMethodDefinition? CompiledMethod { get; private set; }
    
    public T Get(Session session, object? userdata) {
        if (!_attemptedToCompile) {
            _attemptedToCompile = true;
            _compiled = Jit();
        }

        return _compiled is null
            ? _basedOn.Get<T>(session, userdata)
            : _compiled(session, userdata, _basedOn);
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

        if (_basedOn.UsesCurrentConditionLocalInEmit) {
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Stloc, ctx.CurrentCondition);
        }

        try {
            _basedOn.Emit(ctx, typeof(T));
        } catch (Exception ex) {
            Logger.Error("FrostHelper.CompiledCondition", $"Failed to compile session expression '{_basedOn.SourceText}', falling back to interpreter: {ex}");
            return null;
        }
        
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
