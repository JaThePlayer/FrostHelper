using Celeste;
using FrostHelper.Entities.Boosters;
using MonoMod.Utils;

#if PLAYERSTATEHELPER
using Celeste.Mod.PlayerStateHelper.API;
using FrostHelper.CustomStates;
#endif

namespace FrostHelper.API
{
    public static class API
    {
        public static void SetCustomBoostState(Player player, GenericCustomBooster booster)
        {
            new DynData<Player>(player).Set("fh.customBooster", booster);
#if PLAYERSTATEHELPER
            player.SetState(StateIDs.CustomBoost, booster);
#else
            player.StateMachine.State = GenericCustomBooster.CustomBoostState;
#endif
        }

        public static bool IsInCustomBoostState(Player player)
        {
#if PLAYERSTATEHELPER
            return player.IsInState(StateIDs.CustomBoost);
#else
            return player.StateMachine.State == GenericCustomBooster.CustomBoostState;
#endif
        }
    }
}
