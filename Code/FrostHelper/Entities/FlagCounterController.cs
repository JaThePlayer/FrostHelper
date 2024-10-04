using FrostHelper.Helpers;
using System.Globalization;

namespace FrostHelper.Entities;

[CustomEntity("FrostHelper/FlagCounterController")]
internal sealed class FlagCounterController : Entity {
    private readonly struct Entry {
        public readonly ConditionHelper.Condition Condition;
        public readonly int Value;

        public Entry(string from) {
            var sepIndex = from.IndexOf(';');
            if (sepIndex < 0) {
                Value = 1;
                Condition = new(from);
            } else {
                Value = int.Parse(from.AsSpan(sepIndex+1), CultureInfo.InvariantCulture);
                Condition = new(from[..sepIndex]);
            }
        }
    }
    
    public readonly string CounterName;
    private readonly Entry[] _conditions;
    
    public FlagCounterController(EntityData data, Vector2 offset) : base(data.Position + offset) {
        CounterName = data.Attr("counter");
        _conditions = data.Attr("flags")
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(s => new Entry(s))
            .ToArray();

        Active = false;
        Visible = false;
        
        Add(new FlagListener(flag: null, OnFlagSet, mustChange: true, triggerOnRoomBegin: true));
    }

    private void OnFlagSet(Session session, string? flag, bool state) {
        var amt = 0;
        foreach (var c in _conditions) {
            amt += c.Condition.Check(session) ? c.Value : 0;
        }
        
        session.SetCounter(CounterName, amt);
    }
}