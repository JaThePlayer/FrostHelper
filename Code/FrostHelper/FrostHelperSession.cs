namespace FrostHelper;

public class FrostHelperSession : Celeste.Mod.EverestModuleSession {
    public string LightningColorA { get; set; } = null!;
    public string LightningColorB { get; set; } = null!;
    public string LightningFillColor { get; set; } = "ffffff";
    public float? LightningFillColorMultiplier { get; set; } = null;//0.1f;

    public Color BloomColor { get; set; } = Color.White;
    public bool RainbowBloom { get; set; } = false;
}
