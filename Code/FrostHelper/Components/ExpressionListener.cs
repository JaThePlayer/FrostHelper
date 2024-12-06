using FrostHelper.Helpers;

namespace FrostHelper.Components;

internal sealed class ExpressionListener(ConditionHelper.Condition cond, Action<Entity> onCondition)
    : Component(true, false) {

    private bool _lastConditionMet;
    
    public override void Update() {
        var met = cond.Check();
        if (met && !_lastConditionMet) {
            onCondition(Entity);
        }
        _lastConditionMet = met;
        
        base.Update();
    }
}