using FrostHelper.Components;
using FrostHelper.Helpers;

namespace FrostHelper;

[CustomEntity("FrostHelper/RainbowChannelAttacher")]
internal sealed class RainbowChannelAttacher : Entity {
    private readonly EntityFilter _filter;
    private readonly string _channel;
    
    public RainbowChannelAttacher(EntityData data, Vector2 offset) : base(data.Position + offset) {
        Add(new PostAwakeHook(PostAwake));
        _filter = EntityFilter.CreateFrom(data);
        _channel = data.Attr("channelId");
    }

    private void PostAwake() {
        foreach (var entity in _filter.Filter(Scene)) {
            RainbowChannels.SetEntityChannel(entity, _channel);
        }
    }
}