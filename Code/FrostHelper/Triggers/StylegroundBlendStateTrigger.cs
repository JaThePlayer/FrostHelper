using FrostHelper.Helpers;

namespace FrostHelper.Triggers;

[CustomEntity("FrostHelper/StylegroundBlendStateTrigger", "FrostHelper/StylegroundBlendModeTrigger")]
internal class StylegroundBlendStateTrigger : Trigger {
    public string TargetTag;
    public BlendState BlendState;

    public StylegroundBlendStateTrigger(EntityData data, Vector2 offset) : base(data, offset) {
        TargetTag = data.Attr("tag");
        BlendState = data.GetBlendState("__blendStateCache", BlendState.AlphaBlend);
    }

    public override void OnEnter(Player player) {
        base.OnEnter(player);

        var lvl = FrostModule.GetCurrentLevel();

        HandleRenderer(lvl.Background);
        HandleRenderer(lvl.Foreground);
    }

    private void HandleRenderer(BackdropRenderer renderer) {
        foreach (var backdrop in renderer.Backdrops) {
            if (!backdrop.Tags.Contains(TargetTag))
                continue;

            CustomBackdropBlendModeHelper.SetBlendMode(backdrop, BlendState);
        }
    }
}
