namespace FrostHelper.Tests;

public static class MockMap {
    public const string MockRoomName = "mock";
    
    public static AreaKey AreaKey { get; } = new AreaKey(0) {
        _SID = "UnitTesting/TestMap",
        Mode = AreaMode.Normal
    };

    public static AreaData AreaData { get; } = new() {
        Mode = [
            new ModeProperties()
        ]
    };

    public static void Initialize() {
        AreaData.Areas = [
            AreaData
        ];

        AreaData.Mode[0].MapData = new MapData(AreaKey) {
            Levels = [
                new LevelData(new BinaryPacker.Element {
                    Attributes = new Dictionary<string, object> {
                        ["name"] = $"lvl_{MockRoomName}",
                    },
                    Children = [],
                }),
            ]
        };
    }
}