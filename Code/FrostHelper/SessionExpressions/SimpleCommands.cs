using Celeste.Mod.Core;
using static FrostHelper.Helpers.ConditionHelper;

namespace FrostHelper.SessionExpressions;

internal static class SimpleCommands {
    /// <summary>
    /// Simple commands accessible via $cmdname
    /// </summary>
    internal static readonly Dictionary<string, Condition> Registry = new() {
        ["deathsHere"] = new DeathsAccessor(inCurrentLevel: true),
        ["deaths"] = new DeathsAccessor(inCurrentLevel: false),
        ["hasGolden"] = new HasGoldenAccessor(),
        ["restartedFromGolden"] = new RestartedFromGoldenAccessor(),
        ["coreMode"] = new CoreModeAccessor(),
        ["photosensitive"] = new PhotosensitiveAccessor(),
        ["allowLightning"] = new AllowLightningAccessor(),
        ["allowScreenFlash"] = new AllowScreenFlashAccessor(),
        ["allowGlitch"] = new AllowGlitchAccessor(),
        ["allowDistort"] = new AllowDistortAccessor(),
        ["allowTextHighlight"] = new AllowTextHighlightAccessor(),
        ["dashes"] = new DashAccessor(),
        ["maxDashes"] = new MaxDashAccessor(),
        ["stamina"] = new StaminaAccessor(),
        ["player"] = new PlayerAccessor(),
        ["speed"] = new PlayerSpeedAccessor(),
        ["speed.x"] = new PlayerSpeedXAccessor(),
        ["speed.y"] = new PlayerSpeedYAccessor(),
        ["pi"] = new PiAccessor(),
        ["dtime"] = new DeltaTimeAccessor(),
        ["roomName"] = new RoomNameAccessor(),
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
                return b ? 1 : 0;
            return ret;
        }

        protected override IEnumerable<object> GetArgsForDebugPrint() => [func];
    }

    private sealed class DeathsAccessor(bool inCurrentLevel) : Condition {
        public override object Get(Session session, object? userdata) => inCurrentLevel ? session.DeathsInCurrentLevel : session.Deaths;

        protected internal override Type ReturnType => typeof(int);
    }

    private sealed class HasGoldenAccessor : Condition {
        public override object Get(Session session, object? userdata) => session.GrabbedGolden ? 1 : 0;

        protected internal override Type ReturnType => typeof(int);
    }
    
    private sealed class PiAccessor : Condition {
        public override object Get(Session session, object? userdata) => float.Pi;

        protected internal override Type ReturnType => typeof(float);
    }
    
    private sealed class DeltaTimeAccessor : Condition {
        public override object Get(Session session, object? userdata) => Engine.DeltaTime;

        protected internal override Type ReturnType => typeof(float);
    }
    
    private sealed class RoomNameAccessor : Condition {
        public override object Get(Session session, object? userdata) => session.Level;

        protected internal override Type ReturnType => typeof(string);
    }

    private sealed class RestartedFromGoldenAccessor : Condition {
        public override object Get(Session session, object? userdata) => session.RestartedFromGolden ? 1 : 0;

        public override bool OnlyChecksFlags() => false;

        protected internal override Type ReturnType => typeof(int);
    }

    private sealed class CoreModeAccessor : Condition {
        public override object Get(Session session, object? userdata) {
            return (int) session.CoreMode;
        }

        public override bool OnlyChecksFlags() => false;

        protected internal override Type ReturnType => typeof(int);
    }

    private sealed class PhotosensitiveAccessor : Condition {
        public override object Get(Session session, object? userdata) {
            return Settings.Instance.DisableFlashes ? 1 : 0;
        }

        public override bool OnlyChecksFlags() => false;

        protected internal override Type ReturnType => typeof(int);
    }
    
    private sealed class AllowLightningAccessor : Condition {
        public override object Get(Session session, object? userdata) {
            return CoreModule.Settings.AllowLightning ? 1 : 0;
        }

        public override bool OnlyChecksFlags() => false;

        protected internal override Type ReturnType => typeof(int);
    }
    
    private sealed class AllowDistortAccessor : Condition {
        public override object Get(Session session, object? userdata) {
            return CoreModule.Settings.AllowDistort ? 1 : 0;
        }

        public override bool OnlyChecksFlags() => false;

        protected internal override Type ReturnType => typeof(int);
    }
    
    private sealed class AllowGlitchAccessor : Condition {
        public override object Get(Session session, object? userdata) {
            return CoreModule.Settings.AllowGlitch ? 1 : 0;
        }

        public override bool OnlyChecksFlags() => false;

        protected internal override Type ReturnType => typeof(int);
    }
    
    private sealed class AllowScreenFlashAccessor : Condition {
        public override object Get(Session session, object? userdata) {
            return CoreModule.Settings.AllowScreenFlash ? 1 : 0;
        }

        public override bool OnlyChecksFlags() => false;

        protected internal override Type ReturnType => typeof(int);
    }
    
    private sealed class AllowTextHighlightAccessor : Condition {
        public override object Get(Session session, object? userdata) {
            return CoreModule.Settings.AllowTextHighlight ? 1 : 0;
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
    
    private sealed class PlayerSpeedXAccessor : PlayerGetterCondition<float> {
        protected override float GetFromPlayer(Player player) => player.Speed.X;
    }

    private sealed class PlayerSpeedYAccessor : PlayerGetterCondition<float> {
        protected override float GetFromPlayer(Player player) => player.Speed.Y;
    }

    private sealed class StaminaAccessor : PlayerGetterCondition<float> {
        protected override float GetFromPlayer(Player player) => player.Stamina;
    }

    private abstract class PlayerGetterCondition<T> : Condition where T : notnull {
        private object _lastValue;

        protected abstract T GetFromPlayer(Player player);

        public override object Get(Session session, object? userdata) {
            if (Engine.Scene.Tracker.SafeGetEntity<Player>() is { } player)
                return _lastValue = GetFromPlayer(player);

            return _lastValue ?? 0;
        }

        public override bool OnlyChecksFlags() => false;

        protected internal override Type ReturnType => typeof(T);
    }
}