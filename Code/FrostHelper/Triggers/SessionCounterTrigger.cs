namespace FrostHelper.Triggers;

[CustomEntity("FrostHelper/SessionCounterTrigger")]
internal sealed class SessionCounterTrigger : Trigger {
    public readonly string CounterName;
    public readonly int Value;
    public readonly CounterOperation Operation;
    public readonly bool ClearOnSpawn;

    public enum CounterOperation {
        Increment,
        Decrement,
        Multiply,
        Divide,
        Set,
    }
    
    public SessionCounterTrigger(EntityData data, Vector2 offset) : base(data, offset) {
        CounterName = data.Attr("counter", "");
        Value = data.Int("value", 0);
        Operation = data.Enum("operation", CounterOperation.Set);
        ClearOnSpawn = data.Bool("clearOnSpawn", false);
    }

    public override void Added(Scene scene) {
        base.Added(scene);
        if (ClearOnSpawn && scene is Level level) {
            level.Session.SetCounter(CounterName, 0);
        }
    }

    public override void OnEnter(Player player) {
        base.OnEnter(player);

        if (FrostModule.TryGetCurrentLevel() is { } lvl) {
            var counter = lvl.Session.GetCounterObj(CounterName);
            switch (Operation) {
                case CounterOperation.Increment:
                    counter.Value += Value;
                    break;
                case CounterOperation.Decrement:
                    counter.Value -= Value;
                    break;
                case CounterOperation.Multiply:
                    counter.Value *= Value;
                    break;
                case CounterOperation.Divide:
                    counter.Value /= Value;
                    break;
                case CounterOperation.Set:
                    counter.Value = Value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(Operation));
            }
        }
    }
}