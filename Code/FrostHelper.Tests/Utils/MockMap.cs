namespace FrostHelper.Tests;

public static class MockMap {
    public static AreaKey AreaKey { get; } = new AreaKey(0) {
        _SID = "UnitTesting/TestMap"
    };

    public static AreaData AreaData { get; } = new();
}