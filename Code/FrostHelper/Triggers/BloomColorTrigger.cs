namespace FrostHelper;

[CustomEntity("FrostHelper/BloomColorTrigger")]
public class BloomColorTrigger : Trigger {
    public Color Color;

    public BloomColorTrigger(EntityData data, Vector2 offset) : base(data, offset) {
        Color = data.GetColor("color", "ffffff");
    }

    public override void OnEnter(Player player) {
        base.OnEnter(player);

        API.API.SetBloomColor(Color);
    }
}


[CustomEntity("FrostHelper/RainbowBloomTrigger")]
public class RainbowBloomTrigger : Trigger {
    public bool Enable;

    public RainbowBloomTrigger(EntityData data, Vector2 offset) : base(data, offset) {
        Enable = data.Bool("enable", true);
    }

    [OnLoad]
    public static void Load() {
        BloomColorChange.ColorManipulator += RainbowManipulator;
    }

    [OnUnload]
    public static void Unload() {
#pragma warning disable CS8601 // Possible null reference assignment... what
        BloomColorChange.ColorManipulator -= RainbowManipulator;
#pragma warning restore CS8601
    }

    private static Color RainbowManipulator(Color c) => FrostModule.Session.RainbowBloom ? ColorHelper.GetHue(Engine.Scene, new()) : c;

    public override void OnEnter(Player player) {
        base.OnEnter(player);

        FrostModule.Session.RainbowBloom = Enable;
    }
}