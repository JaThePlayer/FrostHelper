using FrostHelper.Helpers;
using FrostHelper.ModIntegration;

namespace FrostHelper;

[CustomEntity("FrostHelper/HsvRainbowChannel")]
internal sealed class HsvRainbowChannelSource : RainbowChannelSource {
    private readonly SessionExpression<float> _hue;
    private readonly SessionExpression<float> _s;
    private readonly SessionExpression<float> _v;
    
    public HsvRainbowChannelSource(EntityData data, Vector2 offset) : base(data, offset) {
        _hue = new(data.GetCondition(RainbowChannelExpression.ExpressionContext, "hue"));
        _s = new(data.GetCondition(RainbowChannelExpression.ExpressionContext, "s"));
        _v = new(data.GetCondition(RainbowChannelExpression.ExpressionContext, "v"));
    }

    public override RainbowChannel CreateChannel() {
        return new HsvRainbowChannel(_hue, _s, _v) { ChannelId = ChannelId };
    }

    sealed class HsvRainbowChannel(SessionExpression<float> hue, SessionExpression<float> s, SessionExpression<float> v) : RainbowChannel, ISavestatePersisted {
        public override Color GetColor(Scene scene, Vector2 position) {
            var session = scene.ToLevel().Session;
            var userdata = RainbowChannelExpression.Instance.Update(scene, position);

            return Calc.HsvToColor(hue.Get(session, userdata), s.Get(session, userdata), v.Get(session, userdata));
        }
    }
}