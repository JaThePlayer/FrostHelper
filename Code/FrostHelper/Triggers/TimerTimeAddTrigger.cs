using FrostHelper.Helpers;
using System.Diagnostics;

namespace FrostHelper.Triggers;

[CustomEntity("FrostHelper/TimerChange")]
internal sealed class TimerTimeChangeTrigger(EntityData data, Vector2 offset) : Trigger(data, offset) {
    private readonly ConditionHelper.Condition _time = data.GetCondition("timeChange", "0");
    private readonly string _timerId = data.Attr("timerId", "");
    private readonly bool _oneUse = data.Bool("oneUse", false);
    private readonly Operations _operation = data.Enum("operation", Operations.Add);

    private enum Operations {
        Add,
        Set,
    }

    public override void OnEnter(Player player) {
        base.OnEnter(player);

        bool anyChanged = false;
        foreach (var entity in player.Scene.Tracker.SafeGetEntities<BaseTimerEntity>()) {
            if (entity is BaseTimerEntity timer && timer.TimerId == _timerId && timer.Started) {
                var time = _time.GetFloat(player.Scene.ToLevel().Session);

                timer.TimeLeft = _operation switch {
                    Operations.Add => timer.TimeLeft + time,
                    Operations.Set => time,
                    _ => throw new UnreachableException()
                };
                anyChanged = true;
            }
        }

        if (anyChanged && _oneUse) {
            RemoveSelf();
        }
    }
}