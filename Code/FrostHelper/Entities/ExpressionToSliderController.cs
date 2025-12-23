using FrostHelper.Components;
using FrostHelper.Helpers;

namespace FrostHelper.Entities;

[CustomEntity("FrostHelper/ExpressionToSliderController")]
internal sealed class ExpressionToSliderController : Entity {
    private readonly SliderAccessor _slider;
    
    public ExpressionToSliderController(EntityData data, Vector2 offset) : base(data.Position + offset) {
        Active = true;
        _slider = new SliderAccessor(data.Attr("slider"));
        var value = data.GetCondition("expression");

        Add(new ExpressionListener<float>(value, OnConditionChanged, activateOnStart: true));
    }

    private void OnConditionChanged(Entity arg1, Maybe<float> old, float newValue) {
        if (Scene is Level l) {
            _slider.Set(l.Session, newValue);
        }
    }
}