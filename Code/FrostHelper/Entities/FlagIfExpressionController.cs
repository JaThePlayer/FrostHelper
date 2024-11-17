using FrostHelper.Helpers;

namespace FrostHelper.Entities;

[CustomEntity("FrostHelper/FlagIfExpressionController")]
internal sealed class FlagIfExpressionController : Entity {
    private readonly ConditionHelper.Condition _value;
    private readonly string _flag;
    
    public FlagIfExpressionController(EntityData data, Vector2 offset) : base(data.Position + offset) {
        Active = true;
        _flag = data.Attr("flagToSet");
        _value = data.GetCondition("expression");
    }

    public override void Update() {
        if (Scene is Level l) {
            l.Session.SetFlag(_flag, _value.Check(l.Session));
        }
    }
}