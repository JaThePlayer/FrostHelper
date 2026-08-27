using FrostHelper.Helpers;

namespace FrostHelper.Triggers;

[CustomEntity("FrostHelper/IndirectFlagTrigger")]
internal sealed class IndirectFlagTrigger(EntityData data, Vector2 offset) : Trigger(data, offset) {
    private readonly SessionExpression<string> _flag = data.GetExpression<string>("flag");
    private readonly SessionExpression<bool> _value = data.GetExpression<bool>("value");

    public override void OnEnter(Player player) {
        base.OnEnter(player);
        if (Scene.MaybeLevel() is not { } level)
            return;
        var session = level.Session;
        session.SetFlag(_flag.Get(session), _value.Get(session));
    }
}

[CustomEntity("FrostHelper/IndirectCounterTrigger")]
internal sealed class IndirectCounterTrigger(EntityData data, Vector2 offset) : Trigger(data, offset) {
    private readonly SessionExpression<string> _counter = data.GetExpression<string>("counter");
    private readonly SessionExpression<int> _value = data.GetExpression<int>("value");

    public override void OnEnter(Player player) {
        base.OnEnter(player);
        if (Scene.MaybeLevel() is not { } level)
            return;
        var session = level.Session;
        session.SetCounter(_counter.Get(session), _value.Get(session));
    }
}

[CustomEntity("FrostHelper/IndirectSliderTrigger")]
internal sealed class IndirectSliderTrigger(EntityData data, Vector2 offset) : Trigger(data, offset) {
    private readonly SessionExpression<string> _slider = data.GetExpression<string>("slider");
    private readonly SessionExpression<float> _value = data.GetExpression<float>("value");

    public override void OnEnter(Player player) {
        base.OnEnter(player);
        if (Scene.MaybeLevel() is not { } level)
            return;
        var session = level.Session;
        session.SetSlider(_slider.Get(session), _value.Get(session));
    }
}
