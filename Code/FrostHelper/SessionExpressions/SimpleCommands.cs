using Celeste.Mod.Core;
using FrostHelper.Helpers;
using static FrostHelper.Helpers.ConditionHelper;
using OpCodes = System.Reflection.Emit.OpCodes;

namespace FrostHelper.SessionExpressions;

internal static class SimpleCommands {
    /// <summary>
    /// Simple commands accessible via $cmdname
    /// </summary>
    internal static readonly Dictionary<string, Condition> Registry = new() {
        ["deathsHere"] = new SessionFieldAccessor(nameof(Session.DeathsInCurrentLevel)),
        ["deaths"] = new SessionFieldAccessor(nameof(Session.Deaths)),
        ["hasGolden"] = new SessionFieldAccessor(nameof(Session.GrabbedGolden)),
        ["restartedFromGolden"] = new SessionFieldAccessor(nameof(Session.RestartedFromGolden)),
        ["coreMode"] = new CoreModeAccessor(),
        ["photosensitive"] = new SettingsFieldAccessor(nameof(Settings.DisableFlashes)),
        ["allowLightning"] = new CoreModuleSettingsPropertyAccessor(nameof(CoreModuleSettings.AllowLightning)),
        ["allowScreenFlash"] = new CoreModuleSettingsPropertyAccessor(nameof(CoreModuleSettings.AllowScreenFlash)),
        ["allowGlitch"] = new CoreModuleSettingsPropertyAccessor(nameof(CoreModuleSettings.AllowGlitch)),
        ["allowDistort"] = new CoreModuleSettingsPropertyAccessor(nameof(CoreModuleSettings.AllowDistort)),
        ["allowTextHighlight"] = new CoreModuleSettingsPropertyAccessor(nameof(CoreModuleSettings.AllowTextHighlight)),
        ["dashes"] = new DashAccessor(),
        ["maxDashes"] = new MaxDashAccessor(),
        ["stamina"] = new StaminaAccessor(),
        ["player"] = new PlayerAccessor(),
        ["speed"] = new PlayerSpeedAccessor(),
        ["pi"] = new ConstFloat(float.Pi),
        ["e"] = new ConstFloat(float.E),
        ["dtime"] = new DeltaTimeAccessor(),
        ["time"] = new TimeAccessor(),
        ["roomName"] = new SessionFieldAccessor(nameof(Session.Level)),
    };

    // Exposed via API
    internal static void RegisterSimpleCommand(string modName, string cmdName, Func<Session, object?, object> func) {
        var key = $"{modName}.{cmdName}";
        if (Registry.TryGetValue(key, out var existing)) {
            Logger.Warn("FrostHelper.ConditionHelper", $"Replacing simple command '${key}'");
        }

        Registry[key] = new ModApiSimpleCommand(func);
    }

    internal static Condition CreateCommandFromModFunc(Func<Session, object?, object> func) {
        return new ModApiSimpleCommand(func);
    }

    private sealed class ModApiSimpleCommand(Func<Session, object?, object> func) : Condition {
        public override object Get(Session session, object? userdata) {
            var ret = func(session, userdata);
            if (ret is bool b)
                return BoolToBoxedInt(b);
            return ret;
        }

        protected override IEnumerable<object> GetArgsForDebugPrint() => [func];
    }
    
    private sealed class DeltaTimeAccessor : Condition {
        public override object Get(Session session, object? userdata) => Engine.DeltaTime;

        protected internal override Type ReturnType => typeof(float);
        
        internal override void Emit(ConditionCompilationCtx ctx, Type targetType) {
            var il = ctx.Il;
            il.Emit(OpCodes.Call, typeof(Engine).GetProperty("DeltaTime")!.GetMethod!);
            il.EmitConvertToInSessionExpression(typeof(float), targetType);
        }

        internal override bool UsesCurrentConditionLocalInEmit => false;
    }
    
    private sealed class TimeAccessor : Condition {
        public override object Get(Session session, object? userdata) => Engine.Scene.TimeActive;

        protected internal override Type ReturnType => typeof(float);

