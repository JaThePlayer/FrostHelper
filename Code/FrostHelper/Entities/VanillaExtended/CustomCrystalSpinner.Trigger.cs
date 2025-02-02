using FrostHelper.Triggers.Spinner;

namespace FrostHelper.Entities.VanillaExtended;

[CustomEntity("FrostHelper/TriggerSpinner")]
internal sealed class TriggerSpinner : CustomSpinner {
    private readonly CustomSpinnerSpriteSource _activatedSpriteSource;
    private readonly ChangeSpinnersTrigger.AnimationBehavior _animationBehavior;
    private readonly bool _activateOnPlayer;

    internal CollisionModes UnactivatedOnHoldable;


    private float _remainingDelay;
    private TriggerState _state;
    
    public TriggerSpinner(EntityData data, Vector2 offset) : base(data, offset)
    {
        _activatedSpriteSource = CustomSpinnerSpriteSource.Get(data.Attr("onDirectory"), "");
        _animationBehavior = data.Enum("animationBehavior", ChangeSpinnersTrigger.AnimationBehavior.ResetAndCompleteIn);
        _activateOnPlayer = data.Bool("activateOnPlayer", true);
        _remainingDelay = data.Float("delay", 0.3f);
        
        UnactivatedOnHoldable = data.Enum("unactivatedOnHoldable", CollisionModes.PassThrough);
    }

    public override void Update() {
        if (_state == TriggerState.Activating) {
            _remainingDelay -= Engine.DeltaTime;
            if (_remainingDelay <= 0f) {
                _state = TriggerState.Activated;
            }
        }
        
        base.Update();
    }

    protected override void OnPlayer(Player player) {
        switch (_state) {
            case TriggerState.Inactive:
                if (_activateOnPlayer)
                    ActivateIfNeeded();
                break;
            case TriggerState.Activating:
                break;
            case TriggerState.Activated:
                base.OnPlayer(player);
                break;
        }
    }

    protected override void OnHoldable(Holdable h) {
        switch (_state) {
            case TriggerState.Inactive:
                
                switch (UnactivatedOnHoldable) {
                    case CollisionModes.Activate:
                        ActivateIfNeeded();
                        break;
                    default:
                        DispatchStandardCollisionMode(UnactivatedOnHoldable);
                        break;
                }
                break;
            case TriggerState.Activating:
                break;
            case TriggerState.Activated:
                base.OnHoldable(h);
                break;
        }
    }


    private void ActivateIfNeeded() {
        if (_state != TriggerState.Inactive)
            return;
        
        _state = TriggerState.Activating;
        ChangeSprites(_activatedSpriteSource, _animationBehavior, finishAnimsIn: _remainingDelay);
    }

    enum TriggerState {
        Inactive,
        Activating,
        Activated,
    }
}