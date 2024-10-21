using FrostHelper.Helpers;

namespace FrostHelper.Entities;

[CustomEntity("FrostHelper/TimeToCounterController")]
internal sealed class TimeToCounterController : Entity {
    private enum TimerKinds {
        Session,
        File,
    }
    
    private readonly CounterAccessor _counter;
    private readonly CounterAccessor.CounterTimeUnits _unit;
    private readonly TimerKinds _timerKind;
    
    public TimeToCounterController(EntityData data, Vector2 offset) : base(data.Position + offset) {
        _counter = new(data.Attr("counter"));
        _unit = data.Enum("unit", CounterAccessor.CounterTimeUnits.Milliseconds);
        _timerKind = data.Enum("timerKind", TimerKinds.Session);
        
        Active = true;
        Visible = false;
    }

    public override void Update() {
        if (Scene is Level level) {
            var timeInTicks = _timerKind switch {
                TimerKinds.Session => level.Session.Time,
                TimerKinds.File => SaveData.Instance.Time,
                _ => throw new ArgumentOutOfRangeException()
            };
            _counter.SetTime(level.Session, TimeSpan.FromTicks(timeInTicks), _unit);
        }
    }
}