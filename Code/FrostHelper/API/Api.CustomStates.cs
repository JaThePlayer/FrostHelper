using FrostHelper.Entities.Boosters;
using FrostHelper.Helpers;

namespace FrostHelper.API;

// [ModExportName("FrostHelper")] - defined in API.cs
public partial class API {
    public static void SetCustomBoostState(Player player, GenericCustomBooster booster) {
        player.SetAttached(booster);
        player.StateMachine.State = GenericCustomBooster.CustomBoostState;
    }

    public static bool IsInCustomBoostState(Player player) {
        return player.StateMachine.State == GenericCustomBooster.CustomBoostState;
    }

    public static int GetCustomFeatherStateId() => CustomFeather.CustomFeatherState;
    public static int GetCustomBoostStateId() => GenericCustomBooster.CustomBoostState;
    public static int GetCustomRedBoostStateId() => GenericCustomBooster.CustomRedBoostState;
    public static int GetHeldDashStateId() => HeldRefill.HeldDashState;
    
    public static void SubscribeToCustomBoostBegin(Action<Player, Entity> onBegin)
        => GenericCustomBooster.OnBoostBegin += onBegin;
    
    public static void UnsubscribeToCustomBoostBegin(Action<Player, Entity> onBegin)
        => GenericCustomBooster.OnBoostBegin -= onBegin;
}