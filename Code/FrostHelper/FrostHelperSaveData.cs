namespace FrostHelper;

public class FrostHelperSaveData : EverestModuleSaveData {
    #region Speed Ring Challenges

    /// <summary>
    /// SID.ChallengeName -> BestTime(Ticks)
    /// </summary>
    public Dictionary<string, long> ChallengeTimes { get; set; } = [];

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
                Save();
            }
        } else {
            Save();
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
    #endregion
    
    #region Timers

    /// <summary>
    /// SID.TimerId -> BestTime
    /// </summary>
    public Dictionary<string, float> TimerPersonalBests { get; set; } = [];

    private static string GetTimerId(string sid, string name) => sid + '>' + name;
    
    internal float? GetTimerBestInCurrentMap(string timerId)
        => GetTimerBest(FrostModule.GetCurrentLevel().Session.Area.SID, timerId);
    
    internal float? GetTimerBest(string sid, string timerId) {
        return TimerPersonalBests.TryGetValue(GetTimerId(sid, timerId), out var time) ? time : null; 
    }

    internal void SetTimerBestInCurrentMap(string timerId, float time) {
        var id = GetTimerId(FrostModule.GetCurrentLevel().Session.Area.SID, timerId);
        
        TimerPersonalBests[id] = time;
        Save();
    }
    #endregion


    private void Save() {
        FrostModule.Instance.WriteSaveData(Index, FrostModule.Instance.SerializeSaveData(Index));
    }
}
