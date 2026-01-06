namespace FrostHelper.Tests.Components;

[Collection("FrostHelper")]
public class FlagListenerTests {
    [Fact]
    public void SpecificFlag_MustChange_TriggerOnRoomBegin() {
        const string flagName = "test";
        bool? justSeenValue = null;
        var calls = 0;
        var listener = new FlagListener(flagName, newValue => {
            justSeenValue = newValue;
            calls++;
        }, mustChange: true, triggerOnRoomBegin: true);

        var level = TestUtils.CreateLevel();
        Engine.Instance.scene = level;
        
        level.Add([ listener ]);
        
        // triggerOnRoomBegin
        level.Entities.UpdateLists();
        Assert.Equal(1, calls);
        Assert.True(justSeenValue.HasValue);
        Assert.False(justSeenValue);
        
        // mustChange
        level.Session.SetFlag(flagName, false);
        Assert.Equal(1, calls);
        
        level.Session.SetFlag(flagName);
        Assert.Equal(2, calls);
        Assert.True(justSeenValue);
    }
    
    [Fact]
    public void SpecificFlag_NoMustChange_NoTriggerOnRoomBegin() {
        const string flagName = "test";
        bool? justSeenValue = null;
        var calls = 0;
        var listener = new FlagListener(flagName, newValue => {
            justSeenValue = newValue;
            calls++;
        }, mustChange: false, triggerOnRoomBegin: false);

        var level = TestUtils.CreateLevel();
        Engine.Instance.scene = level;
        
        level.Add([ listener ]);
        
        // !triggerOnRoomBegin
        level.Entities.UpdateLists();
        Assert.Equal(0, calls);
        Assert.False(justSeenValue.HasValue);
        
        // !mustChange
        level.Session.SetFlag(flagName, false);
        Assert.Equal(1, calls);
        Assert.False(justSeenValue);
        
        level.Session.SetFlag(flagName);
        Assert.Equal(2, calls);
        Assert.True(justSeenValue);
    }
}