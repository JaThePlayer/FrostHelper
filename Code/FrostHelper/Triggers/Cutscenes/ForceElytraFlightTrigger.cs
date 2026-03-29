using Celeste.Mod.CommunalHelper;
using Celeste.Mod.CommunalHelper.States;
using FrostHelper.Helpers;

namespace FrostHelper.Triggers.Cutscenes;

// TODO: PR this to Communal Helper!
[CustomEntity("FrostHelper/ForceElytraFlightTrigger")]
[Tracked]
internal sealed class ForceElytraFlightTrigger : Trigger {
    private readonly Vector2 _speed;
    private readonly Vector2 _aim;
    
    public ForceElytraFlightTrigger(EntityData data, Vector2 offset) : base(data, offset) {
        _speed = data.GetVec2("speed", Vector2.Zero);
        _aim = data.GetVec2("aim", Vector2.Zero);
    }

    public override void OnEnter(Player player) {
        base.OnEnter(player);


        player.Speed = _speed;
        var forcedElytra = ForcedElytra.EnforceElytraFor(player);
        forcedElytra.Aim = _aim;
    }
}

internal sealed record ForcedElytra : IAttachable {
    #region Hooks
    private static bool _hooksLoaded;

    // [HookPreload] DON'T PRELOAD THIS, we don't know if Communal Helper is loaded yet!
    internal static void LoadHooksIfNeeded() {
        if (_hooksLoaded) {
            return;
        }
        _hooksLoaded = true;
        
        Everest.Events.Player.OnBeforeUpdate += PlayerOnBeforeUpdate;
        Everest.Events.Player.OnAfterUpdate += PlayerOnAfterUpdate;
    }
    
    [OnUnload]
    internal static void Unload() {
        if (!_hooksLoaded) {
            return;
        }
        _hooksLoaded = false;
        
        Everest.Events.Player.OnBeforeUpdate -= PlayerOnBeforeUpdate;
        Everest.Events.Player.OnAfterUpdate -= PlayerOnAfterUpdate;
    }

    private static void PlayerOnBeforeUpdate(Player player) {
        if (player.GetDynamicDataAttached<ForcedElytra>() is not { } forcedElytra) {
            return;
        }

        CommunalHelperModule.Settings.ElytraMode = CommunalHelperSettings.ElytraModes.Hold;
        Input.Feather.Value = forcedElytra.Aim;
        forcedElytra.CurrentlyChangingInputs = true;
    }
    
    private static void PlayerOnAfterUpdate(Player player) {
        if (player.GetDynamicDataAttached<ForcedElytra>() is not { } forcedElytra) {
            return;
        }

        forcedElytra.CurrentlyChangingInputs = false;
        CommunalHelperModule.Settings.ElytraMode = (CommunalHelperSettings.ElytraModes)forcedElytra.OrigMode;

        if (player.onGround || player.StateMachine.State != St.Elytra) {
            player.SetDynamicDataAttached<ForcedElytra>(null);
        }
    }
    #endregion
        
    public int OrigMode { get; set; } = (int)CommunalHelperModule.Settings.ElytraMode;
    
    public Vector2 Aim { get; set; }
    
    internal bool CurrentlyChangingInputs { get; private set; }

    public static ForcedElytra EnforceElytraFor(Player player) {
        LoadHooksIfNeeded();

        if (player.GetDynamicDataAttached<ForcedElytra>() is { } forcedElytra) {
            return forcedElytra;
        }
        
        player.StateMachine.State = St.Elytra;
        var elytra = new ForcedElytra();
        player.SetDynamicDataAttached(elytra);
        if (!CommunalHelperModule.Settings.DeployElytra.Button.Nodes.OfType<ControlledNode>().Any()) {
            CommunalHelperModule.Settings.DeployElytra.Button.Nodes.Add(new ControlledNode());
        }

        return elytra;
    }
        
    internal sealed class ControlledNode : VirtualButton.Node {
        private static bool IsForceEnabled() {
            if (Engine.Scene?.Tracker?.SafeGetEntities<Player>() is { } players) {
                foreach (Player player in players) {
                    if (player.GetDynamicDataAttached<ForcedElytra>() is { CurrentlyChangingInputs: true })
                        return true;
                }
            }
            
            return false;
        }

        public override bool Check => IsForceEnabled();
        public override bool Pressed => IsForceEnabled();
        public override bool Released => !IsForceEnabled();
    }
}