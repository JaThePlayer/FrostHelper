namespace FrostHelper;

public class FrostHelperSaveData : EverestModuleSaveData {
    /// <summary>
    /// SID.ChallengeName -> BestTime(Ticks)
    /// </summary>
    public Dictionary<string, long> ChallengeTimes = new Dictionary<string, long>();

    public long GetChallengeTime(string challengeNameWithSID) {
        if (ChallengeTimes.ContainsKey(challengeNameWithSID)) {
            return ChallengeTimes[challengeNameWithSID];
        } else {
            return -1;
        }
    }

    public void SetChallengeTime(string SID, string challengeName, long ticks) {
        if (ChallengeTimes == null)
            ChallengeTimes = new Dictionary<string, long>();
        string name = SID + '>' + challengeName;
        if (ChallengeTimes.ContainsKey(name)) {
            if (ticks < ChallengeTimes[name]) {
                ChallengeTimes[name] = ticks;
                FrostModule.Instance.WriteSaveData(Index, FrostModule.Instance.SerializeSaveData(Index));
            }
        } else {
            ChallengeTimes.Add(name, ticks);
            FrostModule.Instance.WriteSaveData(Index, FrostModule.Instance.SerializeSaveData(Index));
        }
    }

    public bool IsChallengeBeaten(string SID, string challengeName, long timeLimit) {
        string name = SID + '>' + challengeName;
        if (ChallengeTimes == null) {
            ChallengeTimes = new Dictionary<string, long>();
            return false;
        }

        return ChallengeTimes.ContainsKey(name) && timeLimit > ChallengeTimes[name];
    }
}
