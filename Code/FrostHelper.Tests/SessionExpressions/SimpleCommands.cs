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
            
            
            Assert.Equal(Vector2.Zero, TestUtils.CreateExpr("$speed").Get<Vector2>(session));
            Assert.Equal(0f, TestUtils.CreateExpr("$speed.x").Get<float>(session));
            Assert.Equal(0f, TestUtils.CreateExpr("$speed.y").Get<float>(session));

            player.Speed = new Vector2(4f, 2f);
            Assert.Equal(new Vector2(4f, 2f), TestUtils.CreateExpr("$speed").Get<Vector2>(session));
            Assert.Equal(4f, TestUtils.CreateExpr("$speed.x").Get<float>(session));
            Assert.Equal(2f, TestUtils.CreateExpr("$speed.y").Get<float>(session));
        }
    }
}