using Celeste.Mod.Entities;

namespace FrostHelper {
    [CustomEntity("FrostHelper/ColoredLightbeam")]
    public class ColoredLightbeam : LightBeam {
        // thanks for making this not static
        private static FieldInfo LightBeam_color = typeof(LightBeam).GetField("color", BindingFlags.NonPublic | BindingFlags.Instance);
        public ColoredLightbeam(EntityData data, Vector2 offset) : base(data, offset) {
            LightBeam_color.SetValue(this, ColorHelper.GetColor(data.Attr("color", "ccffff")));
        }
    }
}
