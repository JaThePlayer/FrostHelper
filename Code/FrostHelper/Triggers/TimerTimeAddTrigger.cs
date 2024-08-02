namespace FrostHelper.Triggers;

[CustomEntity("FrostHelper/TimerChange")]
internal sealed class TimerTimeChangeTrigger : Trigger {
    private readonly float _time;
    private readonly string _timerId;
    private readonly bool _oneUse;
    
    public TimerTimeChangeTrigger(EntityData data, Vector2 offset) : base(data, offset) {
        _time = data.Float("timeChange", 0f);
        _timerId = data.Attr("timerId", "");
        _oneUse = data.Bool("oneUse", false);
    }

    public override void OnEnter(Player player) {
        base.OnEnter(player);

        bool anyChanged = false;
        foreach (var entity in player.Scene.Tracker.SafeGetEntities<BaseTimerEntity>()) {
            if (entity is BaseTimerEntity timer && timer.TimerId == _timerId && timer.Started) {
                timer.TimeLeft += _time;
                anyChanged = true;
            }
        }

        if (anyChanged && _oneUse) {
            RemoveSelf();
        }
    }
}