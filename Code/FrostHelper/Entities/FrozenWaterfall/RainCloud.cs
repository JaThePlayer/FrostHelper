using FrostHelper.Components;
using FrostHelper.Helpers;

namespace FrostHelper.Entities.FrozenWaterfall;

[CustomEntity("FrostHelper/Raincloud")]
internal sealed class RainCloud : Cloud {
    internal Color Color { get; }

    private readonly DynamicRainGenerator _generator;
    
    public RainCloud(EntityData data, Vector2 offset) : base(data, offset) {
        Depth = data.Int("depth", -9000);
        Small = data.Bool("small");
        Color = data.GetColor("color", "LightSkyBlue");
        var rainCfg = data.Parse("rain", RainConfig.Default);

        var group = new DynamicRainGroup {
            //FlagIfPlayerInside = flagIfPlayerInside,
            OnPlayer = DynamicWaterBehaviorController.OnPlayerTouchedRain,
            EntityFilter = FrostModule.GetTypes(rainCfg.CollideWith),
            WindMultiplier = rainCfg.WindMultiplier,
        };
        Add(group);
        
        _generator = new DynamicRainGenerator(Small.Value ? 16 : 26, rainCfg.Density) {
            Active = true,
            Colors = rainCfg.Colors,
            SpeedRange = rainCfg.SpeedRange,
            ScaleRange = rainCfg.ScaleRange,
            RotationRange = rainCfg.RotationRange,
            EnableCondition = ConditionHelper.CreateOrDefault(rainCfg.EnableFlag, "1"),
            IsRainbow = rainCfg.Rainbow,
            Group = group,
            Offset = new Vector2(0f, 10f),
            PreSimulationTime = rainCfg.PresimulationTime,
            Alpha = rainCfg.Opacity,
        };
        
        Add(_generator);
    }

    public override void Added(Scene scene) {
        base.Added(scene);
        sprite.Color = Color;
    }

    public override void Update() {
        _generator.Enabled = Collidable;
        base.Update();
        _generator.Enabled = Collidable;

        if (GetPlayerRider() is { } player) {
            DynamicWaterBehaviorController.OnPlayerTouchedRain(player, Color);
        }
    }
}