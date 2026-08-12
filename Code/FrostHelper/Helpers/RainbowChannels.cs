using FrostHelper.ModIntegration;
using FrostHelper.SessionExpressions;
using OpCodes = System.Reflection.Emit.OpCodes;

namespace FrostHelper.Helpers;

internal static class RainbowChannels {
    public const string RainbowChannelEntityDataKey = "fh_rc";
    
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

        IL.Celeste.Level.Update += EntityListUpdate;
        IL.Monocle.EntityList.UpdateLists += EntityListUpdateLists; 
        
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
        cursor.Emit(Mono.Cecil.Cil.OpCodes.Dup);
        cursor.Emit(Mono.Cecil.Cil.OpCodes.Call, typeof(Entity).GetProperty(nameof(Entity.Scene))!.GetMethod!);
        cursor.EmitCall(BeforeEntityUpdate);
    }
    
    private static void EntityListUpdate(ILContext il) {
        var cursor = new ILCursor(il);

        while (cursor.TryGotoNext(MoveType.Before, i => i.MatchCallOrCallvirt<Entity>(nameof(Entity.Update)))) {
            cursor.Emit(Mono.Cecil.Cil.OpCodes.Dup);
            cursor.Emit(Mono.Cecil.Cil.OpCodes.Call, typeof(Entity).GetProperty(nameof(Entity.Scene))!.GetMethod!);
            cursor.EmitCall(BeforeEntityUpdate);

            cursor.GotoNext(MoveType.After, i => i.MatchCallOrCallvirt<Entity>(nameof(Entity.Update)));
        }
    }
    
    private static void EntityListUpdateLists(ILContext il) {
        var cursor = new ILCursor(il);

        cursor.TryGotoNext(MoveType.Before, i => i.MatchCallOrCallvirt<Entity>(nameof(Entity.Awake)));
        // Stack is [Entity, Scene]
        VariableDefinition sceneLocal = new VariableDefinition(il.Import(typeof(Scene)));
        il.Body.Variables.Add(sceneLocal);
        cursor.Emit(Mono.Cecil.Cil.OpCodes.Stloc, sceneLocal);
        cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldloc, sceneLocal);
        cursor.EmitCall(BeforeEntityUpdate);
        cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldloc, sceneLocal);
    }

    internal static void SetEntityChannel(Entity entity, string channelId) {
        entity.SourceData ??= new EntityData();
        entity.SourceData.Values ??= new Dictionary<string, object>();
        entity.SourceData.Values[RainbowChannelEntityDataKey] = channelId;
    }
    
    private static Entity BeforeEntityUpdate(Entity entity, Scene scene) {
        if (entity.SourceData?.Attr(RainbowChannelEntityDataKey) is { } channel ) {
            if (!string.IsNullOrWhiteSpace(channel) && ControllerHelper<RainbowChannelController>.FindFirst(scene, _ => true) is { } controller) {
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
        IL.Celeste.Level.Update -= EntityListUpdate;
        IL.Monocle.EntityList.UpdateLists -= EntityListUpdateLists;
        
        On.Celeste.CrystalStaticSpinner.GetHue -= CrystalStaticSpinnerOnGetHue;
    }

    #endregion
    

    public static RainbowChannel? CurrentChannel { get; private set; }
}

[Tracked]
internal sealed class RainbowChannelController : Entity {
    private Dictionary<string, RainbowChannel> _channels = [];

    public IReadOnlyDictionary<string, RainbowChannel> Channels => _channels;

    public RainbowChannelController() {
        Tag |= Tags.TransitionUpdate | Tags.Persistent;
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

    public RainbowChannelExpression Update(Vector2 pos) {
        X = pos.X;
        Y = pos.Y;
        return this;
    }
    
    sealed class GetX : ConditionHelper.Condition {
        public override object Get(Session session, object? userdata) {
            return ((RainbowChannelExpression) userdata).X;
        }

        internal override void Emit(ConditionCompilationCtx ctx, Type targetType) {
            var il = ctx.Il;
            
            ctx.EmitLoadUserdata<RainbowChannelExpression>();
            il.Emit(OpCodes.Callvirt, typeof(RainbowChannelExpression).GetProperty(nameof(X))!.GetMethod!);
            il.EmitConvertToInSessionExpression(typeof(float), targetType);
        }

        internal override bool UsesCurrentConditionLocalInEmit => false;

        protected internal override Type ReturnType => typeof(float);
    }
    
    sealed class GetY : ConditionHelper.Condition {
        public override object Get(Session session, object? userdata) {
            return ((RainbowChannelExpression) userdata).Y;
        }
        
        internal override void Emit(ConditionCompilationCtx ctx, Type targetType) {
            var il = ctx.Il;
            
            ctx.EmitLoadUserdata<RainbowChannelExpression>();
            il.Emit(OpCodes.Callvirt, typeof(RainbowChannelExpression).GetProperty(nameof(Y))!.GetMethod!);
            il.EmitConvertToInSessionExpression(typeof(float), targetType);
        }

        internal override bool UsesCurrentConditionLocalInEmit => false;

        protected internal override Type ReturnType => typeof(float);
    }
        
    sealed class GetPos : ConditionHelper.Condition {
        public override object Get(Session session, object? userdata) {
            return ((RainbowChannelExpression) userdata).Pos;
        }

        protected internal override Type ReturnType => typeof(Vector2);
        
        internal override void Emit(ConditionCompilationCtx ctx, Type targetType) {
            var il = ctx.Il;
            
            ctx.EmitLoadUserdata<RainbowChannelExpression>();
            il.Emit(OpCodes.Callvirt, typeof(RainbowChannelExpression).GetProperty(nameof(Pos))!.GetMethod!);
            il.EmitConvertToInSessionExpression(typeof(Vector2), targetType);
        }

        internal override bool UsesCurrentConditionLocalInEmit => false;
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

    public override void Added(Scene scene) {
        base.Added(scene);
        
        ControllerHelper<RainbowChannelController>.AddToSceneIfNeeded(scene).Register(CreateChannel());
    }

    public abstract RainbowChannel CreateChannel();
}
