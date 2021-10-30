using Celeste;
using FrostHelper.Entities.Boosters;
using Monocle;
using MonoMod.Utils;

#if PLAYERSTATEHELPER
using Celeste.Mod.PlayerStateHelper.API;
using FrostHelper.CustomStates;
#endif

namespace FrostHelper.API {
    public static class API {
        public static int Version => 1;

#if FAILED_INCREASE_LIGHT_LIMIT
        /// <summary>
        /// The key for the session counter containing the current light limit, as long as the limit got changed this session
        /// </summary>
        public static string LightLimitCounter => "FH.LightLimit";

        public static int GetLightLimit() => 256;//GetLightLimit(FrostModule.GetCurrentLevel());

        public static int GetLightLimit(Level level) => GetLightLimit(level.Session);

        public static int GetLightLimit(Session session) {
            var limit = session.GetCounter(LightLimitCounter);

            //return limit == 0 ? 64 : limit;
            return 128;
        }
#endif

        public static void SetCustomBoostState(Player player, GenericCustomBooster booster) {
            new DynData<Player>(player).Set("fh.customBooster", booster);
#if PLAYERSTATEHELPER
            player.SetState(StateIDs.CustomBoost, booster);
#else
            player.StateMachine.State = GenericCustomBooster.CustomBoostState;
#endif
        }

        public static bool IsInCustomBoostState(Player player) {
#if PLAYERSTATEHELPER
            return player.IsInState(StateIDs.CustomBoost);
#else
            return player.StateMachine.State == GenericCustomBooster.CustomBoostState;
#endif
        }
    }
}
