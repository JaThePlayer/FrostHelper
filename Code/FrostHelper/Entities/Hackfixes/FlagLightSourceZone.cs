namespace FrostHelper.Entities.Hackfixes;

/// <summary>
/// A port of Contort Helper's LightSourceZone, but with flag toggle support.
/// Do not use
/// </summary>
[CustomEntity("FrostHelper/HackfixFlagLightSourceZone")]
internal class FlagLightSourceZone : Entity {
    private static readonly Color[] _defaultColors = { Color.White };

    public readonly string Flag;
    public readonly bool FlagInverted;

    public FlagLightSourceZone(EntityData data, Vector2 offset) : base(data.Position + offset) {
        var amount = data.Int("amount", 5);
        var alpha = RangeFloat(data, "alphaMinimum", "alphaMaximum", 0.8f, 1f);
        var radius = RangeFloat(data, "radiusMinimum", "radiusMaximum", 46f, 48f);
        var startFade = RangeFloat(data, "startFadeMinimum", "startFadeMaximum", 22f, 24f);
        var endFade = RangeFloat(data, "endFadeMinimum", "endFadeMaximum", 46f, 48f);
        var colors = data.GetColors("colors", _defaultColors);
        var width = data.Width;
        var height = data.Height;

        for (int i = 0; i < amount; i++) {
            Vector2 position = new Vector2(Calc.Random.Range(0f, width), Calc.Random.Range(0f, height));
            float alpha2 = Range(alpha);
            Add(new VertexLight(position, Calc.Random.Choose(colors), alpha2, (int) Range(startFade), (int) Range(endFade)));
            Add(new BloomPoint(position, alpha2, Range(radius)));
        }

        Flag = data.Attr("flag");
        FlagInverted = data.Bool("flagInverted");

        Tag |= Tags.TransitionUpdate;
    }

    public override void Update() {
        base.Update();
        var lvl = (Scene as Level)!;
        var visible = lvl.Session.GetFlag(Flag) != FlagInverted;
        if (Visible != visible) {
            Visible = visible;
            foreach (var item in Components.components) {
                item.Visible = visible;
            }
        }
    }

    private float Range(Vector2 from) => Calc.Random.Range(from.X, from.Y);

    private Vector2 RangeFloat(EntityData data, string minName, string maxName, float minDefault, float maxDefault) {
        var min = data.Float(minName, minDefault);
        var max = data.Float(maxName, maxDefault);

        return new(min, max);
    }
}
