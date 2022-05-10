namespace FrostHelper; 

public class FrostMapDataProcessor : EverestMapDataProcessor {
    public struct SpeedChallengeInfo {
        public EntityID ID;
        public float GoalTime;
    }


    public static Dictionary<string, SpeedChallengeInfo> SpeedChallenges = new Dictionary<string, SpeedChallengeInfo>();

    /// <summary>
    /// SID -> roomName, container
    /// </summary>
    public static Dictionary<string, KeyValuePair<string, BinaryPacker.Element>> GlobalEntityMarkers = new();
    private string levelName;

    public override Dictionary<string, Action<BinaryPacker.Element>> Init() {
        return new Dictionary<string, Action<BinaryPacker.Element>> {
            {
                "level", level => {
                    // be sure to write the level name down.
                    levelName = level.Attr("name").Split('>')[0];
                    if (levelName.StartsWith("lvl_")) {
                        levelName = levelName.Substring(4);
                    }
                }
            },
            {
                "entity:FrostHelper/SpeedRingChallenge", speedBerry => {
                    SpeedChallenges[AreaKey.GetSID() + '>' + speedBerry.Attr("name")] = new SpeedChallengeInfo() {
                        ID = new EntityID(levelName, speedBerry.AttrInt("id")),
                        GoalTime = speedBerry.AttrFloat("timeLimit"),
                    };
                }
            },
            {
                "entity:FrostHelper/GlobalEntityMarker", container => {
                    GlobalEntityMarkers[AreaData.SID] = new(levelName, container);
                }
            }
        };
    }

    public override void Reset() {
        /*
        string SID = AreaKey.GetSID();
        if (SpeedChallenges.ContainsKey(AreaKey.GetLevelSet()))
        {
            SpeedChallenges.Remove(SID);
        }*/
    }

    
    public override void End() {
    }
}
