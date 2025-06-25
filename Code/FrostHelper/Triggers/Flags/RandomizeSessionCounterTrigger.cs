using FrostHelper.Helpers;
using System.Diagnostics;

namespace FrostHelper.Triggers;

[CustomEntity("FrostHelper/RandomizeSessionCounterTrigger")]
internal sealed class RandomizeSessionCounterTrigger : Trigger {
    private readonly CounterAccessor _counter;
    private readonly SessionRandomGetter _sessionRandom;
    
    public RandomizeSessionCounterTrigger(EntityData data, Vector2 offset) : base(data, offset)
    {
        _counter = new(data.Attr("counter", ""));

        _sessionRandom = new SessionRandomGetter(
            new CounterExpression(data.Attr("min", "0")).ToCondition(),
            new CounterExpression(data.Attr("max", "0")).ToCondition(),
            data.Enum("seedMode", SessionRandomGetter.SeedModes.SessionTime),
            data.Int("seed")
        );
    }

    public override void OnEnter(Player player) {
        base.OnEnter(player);

        var session = player.level.Session;
        _counter.Set(session, _sessionRandom.GetInt(session));
    }
}