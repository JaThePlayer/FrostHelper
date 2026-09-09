namespace FrostHelper.Tests.Utils;

public sealed class TestTrigger : Trigger {
    public int OnEnterCallCount { get; private set; }
    
    public TestTrigger(EntityData data, Vector2 offset) : base(data, offset)
    {
    }

    public override void OnEnter(Player player) {
        base.OnEnter(player);
        OnEnterCallCount++;
    }
}