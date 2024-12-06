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
        // todo: detailed photosensitive settings once those land in stable
        ["dashes"] = new DashAccessor(),
        ["maxDashes"] = new MaxDashAccessor(),
        ["stamina"] = new StaminaAccessor(),
        ["speed.x"] = new PlayerSpeedXAccessor(),
        ["speed.y"] = new PlayerSpeedYAccessor(),
        ["pi"] = new PiAccessor(),
    };

    // Exposed via API
    internal static void RegisterSimpleCommand(string modName, string cmdName, Func<Session, object> func) {
        var key = $"{modName}.{cmdName}";
        if (Registry.TryGetValue(key, out var existing)) {
            Logger.Warn("FrostHelper.ConditionHelper", $"Replacing simple command '${key}'");
        }

        Registry[key] = new ModApiSimpleCommand(modName, cmdName, func);
    }

    private sealed class ModApiSimpleCommand(string modName, string cmdName, Func<Session, object> func) : Condition {
        public override object Get(Session session) {
            var ret = func(session);
            if (ret is bool b)
                return b ? 1 : 0;
            return ret;
        }

        public override bool OnlyChecksFlags() => false;

        protected override IEnumerable<object> GetArgsForDebugPrint() => [modName, cmdName, func];
    }

    private sealed class DeathsAccessor(bool inCurrentLevel) : Condition {
        public override object Get(Session session) {
            return inCurrentLevel ? session.DeathsInCurrentLevel : session.Deaths;
        }

        public override bool OnlyChecksFlags() => false;

        protected internal override Type ReturnType => typeof(int);
    }

    private sealed class HasGoldenAccessor : Condition {
        public override object Get(Session session) {
            return session.GrabbedGolden ? 1 : 0;
        }

        public override bool OnlyChecksFlags() => false;

        protected internal override Type ReturnType => typeof(int);
    }
    
    private sealed class PiAccessor : Condition {
        public override object Get(Session session) {
            return float.Pi;
        }

        public override bool OnlyChecksFlags() => false;

        protected internal override Type ReturnType => typeof(float);
    }

    private sealed class RestartedFromGoldenAccessor : Condition {
        public override object Get(Session session) {
            return session.RestartedFromGolden ? 1 : 0;
        }

        public override bool OnlyChecksFlags() => false;

        protected internal override Type ReturnType => typeof(int);
    }

    private sealed class CoreModeAccessor : Condition {
        public override object Get(Session session) {
            return (int) session.CoreMode;
        }

        public override bool OnlyChecksFlags() => false;

        protected internal override Type ReturnType => typeof(int);
    }

    private sealed class PhotosensitiveAccessor : Condition {
        public override object Get(Session session) {
            return Settings.Instance.DisableFlashes ? 1 : 0;
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

        public override object Get(Session session) {
            if (Engine.Scene.Tracker.SafeGetEntity<Player>() is { } player)
                return _lastValue = GetFromPlayer(player);

            return _lastValue ?? 0;
        }

        public override bool OnlyChecksFlags() => false;

        protected internal override Type ReturnType => typeof(T);
    }
}