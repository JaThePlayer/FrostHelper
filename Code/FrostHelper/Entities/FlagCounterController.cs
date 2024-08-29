using FrostHelper.Helpers;

namespace FrostHelper.Entities;

[CustomEntity("FrostHelper/FlagCounterController")]
internal sealed class FlagCounterController : Entity {
    public readonly string CounterName;
    public readonly ConditionHelper.Condition[] Conditions;
    
    public FlagCounterController(EntityData data, Vector2 offset) : base(data.Position + offset) {
        CounterName = data.Attr("counter");
        Conditions = data.Attr("flags")
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(s => new ConditionHelper.Condition(s))
            .ToArray();

        Active = false;
        Visible = false;
        
        Add(new FlagListener(flag: null, OnFlagSet, mustChange: true, triggerOnRoomBegin: true));
    }

    private void OnFlagSet(Session session, string? flag, bool state) {
        var amt = 0;
        foreach (var c in Conditions) {
            amt += c.Check(session) ? 1 : 0;
        }
        
        session.SetCounter(CounterName, amt);
    }
}