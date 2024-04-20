using FrostHelper.Entities;

namespace FrostHelper.Triggers;

internal abstract class BaseArbitraryShapeCloudEditTrigger : Trigger {
    public readonly string CloudTag;

    protected BaseArbitraryShapeCloudEditTrigger(EntityData data, Vector2 offset) : base(data, offset)
    {
        CloudTag = data.Attr("cloudTag");
    }

    public sealed override void OnEnter(Player player) {
        base.OnEnter(player);

        foreach (var en in player.Scene.Tracker.SafeGetEntities<ArbitraryShapeCloud>()) {
            if (en is ArbitraryShapeCloud cloud && cloud.CloudTags.Contains(CloudTag)) {
                EditCloud(cloud);
            }
        }
    }

    public abstract void EditCloud(ArbitraryShapeCloud cloud);
}

[CustomEntity("FrostHelper/ArbitraryShapeCloudEditColor")]
internal sealed class ArbitraryShapeCloudEditColorTrigger : BaseArbitraryShapeCloudEditTrigger {
    private readonly Color NewColor;
    private readonly float Duration;
    private readonly Ease.Easer Easer;
    
    public ArbitraryShapeCloudEditColorTrigger(EntityData data, Vector2 offset) : base(data, offset) {
        NewColor = data.GetColor("color", "ffffff");
        Duration = data.Float("duration", 0f);
        Easer = data.Easing("easing", Ease.Linear);
    }

    public override void EditCloud(ArbitraryShapeCloud cloud) {
        cloud.Rainbow = false;
        
        if (Duration <= 0f) {
            cloud.Color = NewColor;
            return;
        }
        
        var tween = Tween.Create(Tween.TweenMode.Oneshot, Easer, Duration);
        var from = cloud.Color;
        tween.OnUpdate = t => {
            var lerped = Color.Lerp(from, NewColor, MathHelper.Clamp(t.Eased, 0f, 1f));

            cloud.Color = lerped;
        };
        tween.Start();
        cloud.Add(tween);
        cloud.Active = true;
    }
}

[CustomEntity("FrostHelper/ArbitraryShapeCloudEditRainbow")]
internal sealed class ArbitraryShapeCloudEditRainbowTrigger : BaseArbitraryShapeCloudEditTrigger {
    private readonly bool Value;
    
    public ArbitraryShapeCloudEditRainbowTrigger(EntityData data, Vector2 offset) : base(data, offset) {
        Value = data.Bool("enable");
    }

    public override void EditCloud(ArbitraryShapeCloud cloud) {
        cloud.Rainbow = Value;
    }
}