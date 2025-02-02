namespace FrostHelper.Triggers;

[CustomEntity("FrostHelper/LightingBaseColorTrigger", "FrostHelper/LightningBaseColorTrigger")]
public class LightingBaseColorTrigger : Trigger {
    public readonly Color Color;
    public bool Persistent;

    [OnLoad]
    internal static void Load() {
        // Doesn't quite work yet iirc, I'll look into it later
        //Everest.Events.Level.OnLoadLevel += LevelOnLoadLevel;
    }

    [OnUnload]
    internal static void Unload() {
        //Everest.Events.Level.OnLoadLevel -= LevelOnLoadLevel;
    }
    
    private static void LevelOnLoadLevel(Level level, Player.IntroTypes playerintro, bool isfromloader) {
        if (FrostModule.Session.LightingColor is { } color) {
            level.Lighting.BaseColor = color;
        }
    }

    public LightingBaseColorTrigger(EntityData data, Vector2 offset) : base(data, offset) {
        Color = data.GetColor("color", "000000");
        Persistent = data.Bool("persistent", false);
    }

    public override void OnEnter(Player player) {
        if (SceneAs<Level>() is { } level) {
            level.Lighting.BaseColor = Color;

            if (Persistent) {
                FrostModule.Session.LightingColor = Color;
            }
        }
    }
}
