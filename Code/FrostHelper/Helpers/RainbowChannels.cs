using FrostHelper.ModIntegration;
using FrostHelper.SessionExpressions;

namespace FrostHelper.Helpers;

internal static class RainbowChannels {
    #region Hooks

    private static bool _hooksEnabled;
    
    public static void LoadHooksIfNeeded() {
        if (_hooksEnabled)
            return;
        _hooksEnabled = true;
        
        IL.Monocle.EntityList.Render += EntityListRender;
        IL.Monocle.EntityList.RenderExcept += EntityListRender;
        IL.Monocle.EntityList.RenderOnly += EntityListRender;
        IL.Monocle.EntityList.RenderOnlyFullMatch += EntityListRender;
        IL.Monocle.EntityList.Update += EntityListUpdate;
        
        On.Celeste.CrystalStaticSpinner.GetHue += CrystalStaticSpinnerOnGetHue;
    }

    private static Color CrystalStaticSpinnerOnGetHue(On.Celeste.CrystalStaticSpinner.orig_GetHue orig, CrystalStaticSpinner self, Vector2 position) {
        if (CurrentChannel is { } channel) {
            return channel.GetColor(self.Scene, position);
        }
        
        return orig(self, position);
    }

    private static void EntityListRender(ILContext il) {
        var cursor = new ILCursor(il);

        cursor.TryGotoNext(MoveType.Before, i => i.MatchCallOrCallvirt<Entity>(nameof(Entity.Render)));
        cursor.EmitCall(BeforeEntityRender);
    }
    
    private static void EntityListUpdate(ILContext il) {
        var cursor = new ILCursor(il);

        cursor.TryGotoNext(MoveType.Before, i => i.MatchCallOrCallvirt<Entity>(nameof(Entity.Update)));
        cursor.EmitCall(BeforeEntityUpdate);
    }

    private static Entity BeforeEntityRender(Entity entity) {
        if (entity.SourceData?.Attr("fh_rc") is { } channel ) {
            if (!string.IsNullOrWhiteSpace(channel) && entity.Scene.Tracker.GetEntity<RainbowChannelController>() is { } controller) {
                CurrentChannel = controller.GetChannel(channel);
            } else {
                CurrentChannel = null;
            }
        } else {
            CurrentChannel = null;
        }
        
        return entity;
    }
    
    private static Entity BeforeEntityUpdate(Entity entity) {
        if (entity.SourceData?.Attr("fh_rc") is { } channel ) {
            if (!string.IsNullOrWhiteSpace(channel) && entity.Scene.Tracker.GetEntity<RainbowChannelController>() is { } controller) {
                CurrentChannel = controller.GetChannel(channel);
            } else {
                CurrentChannel = null;
            }
        } else {
            CurrentChannel = null;
        }
        
        return entity;
    }

    [OnUnload]
    public static void UnloadHooks() {
        if (!_hooksEnabled)
            return;
        _hooksEnabled = false;
        
        IL.Monocle.EntityList.Render -= EntityListRender;
        IL.Monocle.EntityList.RenderExcept -= EntityListRender;
        IL.Monocle.EntityList.RenderOnly -= EntityListRender;
        IL.Monocle.EntityList.RenderOnlyFullMatch -= EntityListRender;
        IL.Monocle.EntityList.Update -= EntityListUpdate;
        
        On.Celeste.CrystalStaticSpinner.GetHue -= CrystalStaticSpinnerOnGetHue;
    }

    #endregion
    

    public static RainbowChannel? CurrentChannel { get; private set; }
}

[Tracked]
internal sealed class RainbowChannelController : Entity {
    private Dictionary<string, RainbowChannel> _channels = [];

    public RainbowChannelController() {
        RainbowChannels.LoadHooksIfNeeded();
    }
    
    public void Register(RainbowChannel channel) {
        _channels[channel.ChannelId] = channel;
    }

    public RainbowChannel? GetChannel(string id) {
        return _channels.GetValueOrDefault(id);
    }
}

internal sealed class RainbowChannelExpression : ISavestatePersisted {
    public float X { get; set; }
    
    public float Y { get; set; }

    public Vector2 Pos => new Vector2(X, Y);
    
    public static ExpressionContext ExpressionContext { get; } = new ExpressionContext(new() {
        ["x"] = new GetX(),
        ["y"] = new GetY(),
        ["pos"] = new GetPos(),
    }, []);

    public static RainbowChannelExpression Instance { get; } = new RainbowChannelExpression();

    public RainbowChannelExpression Update(Scene scene, Vector2 pos) {
        X = pos.X;
        Y = pos.Y;
        return this;
    }
    
    sealed class GetX : ConditionHelper.Condition {
        public override object Get(Session session, object? userdata) {
            return ((RainbowChannelExpression) userdata).X;
        }

        protected internal override Type ReturnType => typeof(float);
    }
    
    sealed class GetY : ConditionHelper.Condition {
        public override object Get(Session session, object? userdata) {
            return ((RainbowChannelExpression) userdata).Y;
        }

        protected internal override Type ReturnType => typeof(float);
    }
        
    sealed class GetPos : ConditionHelper.Condition {
        public override object Get(Session session, object? userdata) {
            return ((RainbowChannelExpression) userdata).Pos;
        }

        protected internal override Type ReturnType => typeof(Vector2);
    }
}

internal abstract class RainbowChannel : Entity {

    
    public required string ChannelId { get; init; }

    public abstract Color GetColor(Scene scene, Vector2 position);
}

internal abstract class RainbowChannelSource : Entity {
    public string ChannelId { get; }
    
    public RainbowChannelSource(EntityData data, Vector2 offset) : base(data.Position + offset) {
        ChannelId = data.Attr("channelId");
        Active = false;
        Visible = false;
    }

    public override void Awake(Scene scene) {
        base.Awake(scene);
        
        ControllerHelper<RainbowChannelController>.AddToSceneIfNeeded(scene).Register(CreateChannel());
    }

    public abstract RainbowChannel CreateChannel();
}
