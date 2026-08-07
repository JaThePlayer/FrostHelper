using System.Runtime.CompilerServices;

namespace FrostHelper.Tests.SessionExpressions;

[Collection("FrostHelper")]
public class SimpleCommands {
    [Fact]
    public void PlayerCommands() {
        var level = TestUtils.CreateLevel();
        var session = level.Session;
        var player = TestUtils.CreatePlayer();
        
        lock (TestUtils.EngineSceneLock) {
            Engine.Instance.scene = level;
        
            // No player in level -> returns 0.
            Assert.Equal(0, TestUtils.CreateExpr("$player").Get<int>(session));
        
            level.Add(player);
            level.Entities.UpdateLists();
            Assert.Equal(player, TestUtils.CreateExpr("$player").Get<Player>(session));
        }
    }
}