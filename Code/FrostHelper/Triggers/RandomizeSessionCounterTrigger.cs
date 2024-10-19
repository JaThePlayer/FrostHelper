using FrostHelper.Helpers;
using System.Diagnostics;

namespace FrostHelper.Triggers;

[CustomEntity("FrostHelper/RandomizeSessionCounterTrigger")]
internal sealed class RandomizeSessionCounterTrigger : Trigger {
    private readonly CounterExpression _min;
    private readonly CounterExpression _max;
    private readonly CounterAccessor _counter;
    private readonly SeedModes _seedMode;

    private readonly Random? _rng;

    private enum SeedModes {
        SessionTime,
        RoomSeed,
        FullRandom,
        Custom
    }
    
    public RandomizeSessionCounterTrigger(EntityData data, Vector2 offset) : base(data, offset)
    {
        _counter = new(data.Attr("counter", ""));
        _min = new(data.Attr("min", "0"));
        _max = new(data.Attr("max", "0"));
        _seedMode = data.Enum("seedMode", SeedModes.SessionTime);

        _rng = _seedMode switch {
            SeedModes.RoomSeed => new Random(Calc.Random.Next()),
            SeedModes.FullRandom => Random.Shared,
            SeedModes.Custom => new Random(data.Int("seed")),
            _ => null
        };
    }

    public override void OnEnter(Player player) {
        base.OnEnter(player);
        
        var session = player.level.Session;
        var min = _min.Get(session);
        var max = _max.Get(session);
        if (min > max)
            (min, max) = (max, min);

        int val = _rng?.Next(min, max + 1) ?? _seedMode switch {
            // Use splitmix64 instead, avoiding creating a new Random instance each time
            SeedModes.SessionTime => RandomExt.RandomInclusive((ulong)player.SceneAs<Level>().Session.Time, min, max),
            _ => throw new UnreachableException(),
        };
        
        _counter.Set(session, val);
    }
}