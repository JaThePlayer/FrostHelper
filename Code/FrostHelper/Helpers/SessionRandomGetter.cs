using FrostHelper.ModIntegration;
using System.Diagnostics;

namespace FrostHelper.Helpers;

internal sealed class SessionRandomGetter(ConditionHelper.Condition minCond, ConditionHelper.Condition maxCond, SessionRandomGetter.SeedModes mode, int seed) : ISavestatePersisted {
    public enum SeedModes {
        SessionTime,
        RoomSeed,
        FullRandom,
        Custom
    }
    
    private Random? _rng = mode switch {
        SeedModes.RoomSeed => new Random(Calc.Random.Next()),
        SeedModes.FullRandom => Random.Shared,
        SeedModes.Custom => new Random(seed),
        _ => null
    };

    public float GetFloat(Session session) {
        var min = minCond.GetFloat(session);
        var max = maxCond.GetFloat(session);
        if (min > max)
            (min, max) = (max, min);

        var val = (_rng?.NextFloat(max - min) + min) ?? mode switch {
            // Use splitmix64 instead, avoiding creating a new Random instance each time
            SeedModes.SessionTime => RandomExt.RandomFloat((ulong)session.Time, min, max),
            _ => throw new UnreachableException(),
        };

        return val;
    }
    
    public int GetInt(Session session) {
        var min = minCond.GetInt(session);
        var max = maxCond.GetInt(session);
        if (min > max)
            (min, max) = (max, min);

        var val = _rng?.Next(min, max) ?? mode switch {
            // Use splitmix64 instead, avoiding creating a new Random instance each time
            SeedModes.SessionTime => RandomExt.RandomInclusive((ulong)session.Time, min, max),
            _ => throw new UnreachableException(),
        };

        return val;
    }
}