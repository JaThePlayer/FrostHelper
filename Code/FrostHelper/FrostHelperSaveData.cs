namespace FrostHelper;

public class FrostHelperSaveData : EverestModuleSaveData {
    /// <summary>
    /// SID.ChallengeName -> BestTime(Ticks)
    /// </summary>
    public Dictionary<string, long> ChallengeTimes = new Dictionary<string, long>();

    private static string GetChallengeId(string sid, string name) => sid + '>' + name;
    
    public long GetChallengeTime(string challengeNameWithSid) {
        return ChallengeTimes.GetValueOrDefault(challengeNameWithSid, -1);
    }
    
    public long GetChallengeTime(string sid, string challengeName) {
        return ChallengeTimes.GetValueOrDefault(GetChallengeId(sid, challengeName), -1);
    }

    public void SetChallengeTime(string sid, string challengeName, long ticks) {
        ChallengeTimes ??= new();
        
        string name = GetChallengeId(sid, challengeName);
        if (!ChallengeTimes.TryAdd(name, ticks)) {
            if (ticks < ChallengeTimes[name]) {
                ChallengeTimes[name] = ticks;
                FrostModule.Instance.WriteSaveData(Index, FrostModule.Instance.SerializeSaveData(Index));
            }
        } else {
            FrostModule.Instance.WriteSaveData(Index, FrostModule.Instance.SerializeSaveData(Index));
        }
    }

    public bool IsChallengeBeaten(string sid, string challengeName, long timeLimit) {
        string name = GetChallengeId(sid, challengeName);
        if (ChallengeTimes == null) {
            ChallengeTimes = new Dictionary<string, long>();
            return false;
        }

        return ChallengeTimes.ContainsKey(name) && timeLimit > ChallengeTimes[name];
    }
}
