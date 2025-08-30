using YamlDotNet.Serialization;

namespace FrostHelper;

public class FrostHelperSession : EverestModuleSession {
    public string? LightningColorA { get; set; }
    public string? LightningColorB { get; set; }
    public string? LightningFillColor { get; set; }
    public float? LightningFillColorMultiplier { get; set; } = null;//0.1f;

    public Color BloomColor { get; set; } = Color.White;
    public bool RainbowBloom { get; set; } = false;
    
    public Color? LightingColor { get; set; } = null;
    
    public float? NextDashSpeed { get; set; }
    public float? NextSuperJumpSpeed { get; set; }
    
    public float NoClimbTimer { get; set; }

    /// <summary>
    /// Used by some Frost Helper entities to make an entity remove itself immediately, but while allowing it to do other stuff
    /// </summary>
    public HashSet<EntityID> SoftDoNotLoad { get; set; } = new();

    public Color? FlashlightColor { get; set; } = null;

    // everything below might get refactored later to be more general-usecase:

    // for anyone thinking about accessing this from another mod: don't even think about it, I will intentionally break your mod if you do that.
    public HashSet<IceKeyInfo> PersistentIceKeys { get; set; } = new();

    // for anyone thinking about accessing this from another mod: don't even think about it, I will intentionally break your mod if you do that.
    [YamlIgnore]
    internal HashSet<DissolvedIceKeyInfo> PersistentIceKeysDissolvedThisRun { get; set; } = new();

    // for anyone thinking about accessing this from another mod: don't even think about it, I will intentionally break your mod if you do that.
    internal class DissolvedIceKeyInfo {
        public Vector2? RespawnPoint;
        public EntityID ID;

        public DissolvedIceKeyInfo(EntityID id, Vector2? respawn) {
            RespawnPoint = respawn;
            ID = id;
        }
    }

    // for anyone thinking about accessing this from another mod: don't even think about it, I will intentionally break your mod if you do that.
    // this is public ONLY because of serialization!
    public class IceKeyInfo {
        public Dictionary<string, object> Data { get; set; }
        public EntityID ID { get; set; }

        public Vector2 KeyStartPos { get; set; }

        public IceKeyInfo() {

        }

        public IceKeyInfo(EntityID id, Dictionary<string, object> data, Vector2 keyStartPos) {
            Data = data;
            ID = id;
            KeyStartPos = keyStartPos;
        }

        public override bool Equals(object? y) {
            if (y is null && this is null)
                return true;

            if (y is not IceKeyInfo inf)
                return false;

            return ID.ID == inf.ID.ID && ID.Level == inf.ID.Level;
        }

        public override int GetHashCode() {
            return ID.GetHashCode();
        }
    }
}
