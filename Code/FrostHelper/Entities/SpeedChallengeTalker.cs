#if SPEEDCHALLENGES
using Celeste.Mod.Entities;

namespace FrostHelper {
    [CustomEntity("FrostHelper/SpeedChallengeJournal")]
    class SpeedChallengeTalker : Trigger {
        string[] challenges;
        bool autoAddSID;


        public SpeedChallengeTalker(EntityData data, Vector2 offset) : base(data, offset) {
            Add(new TalkComponent(new Rectangle(0, 0, data.Width, data.Height), new Vector2(data.Width / 2, 0), OnTalk));
            challenges = data.Attr("challengeNames", "SpringCollab2020TimeTrial/1-Beginner>sc2020_beg_rainbowBerryRush").Split(',');
            autoAddSID = data.Bool("autoAddSid", false);
        }


        Player? talkingPlayer;
        CustomJournal journal;

        public override void Awake(Scene scene) {
            base.Awake(scene);
            if (challenges[0].StartsWith("readFromDialog:")) {
                challenges = Dialog.Clean(challenges[0].Split(':')[1]).Split(',');
            }
            if (autoAddSID) {
                for (int i = 0; i < challenges.Length; i++) {
                    challenges[i] = SceneAs<Level>().Session.Area.SID + '>' + challenges[i];
                }
            }
        }

        public void OnTalk(Player player) {
            journal = new CustomJournal();
            journal.Pages = new List<CustomJournalPage>() { new SpeedChallengePage(journal, challenges) };
            Scene.Add(journal);
            journal.Add(new Coroutine(journal.Enter()));
            talkingPlayer = player;
            player.StateMachine.State = Player.StDummy;
            Input.MenuCancel.ConsumePress();
            Input.MenuCancel.ConsumeBuffer();
        }

        public override void Update() {
            base.Update();

            if (talkingPlayer != null && Input.MenuCancel.Pressed) {
                journal.Add(new Coroutine(journal.Leave()));
                Input.Dash.ConsumePress();
                Input.Jump.ConsumePress();
                talkingPlayer.StateMachine.State = Player.StNormal;
                talkingPlayer = null;
            }
        }
    }
}
#endif