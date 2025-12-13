using FrostHelper.Components;
using FrostHelper.Helpers;

namespace FrostHelper.Entities.FrozenWaterfall;

[CustomEntity("FrostHelper/Raincloud")]
internal sealed class RainCloud : Cloud {
    internal Color Color { get; }

    private readonly DynamicRainGenerator _generator;
    
    public RainCloud(EntityData data, Vector2 offset) : base(data, offset) {
        Small = data.Bool("small");
        Color = data.GetColor("color", "LightSkyBlue");
        
        var opacity = data.Float("opacity", 1f);

        var speedRange = data.GetVec2("speedRange", new(200f, 600f));
        var scaleRange = data.GetVec2("scaleRange", new(4f, 16f));
        var rotationRange = data.GetVec2("rotationRange", new(0f));
        rotationRange = new(rotationRange.X.ToRad(), rotationRange.Y.ToRad());
        
        var preSimulationTime = data.Float("presimulationTime", 1f);
        if (preSimulationTime < 0f)
            preSimulationTime = 0f;
        var density = data.Float("density", 0.75f);

        var group = new DynamicRainGroup {
            //FlagIfPlayerInside = flagIfPlayerInside,
            OnPlayer = DynamicWaterBehaviorController.OnPlayerTouchedRain,
            EntityFilter = FrostModule.GetTypes(data.Attr("collideWith", "Celeste.Player,Celeste.Solid")),
        };
        Add(group);
        
        _generator = new DynamicRainGenerator(Small.Value ? 16 : 26, density) {
            Active = true,
            Colors = [ Color ],
            SpeedRange = speedRange,
            ScaleRange = scaleRange,
            RotationRange = rotationRange,
            EnableCondition = ConditionHelper.TrueCondition,
            IsRainbow = false,
            Group = group,
            Offset = new Vector2(0f, 10f),
            PreSimulationTime = preSimulationTime,
            Alpha = opacity,
        };
        
        Add(_generator);
    }

    public override void Added(Scene scene) {
        base.Added(scene);
        sprite.Color = Color;
    }

    public override void Update() {
        _generator.EnableCondition = Collidable ? ConditionHelper.TrueCondition : ConditionHelper.FalseCondition;
        base.Update();
        _generator.EnableCondition = Collidable ? ConditionHelper.TrueCondition : ConditionHelper.FalseCondition;

        if (GetPlayerRider() is { } player) {
            DynamicWaterBehaviorController.OnPlayerTouchedRain(player, Color);
        }
    }
}