using FrostHelper.Helpers;
using FrostHelper.ModIntegration;

namespace FrostHelper;

[CustomEntity("FrostHelper/ColorRainbowChannel")]
internal sealed class ColorRainbowChannelSource(EntityData data, Vector2 offset) : RainbowChannelSource(data, offset) {
    private readonly SessionExpression<Color> _color = data.GetExpression<Color>(RainbowChannelExpression.ExpressionContext, "color");
    private readonly SessionExpression<float> _alpha = data.GetExpression<float>(RainbowChannelExpression.ExpressionContext, "alpha");

    public override RainbowChannel CreateChannel() {
        return new ColorRainbowChannel(_color, _alpha) {
            ChannelId = ChannelId
        };
    }
    
    sealed class ColorRainbowChannel(SessionExpression<Color> color, SessionExpression<float> alpha) : RainbowChannel, ISavestatePersisted {
        public override Color GetColor(Scene scene, Vector2 position) {
            var session = scene.ToLevel().Session;
            var userdata = RainbowChannelExpression.Instance.Update(position);

            return color.Get(session, userdata) * alpha.Get(session, userdata);
        }
    }
}
