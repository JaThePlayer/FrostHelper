using FrostHelper.Tests.Utils;
using FrostHelper.Triggers.Activator;

namespace FrostHelper.Tests.Activators;

[Collection("FrostHelper")]
public class OnFlagActivatorTests {
    [Fact]
    public void EmptyFlagIsTreatedAsNotMet() {
        var activator = new OnFlagActivator(new EntityData {
            Width = 8, Height = 8,
            Values = new Dictionary<string, object> {
                ["flag"] = "",
                ["triggerOnRoomBegin"] = true,
                ["targetState"] = true,
            },
            Nodes = [
                new Vector2(10, 10)
            ]
        }, default);
        
        var testTrigger = new TestTrigger(new EntityData {
            Width = 16, Height = 16,
            Position = new Vector2(8, 8)
        }, default);
        
        
        var level = TestUtils.CreateLevel();
        lock (TestUtils.EngineSceneLock) {
            Engine.Instance.scene = level;
        
            level.Add(activator);
            level.Add(testTrigger);
        
            level.Entities.UpdateLists();
            Assert.Equal(0, testTrigger.OnEnterCallCount);
        }
    }
}