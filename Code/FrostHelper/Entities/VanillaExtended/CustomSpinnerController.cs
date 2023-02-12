using FrostHelper.ModIntegration;

namespace FrostHelper;

[CustomEntity("FrostHelper/CustomSpinnerController")]
[Tracked]
public class CustomSpinnerController : Entity {
    public readonly bool NoCycles;
    public readonly Effect? OutlineShader;

    /// <summary>
    /// The first player in the scene, used for optimising proximity checks.
    /// </summary>
    internal Player? Player;

    /// <summary>
    /// The border color of the first added spinner. Used for outline rendering optimisations
    /// </summary>
    internal Color? FirstBorderColor = null;

    /// <summary>
    /// Whether border rendering can use Render Targets
    /// </summary>
    internal bool CanUseRenderTargetRender;

    /// <summary>
    /// Whether rendering can be optimised further thanks to all spinners using black borders
    /// </summary>
    internal bool CanUseBlackOutlineRenderTargetOpt = true;

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
