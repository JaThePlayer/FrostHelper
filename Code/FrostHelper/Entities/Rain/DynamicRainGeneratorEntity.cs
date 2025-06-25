using FrostHelper.Components;
using FrostHelper.Helpers;

namespace FrostHelper;

[CustomEntity("FrostHelper/DynamicRainGenerator")]
internal sealed class DynamicRainGeneratorEntity : Entity 
{
    public DynamicRainGeneratorEntity(EntityData data, Vector2 offset) : base(data.Position + offset) {
        Depth = data.Int("depth");
        var genLength = data.Int("generatorLength", -1);
        if (genLength == -1) {
            // We can choose any value we like.
            // Warning: Because this value can have visible side-effects, the below value should
            //          only be changed after checking it won't change visuals (most likely, only for straight angles)
            genLength = 64;
        }
        
        var colors = data.GetColors("colors", DynamicRainGenerator.DefaultColors);

        var speedRange = data.GetVec2("speedRange", new(200f, 600f));
        var scaleRange = data.GetVec2("scaleRange", new(4f, 16f));
        var rotationRange = data.GetVec2("rotationRange", new(-2.8647888f, 2.8647888f));
        rotationRange = new(rotationRange.X.ToRad(), rotationRange.Y.ToRad());
        
        var density = data.Float("density", 0.75f);
        var isRainbow = data.Bool("rainbow", false);
        var enableCondition = data.GetCondition("enableFlag");
        var flagIfPlayerInside = data.Attr("flagIfPlayerInside");
        var preSimulationTime = data.Float("presimulationTime", 1f);
        if (preSimulationTime < 0f)
            preSimulationTime = 0f;

        var group = new DynamicRainGroup() {
            FlagIfPlayerInside = flagIfPlayerInside,
            EntityFilter = [ typeof(Player), typeof(Solid) ], // FrostModule.GetTypes(data.Attr("collideWith", "Celeste.Player,Celeste.Solid")),
        };
        Add(group);
        
        var factory = (Vector2 offset, int genLength) => new DynamicRainGenerator(genLength, density) {
            Active = true,
            Colors = colors,
            SpeedRange = speedRange,
            ScaleRange = scaleRange,
            RotationRange = rotationRange,
            EnableCondition = enableCondition,
            IsRainbow = isRainbow,
            Group = group,
            Offset = offset,
            PreSimulationTime = preSimulationTime,
        };
        
        Rectangle collisionBox = new((int)Position.X, (int)Position.Y, 8, 8);
        
        if (data.Width > 0f) {
            genLength = int.Min(genLength, data.Width);
            collisionBox.Width = data.Width;
            for (int i = 0; i < data.Width / genLength; i++) {
                var gen = factory(Vector2.UnitX * (i * genLength + (genLength/2f)), genLength);
                Add(gen);
            }
        } else {
            genLength = int.Min(genLength, data.Height);
            collisionBox.Height = data.Width;
            for (int i = 0; i < data.Height / genLength; i++) {
                var gen = factory(Vector2.UnitY * (i * genLength + (genLength/2f)), genLength);
                Add(gen);
            }
        }
        collisionBox.Inflate(1, 1);
        
        if (data.Bool("attachToSolid")) {
            Add(new StaticMover {
                SolidChecker = s => s.CollideRect(collisionBox),
                JumpThruChecker = j => j.CollideRect(collisionBox),
            });
        }
    }
}