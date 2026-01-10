using FrostHelper.Helpers;
using FrostHelper.Triggers.Flags;

namespace FrostHelper.Entities.FrozenWaterfall;

[CustomEntity("FrostHelper/DynamicWaterBehaviorController")]
[Tracked]
internal sealed class DynamicWaterBehaviorController : Entity {
    private readonly Dictionary<RgbaOrXnaColor, List<SequencerCommand>> _behaviors;
    private readonly Dictionary<RgbaOrXnaColor, List<SequencerCommand>> _rainBehaviors;

    private Dictionary<RgbaOrXnaColor, List<SequencerCommand>> ParseBehaviors(string behaviors) {
        var parser = new SpanParser(behaviors);
        if (!parser.TryParseDictWithRepeats(';', '~', out Dictionary<RgbaOrXnaColor, List<SequencerCommand>> parsed, out var errorMsg)) {
            NotificationHelper.Notify(errorMsg);
            parsed = [];
        }

        return parsed;
    }
    
    public DynamicWaterBehaviorController(EntityData data, Vector2 offset) : base(data.Position + offset) {
        Active = false;
        Visible = false;

        _behaviors = ParseBehaviors(data.Attr("behaviors"));
        _rainBehaviors = ParseBehaviors(data.Attr("rainBehaviors"));
    }

    public void HandleBehaviorFor(Player player, Color color) {
        if (_behaviors.TryGetValue(new RgbaOrXnaColor(color), out var behaviors)) {
            foreach (var behavior in behaviors)
                behavior.Execute(player.Scene.ToLevel(), null, player);
        }
    }
    
    public void HandleRainBehaviorFor(Player player, Color color) {
        if (_rainBehaviors.TryGetValue(new RgbaOrXnaColor(color), out var behaviors)) {
            foreach (var behavior in behaviors)
                behavior.Execute(player.Scene.ToLevel(), null, player);
        }
    }
    
    internal static void OnPlayerTouchedRain(Player player, Color color) {
        if (player.Scene.Tracker.SafeGetEntity<DynamicWaterBehaviorController>() is {} controller)
            controller.HandleRainBehaviorFor(player, color);
    }
}