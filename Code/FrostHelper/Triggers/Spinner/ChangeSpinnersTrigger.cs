namespace FrostHelper.Triggers.Spinner;

[CustomEntity("FrostHelper/ChangeSpinnersTrigger")]
internal sealed class ChangeSpinnersTrigger : SpinnerTrigger {
    private readonly CustomSpinnerSpriteSource? _newSource;
    private readonly Tristate _newCollidable, _newRainbow;
    private readonly Color? _newTint, _newBorderTint;
    private readonly AnimationBehavior _animationBehavior;
    private readonly CustomSpinner.CollisionModes _newDashThrough, _newOnHoldable;
    private readonly int? _newDepth;
    
    private enum Tristate {
        LeaveUnchanged,
        True,
        False
    }
    
    internal enum AnimationBehavior {
        LeaveUnchanged,
        Reset,
        ResetAndCompleteIn,
    }
    
    public ChangeSpinnersTrigger(EntityData data, Vector2 offset) : base(data, offset)
    {
        var dir = data.Attr("newDirectory");
        if (!string.IsNullOrWhiteSpace(dir)) {
            _newSource = CustomSpinnerSpriteSource.Get(dir, "");
        }
        
        _newCollidable = data.Enum("newCollidable", Tristate.LeaveUnchanged);
        _newRainbow = data.Enum("newRainbow", Tristate.LeaveUnchanged);
        _newTint = data.GetColorNullable("newTint");
        _newBorderTint = data.GetColorNullable("newBorderColor");
        _animationBehavior = data.Enum("animationBehavior", AnimationBehavior.LeaveUnchanged);
        _newDashThrough = data.Enum("newDashThrough", CustomSpinner.CollisionModes.LeaveUnchanged);
        _newOnHoldable = data.Enum("newOnHoldable", CustomSpinner.CollisionModes.LeaveUnchanged);
        _newDepth = data.GetIntNullable("newDepth");
    }

    protected override void ChangeSpinner(Session session, CustomSpinner spinner, bool fromExternalSource) {
        if (_newSource is {})
            spinner.ChangeSprites(_newSource, _animationBehavior);

        switch (_newCollidable) {
            case Tristate.False:
                spinner.ColliderDisabledExternally = true;
                spinner.Collidable = false;
                break;
            case Tristate.True:
                spinner.ColliderDisabledExternally = false;
                spinner.Collidable = spinner.ShouldBeCollidable;
                spinner.CreateCollider();
                break;
        }
            
        if (_newTint is { }) {
            spinner.SetColor(_newTint.Value);
        }
        if (_newBorderTint is { }) {
            spinner.SetBorderColor(_newBorderTint.Value);
        }
        
        switch (_newRainbow) {
            case Tristate.False:
                spinner.Rainbow = false;
                spinner.SetColor(spinner.Tint, force: true);
                break;
            case Tristate.True:
                spinner.Rainbow = true;
                spinner.UpdateHue();
                break;
        }

        SetCollisionMode(ref spinner.DashThrough, _newDashThrough);
        SetCollisionMode(ref spinner.HoldableCollisionMode, _newOnHoldable);

        if (_newDepth is { } newDepth) {
            spinner.SetDepth(newDepth);
        }
    }

    private void SetCollisionMode(ref CustomSpinner.CollisionModes spinnerMode,
        CustomSpinner.CollisionModes newMode) {
        if (newMode == CustomSpinner.CollisionModes.LeaveUnchanged)
            return;
        
        spinnerMode = newMode;
    }

    private static void SetBool(ref bool x, Tristate state) {
        x = state switch {
            Tristate.False => false,
            Tristate.True => true,
            _ => x,
        };
    }
}