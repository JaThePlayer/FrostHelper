using FrostHelper.Helpers;
using FrostHelper.ModIntegration;

namespace FrostHelper;

[CustomEntity("FrostHelper/GradientRainbowChannel")]
internal sealed class GradientRainbowChannelSource : RainbowChannelSource {
    private readonly Color[] _colors;
    private readonly SessionExpression<float> _gradientSize;
    private readonly SessionExpression<float> _gradientSpeed;
    private readonly SessionExpression<Vector2> _gradientCenter;
    private readonly bool _loopColors;
    
    
    public GradientRainbowChannelSource(EntityData data, Vector2 offset) : base(data, offset) {
        _colors = data.GetColors("colors", [], separator: ';');
        _gradientSpeed = data.GetExpression<float>(RainbowChannelExpression.ExpressionContext, "speed");
        _gradientSize = data.GetExpression<float>(RainbowChannelExpression.ExpressionContext, "size");
        _gradientCenter = data.GetExpression<Vector2>(RainbowChannelExpression.ExpressionContext, "center");
        _loopColors = data.Bool("loopColors");
    }

    public override RainbowChannel CreateChannel() {
        return new GradientRainbowChannel(_colors, _gradientSize, _gradientSpeed, _gradientCenter, _loopColors) { ChannelId = ChannelId };
    }

    sealed class GradientRainbowChannel(Color[] colors, SessionExpression<float> gradientSize, SessionExpression<float> gradientSpeed, SessionExpression<Vector2> gradientCenter, bool loopColors) : RainbowChannel, ISavestatePersisted {
        public override Color GetColor(Scene scene, Vector2 position) {
            if (colors.Length == 0)
                return Color.White;
            if (colors.Length == 1)
                return colors[0];
            
            var session = scene.ToLevel().Session;
            var userdata = RainbowChannelExpression.Instance.Update(position);

            // https://github.com/maddie480/MaddieHelpingHand/blob/master/Entities/RainbowSpinnerColorController.cs
            
            var size = gradientSize.Get(session, userdata);
            var speed = gradientSpeed.Get(session, userdata);
            var center = gradientCenter.Get(session, userdata);
            
            var p = scene.TimeActive * speed + Vector2.Distance(position, center);
            while (p < 0) {
                p += size;
            }
            p = p % size / size;

            if (!loopColors) {
                p = Calc.YoYo(p);
            }

            if (p >= 1f)
                return colors[^1];

            p *= colors.Length - 1;
            int index = (int) p;
            
            return Color.Lerp(colors[index], colors[index + 1], p - index);
        }
    }
}