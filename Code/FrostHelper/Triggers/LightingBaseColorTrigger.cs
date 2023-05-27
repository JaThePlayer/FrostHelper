namespace FrostHelper.Triggers;

[CustomEntity("FrostHelper/LightningBaseColorTrigger")]
public class LightingBaseColorTrigger : Trigger {
    public readonly Color Color;

    public LightingBaseColorTrigger(EntityData data, Vector2 offset) : base(data, offset) {
        Color = data.GetColor("color", "000000");
    }

    public override void OnEnter(Player player) {
        if (SceneAs<Level>() is Level level) {
            level.Lighting.BaseColor = Color;
        }
    }
}
