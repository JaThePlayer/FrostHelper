using System.Collections.Concurrent;

namespace FrostHelper; 

internal sealed class FrostMapDataProcessor : EverestMapDataProcessor {
    public record struct SpeedChallengeInfo(EntityID Id, float GoalTime);

    public record EntityMarker(string RoomName, BinaryPacker.Element Marker);

    public static ConcurrentDictionary<string, SpeedChallengeInfo> SpeedChallenges { get; } = [];

    /// <summary>
    /// SID -> roomName, container
    /// </summary>
    public static ConcurrentDictionary<string, List<EntityMarker>> GlobalEntityMarkers { get; } = new();

    private string _levelName;

    public override Dictionary<string, Action<BinaryPacker.Element>> Init() =>
        new() {
            ["level"] = level => {
                // be sure to write the level name down.
                _levelName = level.Attr("name").Split('>')[0];
                if (_levelName.StartsWith("lvl_")) {
                    _levelName = _levelName.Substring(4);
                }
            },
            ["entity:FrostHelper/SpeedRingChallenge"] = ParseSpeedChallenge,
            ["entity:FrostHelper/SpeedRingChallenge3d"] = ParseSpeedChallenge,
            ["entity:FrostHelper/GlobalEntityMarker"] = container => {
                GlobalEntityHelper.LoadIfNeeded();
                    
                GlobalEntityMarkers
                    .GetOrAdd(AreaData.SID, _ => [])
                    .Add(new EntityMarker(_levelName, container));
            },
        };

    private void ParseSpeedChallenge(BinaryPacker.Element speedBerry) {
        SpeedChallenges[AreaKey.GetSID() + '>' + speedBerry.Attr("name")] = new SpeedChallengeInfo {
            Id = new EntityID(_levelName, speedBerry.AttrInt("id")),
            GoalTime = speedBerry.AttrFloat("timeLimit"),
        };
    }

    public override void Reset() {
        GlobalEntityMarkers.Remove(AreaKey.SID, out _);
    }

    public override void End() {
    }
}
