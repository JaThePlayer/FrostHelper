using FrostHelper.Components;
using FrostHelper.Helpers;

namespace FrostHelper;

[CustomEntity("FrostHelper/RainbowChannelAttacher")]
internal sealed class RainbowChannelAttacher : Entity {
    private readonly EntityFilter _filter;
    private readonly string _channel;
    
    public RainbowChannelAttacher(EntityData data, Vector2 offset) : base(data.Position + offset) {
        _filter = EntityFilter.CreateFrom(data);
        _channel = data.Attr("channelId");
        
        Add(new AnyEntityAddedHook(OtherEntityAdded) {
            CatchUpToPreviouslyAdded = true,
        });
    }

    private void OtherEntityAdded(Entity entity, Scene scene) {
        if (_filter.Matches(entity)) {
            RainbowChannels.SetEntityChannel(entity, _channel);
        }
    }
}
