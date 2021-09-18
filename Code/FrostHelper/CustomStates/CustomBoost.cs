#if PLAYERSTATEHELPER
using Celeste;
using Celeste.Mod.PlayerStateHelper.API;
using FrostHelper.Entities.Boosters;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System.Collections;
namespace FrostHelper.CustomStates
{
    public class CustomBoost : CustomPlayerState
    {
        [OnLoad]
        public static void OnLoad()
        {
            Celeste.Mod.PlayerStateHelper.API.API.RegisterState(typeof(CustomBoost));
        }

        [OnUnload]
        public static void OnUnload()
        {
            Celeste.Mod.PlayerStateHelper.API.API.DeregisterState(typeof(CustomBoost));
        }

        GenericCustomBooster Booster;

        private static StateTags Tags = new StateTags()
        {
            StateTags.Booster,
        };

        public override void Begin()
        {
            Booster = (GenericCustomBooster)Parameters;
            Booster.HandleBoostBegin(Player);
        }

        public override void End()
        {
            Vector2 boostTarget = (Vector2)FrostModule.player_boostTarget.GetValue(Player);
            Vector2 vector = (boostTarget - Player.Collider.Center).Floor();
            Player.MoveToX(vector.X, null);
            Player.MoveToY(vector.Y, null);
            new DynData<Player>(Player).Set<GenericCustomBooster>("fh.customBooster", null);
        }

        public override string GetCelesteStudioDisplayName() => "Custom Boost";

        public override string GetID() => StateIDs.CustomBoost;

        public override StateTags GetTags() => Tags;

        public override IEnumerator Routine()
        {
            if (Booster.BoostTime > 0.25f)
            {
                yield return Booster.BoostTime - 0.25f;
                Audio.Play(Booster.boostSfx, Booster.Position);
                Booster.Flash();
                yield return 0.25f;
            }
            else
            {
                yield return Booster.BoostTime;
            }

            Player.SetState(Booster.Red ? StateIDs.CustomRedBoost : "Dash");
            yield break;
        }

        public override string Update()
        {
            Vector2 boostTarget = (Vector2)FrostModule.player_boostTarget.GetValue(Player);
            Vector2 value = Input.Aim.Value * 3f;
            Vector2 vector = Calc.Approach(Player.ExactPosition, boostTarget - Player.Collider.Center + value, 80f * Engine.DeltaTime);
            Player.MoveToX(vector.X, null);
            Player.MoveToY(vector.Y, null);

            // check for fastbubble
            if ((Input.Dash.Pressed || Input.CrouchDashPressed) && Booster.CanFastbubble())
            {
                Player.SetValue("demoDashed", Input.CrouchDashPressed);
                Input.Dash.ConsumePress();
                Input.CrouchDash.ConsumePress();
                return Booster.Red ? StateIDs.CustomRedBoost : "Dash";
            }

            return StateIDs.CustomBoost;
        }
    }
}
#endif