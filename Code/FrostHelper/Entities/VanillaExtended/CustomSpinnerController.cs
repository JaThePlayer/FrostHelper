using FrostHelper.ModIntegration;

namespace FrostHelper;

[CustomEntity("FrostHelper/CustomSpinnerController")]
[Tracked]
public class CustomSpinnerController : Entity {
    public readonly bool NoCycles;
    public readonly Effect? OutlineShader;

    internal Player? Player;

    public CustomSpinnerController() { }

    public CustomSpinnerController(EntityData data, Vector2 offset) : base() {
        NoCycles = !data.Bool("cycles", true);

        OutlineShader = ShaderHelperIntegration.TryGetEffect(data.Attr("outlineShader", ""));
    }

    public override void Awake(Scene scene) {
        base.Awake(scene);

        Player = scene.Tracker.SafeGetEntity<Player>();
    }
}
