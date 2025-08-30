using FrostHelper.Helpers;

namespace FrostHelper;

[CustomEntity("FrostHelper/StylegroundMoveTrigger")]
internal class StylegroundMoveTrigger : Trigger {
    internal class TweenHolder : Entity { }

    public string TargetTag;
    public Vector2 Movement;
    public Ease.Easer Easer;
    public float Duration;
    public bool Once;
    public AfterDeathBehaviours AfterDeath;

    public enum AfterDeathBehaviours {
        Reset, // resets the stylegrounds to the position before any movement.
        Stay, // makes the styleground stay at the location it was before death.
        SnapToEnd, // makes the styleground snap to the end position after death.
    }

    public StylegroundMoveTrigger(EntityData data, Vector2 offset) : base(data, offset) {
        TargetTag = data.Attr("tag");

        Easer = data.Easing("easing", Ease.CubeInOut);
        Duration = data.Float("duration", 1f);
        Movement = new(data.Float("moveByX", 0f), data.Float("moveByY", 0f));
        Once = data.Bool("once", false);

        AfterDeath = data.Enum("afterDeath", AfterDeathBehaviours.Reset);
    }

    public override void OnEnter(Player player) {
        base.OnEnter(player);

        var lvl = FrostModule.GetCurrentLevel();

        // If we're one use, the RemoveSelf call below would break all tweens if they were attached to this trigger. We'll use a helper entity for those cases.
        Entity tweenHolder = Once ? ControllerHelper<TweenHolder>.AddToSceneIfNeeded(lvl) : this;
        HandleRenderer(lvl.Background, tweenHolder);
        HandleRenderer(lvl.Foreground, tweenHolder);

        if (Once)
            RemoveSelf();
    }

    private void StoreOrigPosition(Backdrop backdrop) {
        if (AfterDeath is AfterDeathBehaviours.Reset)
            backdrop.GetOrCreateDynamicDataAttached<BackdropHelper.OrigPositionData>().Pos ??= backdrop.Position;
    }

    private void HandleRenderer(BackdropRenderer renderer, Entity tweenHolder) {
        var duration = Duration;

        if (duration == 0f) {
            foreach (var backdrop in renderer.Backdrops) {
                if (!backdrop.Tags.Contains(TargetTag))
                    continue;

                StoreOrigPosition(backdrop);
                backdrop.Position += Movement;
            }

            return;
        }

        foreach (var backdrop in renderer.Backdrops) {
            if (!backdrop.Tags.Contains(TargetTag))
                continue;

            var tween = Tween.Create(Tween.TweenMode.Oneshot, Easer, duration, true);
            tweenHolder.Add(tween);

            var startPos = backdrop.Position;
            //var lastEased = tween.Eased;
            var lastMovement = Vector2.Zero;
            if (AfterDeath is AfterDeathBehaviours.SnapToEnd) // since everything else uses null coalescence, this will work even if other triggers are using the Reset settings
                backdrop.GetOrCreateDynamicDataAttached<BackdropHelper.OrigPositionData>().Pos = backdrop.Position + Movement;
            else 
                StoreOrigPosition(backdrop);

            tween.OnUpdate = (t) => {
                var eased = t.Easer(Math.Min(1f, t.Percent));
                var move = (Movement * eased);

                backdrop.Position += move - lastMovement;
                lastMovement = move;
                //backdrop.Position += Movement * (eased - lastEased);

                //lastEased = eased;
            };

            tween.OnComplete = (t) => {
                backdrop.Position = startPos + Movement;
            };
        }
    }
}
