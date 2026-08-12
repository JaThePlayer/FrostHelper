using FrostHelper.Helpers;
using FrostHelper.ModIntegration;

namespace FrostHelper;

[CustomEntity("FrostHelper/HsvRainbowChannel")]
internal sealed class HsvRainbowChannelSource : RainbowChannelSource {
    private readonly SessionExpression<float> _hue;
    private readonly SessionExpression<float> _s;
    private readonly SessionExpression<float> _v;
    private readonly SessionExpression<float> _alpha;
    
    public HsvRainbowChannelSource(EntityData data, Vector2 offset) : base(data, offset) {
        _hue = new(data.GetCondition(RainbowChannelExpression.ExpressionContext, "hue"));
        _s = new(data.GetCondition(RainbowChannelExpression.ExpressionContext, "s"));
        _v = new(data.GetCondition(RainbowChannelExpression.ExpressionContext, "v"));
        _alpha = new(data.GetCondition(RainbowChannelExpression.ExpressionContext, "alpha"));
    }

    public override RainbowChannel CreateChannel() {
        return new HsvRainbowChannel(_hue, _s, _v, _alpha) { ChannelId = ChannelId };
    }

    sealed class HsvRainbowChannel(SessionExpression<float> hue, SessionExpression<float> s, SessionExpression<float> v, SessionExpression<float> alpha) : RainbowChannel, ISavestatePersisted {
        public override Color GetColor(Scene scene, Vector2 position) {
            var session = scene.ToLevel().Session;
            var userdata = RainbowChannelExpression.Instance.Update(position);

            return Calc.HsvToColor(hue.Get(session, userdata), s.Get(session, userdata), v.Get(session, userdata)) * alpha.Get(session, userdata);
        }
    }
}