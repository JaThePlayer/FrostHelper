namespace FrostHelper.Entities;

[CustomEntity("FrostHelper/BerryTrackerController")]
internal sealed class BerryTrackerController(EntityData data, Vector2 offset) : Entity(data.Position + offset) {
    private readonly string _berryCounter = data.Attr("berryCounter");
    
    private readonly string[] _levelSets = data.Attr("levelsets").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    
    public override void Update() {
        base.Update();
        UpdateFlags();
    }

    private void UpdateFlags() {
        if (Scene.MaybeLevel() is not { Session: {} session } level) {
            return;
        }
        
        var berriesInAllSets = 0;
        foreach (var levelSet in _levelSets) {
            var stats = SaveData.Instance.GetLevelSetStatsFor(levelSet);
            if (stats is null) {
                continue;
            }
            berriesInAllSets += stats.TotalStrawberries;
        }
        session.SetCounter(_berryCounter, berriesInAllSets);

        /*
        var area = session.Area;
        var isGoldenRun = session.GrabbedGolden;
        var stats = SaveData.Instance.Areas_Safe[area.ID].Modes[(int) area.Mode];
        var collectedBerries = isGoldenRun ? session.Strawberries : stats.Strawberries;
        session.SetCounter(_berryCounter, collectedBerries.Count);
        */
    }
}