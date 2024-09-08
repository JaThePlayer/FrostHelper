using FrostHelper.Helpers;
using System.Diagnostics;

namespace FrostHelper.Triggers.Activator;

[CustomEntity("FrostHelper/SwitchOnCounterActivator")]
internal sealed class SwitchOnCounterActivator : BaseActivator {
    private readonly string CounterName;
    private readonly List<SessionCounterComparer> _cases;

    internal override bool NeedsNodeIndexes => true;

    public SwitchOnCounterActivator(EntityData data, Vector2 offset) : base(data, offset) {
        ActivationMode = ActivationModes.All;
        Collidable = false;
        Active = false;

        CounterName = data.Attr("counter");
        _cases = [];
        foreach (var caseStr in data.Attr("cases").Split(',', StringSplitOptions.TrimEntries)) {
            var (caseVal, operation) = caseStr switch {
                ['>', '=', .. var rest] => (rest.Trim(), SessionCounterComparer.CounterOperation.GreaterThanOrEqual),
                ['<', '=', .. var rest] => (rest.Trim(), SessionCounterComparer.CounterOperation.LessThanOrEqual),
                ['!', '=', .. var rest] => (rest.Trim(), SessionCounterComparer.CounterOperation.NotEqual),
                ['=', '=', .. var rest] => (rest.Trim(), SessionCounterComparer.CounterOperation.Equal),
                ['>', .. var rest] => (rest.Trim(), SessionCounterComparer.CounterOperation.GreaterThan),
                ['<', .. var rest] => (rest.Trim(), SessionCounterComparer.CounterOperation.LessThan),
                _ => (caseStr, SessionCounterComparer.CounterOperation.Equal),
            };
            
            _cases.Add(new(CounterName, caseVal, operation));
        }
    }

    public override void OnEnter(Player player) {
        base.OnEnter(player);

        if (ToActivatePerNode is null) {
            ToActivate = FastCollideAll();
            if (ToActivatePerNode is null)
                throw new UnreachableException();
        }

        var level = player.level;

        var counter = level.Session.GetCounter(CounterName);

        for (int i = 0; i < ToActivatePerNode.Length; i++) {
            var caseObj = _cases.ElementAtOrDefault(i);
            if (caseObj is { }) {
                if (caseObj.Check(level)) {
                    ActivateAtNode(player, i);
                    break;
                }
            } else if (counter == i) {
                ActivateAtNode(player, i);
                break;
            }
        }
    }
}