        internal override void Emit(ConditionCompilationCtx ctx, Type targetType) {
            var il = ctx.Il;
            ctx.EmitLoadScene();
            il.Emit(OpCodes.Ldfld, typeof(Scene).GetField("TimeActive")!);
            il.EmitConvertToInSessionExpression(typeof(float), targetType);
        }

        internal override bool UsesCurrentConditionLocalInEmit => false;
    }

    private sealed class SessionFieldAccessor(string fieldName) : Condition {
        public readonly FieldInfo Field = typeof(Session).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public)!;
        
        public override object Get(Session session, object? userdata) {
            var ret = Field.GetValue(session)!;
            return ret is bool b ? BoolToBoxedInt(b) : ret;
        }

        internal override void Emit(ConditionCompilationCtx ctx, Type targetType) {
            ctx.EmitLoadSession();
            ctx.Il.Emit(OpCodes.Ldfld, Field);
            ctx.EmitConvertTo(ReturnType, targetType);
        }

        internal override bool UsesCurrentConditionLocalInEmit => false;

        protected internal override Type ReturnType => Field.FieldType;
    }

    private sealed class SettingsFieldAccessor(string fieldName) : Condition {
        public readonly FieldInfo Field = typeof(Settings).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public)!;
        
        public override object Get(Session session, object? userdata) {
            var ret = Field.GetValue(Settings.Instance)!;
            return ret is bool b ? BoolToBoxedInt(b) : ret;
        }

        internal override void Emit(ConditionCompilationCtx ctx, Type targetType) {
            ctx.EmitLoadSettings();
            ctx.Il.Emit(OpCodes.Ldfld, Field);
            ctx.EmitConvertTo(ReturnType, targetType);
        }

        internal override bool UsesCurrentConditionLocalInEmit => false;

        protected internal override Type ReturnType => Field.FieldType;
    }
    
    private sealed class CoreModuleSettingsPropertyAccessor(string fieldName) : Condition {
        public readonly PropertyInfo Property = typeof(CoreModuleSettings).GetProperty(fieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public)!;
        
        public override object Get(Session session, object? userdata) {
            var ret = Property.GetValue(CoreModule.Settings)!;
            return ret is bool b ? BoolToBoxedInt(b) : ret;
        }

        internal override void Emit(ConditionCompilationCtx ctx, Type targetType) {
            ctx.EmitLoadCoreModuleSettings();
            ctx.Il.Emit(OpCodes.Call, Property.GetMethod!);
            ctx.EmitConvertTo(ReturnType, targetType);
        }

        internal override bool UsesCurrentConditionLocalInEmit => false;

        protected internal override Type ReturnType => Property.PropertyType;
    }
    
    private sealed class CoreModeAccessor : Condition {
        public override object Get(Session session, object? userdata) {
            return (int) session.CoreMode;
        }

        public override bool OnlyChecksFlags() => false;

        protected internal override Type ReturnType => typeof(int);
    }

    private sealed class DashAccessor : PlayerGetterCondition<int> {
        protected override int GetFromPlayer(Player player) => player.Dashes;
    }

    private sealed class MaxDashAccessor : PlayerGetterCondition<int> {
        protected override int GetFromPlayer(Player player) => player.MaxDashes;
    }

    private sealed class PlayerAccessor : PlayerGetterCondition<Player> {
        protected override Player GetFromPlayer(Player player) => player;
    }
    
    private sealed class PlayerSpeedAccessor : PlayerGetterCondition<Vector2> {
        protected override Vector2 GetFromPlayer(Player player) => player.Speed;
    }

    private sealed class StaminaAccessor : PlayerGetterCondition<float> {
        protected override float GetFromPlayer(Player player) => player.Stamina;
    }

    private abstract class PlayerGetterCondition<T> : Condition where T : notnull {
        private object? _lastValue;

        protected abstract T GetFromPlayer(Player player);

        public override object Get(Session session, object? userdata) {
            if (Engine.Scene.Tracker.SafeGetEntity<Player>() is { } player)
                return _lastValue = GetFromPlayer(player);

            return _lastValue ?? Zero;
        }

        public override bool OnlyChecksFlags() => false;

        protected internal override Type ReturnType => typeof(T);
    }
}