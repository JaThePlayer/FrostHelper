using Celeste.Mod.Core;
using FrostHelper.API;
using FrostHelper.Helpers;
using static FrostHelper.Helpers.ConditionHelper;
using OpCodes = System.Reflection.Emit.OpCodes;

namespace FrostHelper.SessionExpressions;

internal sealed record SimpleCommand(CommandDescriptor Descriptor, Condition Condition);

internal static class SimpleCommands {
    static SimpleCommands() {
        Registry = [];
        
        Register<int>("deathsHere",
            "Returns how many times the player has died in this room, resets after a screen transition.",
            new SessionFieldAccessor(nameof(Session.DeathsInCurrentLevel)));
        
        Register<int>("deaths",
            "Returns how many times the player has died this session.",
            new SessionFieldAccessor(nameof(Session.Deaths)));
        
        Register<bool>("hasGolden",
            "Returns whether the player is carrying a golden berry. 1 if they are, 0 otherwise.",
            new SessionFieldAccessor(nameof(Session.GrabbedGolden)));
        
        Register<bool>("restartedFromGolden",
            "Returns 1 if the current session started due to a golden death.",
            new SessionFieldAccessor(nameof(Session.RestartedFromGolden)));
        
        Register<int>("coreMode",
            "Returns the current core mode: 0 if not set, 1 if hot, 2 if cold.",
            new CoreModeAccessor());
        
        Register<bool>("photosensitive",
            "Returns whether Photosensitive Mode is enabled. 1 if it is, 0 otherwise.",
            new SettingsFieldAccessor(nameof(Settings.DisableFlashes)));
        
        Register<bool>("allowLightning",
            "Checks the corresponding Everest-extended Photosensitive Mode setting.",
            new CoreModuleSettingsPropertyAccessor(nameof(CoreModuleSettings.AllowLightning)));
        
        Register<bool>("allowScreenFlash",
            "Checks the corresponding Everest-extended Photosensitive Mode settings.",
            new CoreModuleSettingsPropertyAccessor(nameof(CoreModuleSettings.AllowScreenFlash)));
        
        Register<bool>("allowGlitch",
            "Checks the corresponding Everest-extended Photosensitive Mode settings.",
            new CoreModuleSettingsPropertyAccessor(nameof(CoreModuleSettings.AllowGlitch)));
        
        Register<bool>("allowDistort",
            "Checks the corresponding Everest-extended Photosensitive Mode settings.",
            new CoreModuleSettingsPropertyAccessor(nameof(CoreModuleSettings.AllowDistort)));
        
        Register<bool>("allowTextHighlight",
            "Checks the corresponding Everest-extended Photosensitive Mode settings.",
            new CoreModuleSettingsPropertyAccessor(nameof(CoreModuleSettings.AllowTextHighlight)));
        
        Register<int>("dashes",
            "Current player dash count.",
            new DashAccessor());
        
        Register<int>("maxDashes",
            "Maximum allowed player dash count.",
            new MaxDashAccessor());
        
        Register<float>("stamina",
            "Current player stamina.",
            new StaminaAccessor());
        
        Register<Player>("player",
            "Current player Entity instance.",
            new PlayerAccessor());
        
        Register<Vector2>("speed",
            "Player's speed.",
            new PlayerSpeedAccessor());
        
        Register<float>("pi",
            "The value of Pi.",
            new ConstFloat(float.Pi));
        
        Register<float>("e",
            "The value of Euler's Number.",
            new ConstFloat(float.E));
        
        Register<float>("dtime",
            "The delta time between frames, taking into account Assist Mode options.",
            new DeltaTimeAccessor());
        
        Register<float>("time",
            "Equivalent to Scene.TimeActive in C#.",
            new TimeAccessor());
        
        Register<string>("roomName",
            "Gets the current room's name.",
            new SessionFieldAccessor(nameof(Session.Level)));
        
        Register<IEnumerable<EntityID>>("strawberries",
            "Gets the IDs of all strawberries collected this session.",
            new SessionFieldAccessor(nameof(Session.Strawberries)));
        
        Register<IEnumerable<string>>("flags",
            "Gets the names of all currently enabled flags.",
            new SessionFieldAccessor(nameof(Session.Flags)));
    }
    
    private static void Register<TRet>(string name, string description, Condition condition)
        => Register<TRet>(name, [ RenderPart.Default(description) ], condition);

    private static void Register<TRet>(string name, IReadOnlyList<RenderPart> description, Condition condition) {
        var desc = new CommandDescriptor {
            Name = name,
            Description = description,
            DeclaringType = null,
            DeclaringMod = null,
            ReturnType = TypeDescriptor.For(typeof(TRet)),
            Arguments = [],
        };
        condition.Descriptor = desc;
        Registry[name] = new SimpleCommand(desc, condition);
    }
    
    /// <summary>
    /// Simple commands accessible via $cmdname
    /// </summary>
    internal static readonly Dictionary<string, SimpleCommand> Registry;

    // Exposed via API
    internal static void RegisterSimpleCommand(string modName, string cmdName, Func<Session, object?, object> func) {
        var key = $"{modName}.{cmdName}";
        if (Registry.TryGetValue(key, out var existing)) {
            Logger.Warn("FrostHelper.ConditionHelper", $"Replacing simple command '${key}'");
        }

        Registry[key] = new SimpleCommand(
            new CommandDescriptor {
                Name = key,
                DeclaringMod = modName,
                DeclaringType = null,
                Description = [],
                Arguments = [],
                ReturnType = TypeDescriptor.Any,
            }, new ModApiSimpleCommand(func));
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