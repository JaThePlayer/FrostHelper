using FrostHelper.Helpers;
using System.Globalization;

namespace FrostHelper.Entities;

[CustomEntity("FrostHelper/FlagCounterController")]
internal sealed class FlagCounterController : Entity {
    private readonly struct Entry {
        public readonly ConditionHelper.Condition Condition;
        public readonly ConditionHelper.Condition Value;

        public Entry(string from) {
            var sepIndex = from.IndexOf(';');
            if (sepIndex < 0) {
                Value = ConditionHelper.CreateOrDefault("1", "1");
                Condition = ConditionHelper.CreateOrDefault(from, "");
            } else {
                Value = ConditionHelper.CreateOrDefault(from[(sepIndex+1)..], "1");
                Condition = ConditionHelper.CreateOrDefault(from[..sepIndex], "");
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

        Visible = false;

        if (_conditions.All(c => c.Condition.OnlyChecksFlags() && c.Value.OnlyChecksFlags())) {
            Active = false;
            Add(new FlagListener(flag: null, OnFlagSet, mustChange: true, triggerOnRoomBegin: true));
        } else {
            // Session counters are checked, we need to recalculate each frame :/
            Active = true;
        }
    }

    public override void Update() {
        if (Engine.Scene is Level level)
            UpdateCounter(level.Session);
    }

    private void OnFlagSet(Session session, string? flag, bool state) {
        UpdateCounter(session);
    }

    private void UpdateCounter(Session session) {
        var amt = 0;
        foreach (var c in _conditions) {
            amt += c.Condition.Check(session) ? c.Value.GetInt(session) : 0;
        }
        
        session.SetCounter(CounterName, amt);
    }
}