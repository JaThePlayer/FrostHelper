namespace FrostHelper;

public class FrostHelperSession : Celeste.Mod.EverestModuleSession {
    public string LightningColorA { get; set; } = null!;
    public string LightningColorB { get; set; } = null!;
    public string LightningFillColor { get; set; } = "ffffff";
    public float? LightningFillColorMultiplier { get; set; } = null;//0.1f;

    public Color BloomColor { get; set; } = Color.White;
    public bool RainbowBloom { get; set; } = false;

    /// <summary>
    /// Used by some Frost Helper entities to make an entity remove itself immediately, but while allowing it to do other stuff
    /// </summary>
    public HashSet<EntityID> SoftDoNotLoad { get; set; } = new();
}
