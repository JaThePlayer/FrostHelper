using Celeste.Mod.Core;

namespace FrostHelper.Tests.SessionExpressions;

[Collection("FrostHelper")]
public class SimpleCommands {
    [Fact]
    public void PlayerCommands() {
        var level = TestUtils.CreateLevel();
        var session = level.Session;
        var player = new Player(default, PlayerSpriteMode.Madeline);
        
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


            var playerPosExpr = TestUtils.CreateExpr("$player.pos");
            Assert.Equal(new Vector2(0f, 0f), playerPosExpr.Get<Vector2>(session));
            player.Position = new Vector2(12f, 1f);
            Assert.Equal(new Vector2(12f, 1f), playerPosExpr.Get<Vector2>(session));

            // Expressions should store the player even after death, so that expressions running on the player don't suddenly stop working.
            level.Remove(player);
            level.Entities.UpdateLists();
            Assert.Null(level.Tracker.GetEntity<Player>());
            Assert.Equal(player, TestUtils.CreateExpr("$player").Get<Player>(session));
            Assert.Equal(new Vector2(12f, 1f), playerPosExpr.Get<Vector2>(session));
        }
    }
    
    [Fact]
    public void SessionFieldAccess() {
        var level = TestUtils.CreateLevel();
        var session = level.Session;
        
        Assert.Equal(0, TestUtils.CreateExpr("$deaths").Get<int>(session));
        session.Deaths = 12;
        Assert.Equal(12, TestUtils.CreateExpr("$deaths").Get<int>(session));
            
        Assert.Equal(0, TestUtils.CreateExpr("$deathsHere").Get<int>(session));
        session.DeathsInCurrentLevel = 12;
        Assert.Equal(12, TestUtils.CreateExpr("$deathsHere").Get<int>(session));
        
        Assert.Equal(MockMap.MockRoomName, TestUtils.CreateExpr("$roomName").Get<string>(session));
    }

    [Fact]
    public void SettingsAccess() {
        var session = TestUtils.CreateTestSession();

        lock (TestUtils.SettingsInstanceLock) {
            Settings.Instance.DisableFlashes = false;
            Assert.False(TestUtils.CreateExpr("$photosensitive").Get<bool>(session));
            Settings.Instance.DisableFlashes = true;
            Assert.True(TestUtils.CreateExpr("$photosensitive").Get<bool>(session));
        }
    }
    
    [Fact]
    public void CoreModuleSettingsAccess() {
        var session = TestUtils.CreateTestSession();

        lock (TestUtils.SettingsInstanceLock) {
            Settings.Instance.DisableFlashes = false;
            
            CoreModule.Settings.PhotosensitivityScreenFlashOverride = false;
            Assert.True(TestUtils.CreateExpr("$allowScreenFlash").Get<bool>(session));
            CoreModule.Settings.PhotosensitivityScreenFlashOverride = true;
            Assert.True(TestUtils.CreateExpr("$allowScreenFlash").Get<bool>(session));
            
            Settings.Instance.DisableFlashes = true;
            CoreModule.Settings.PhotosensitivityScreenFlashOverride = false;
            Assert.False(TestUtils.CreateExpr("$allowScreenFlash").Get<bool>(session));
            CoreModule.Settings.PhotosensitivityScreenFlashOverride = true;
            Assert.True(TestUtils.CreateExpr("$allowScreenFlash").Get<bool>(session));
        }
    }

    [Fact]
    public void Strawberries() {
        var session = TestUtils.CreateTestSession();
        
        Assert.Empty(TestUtils.CreateExpr("$strawberries").Get<HashSet<EntityID>>(session));
        Assert.Equal(0, TestUtils.CreateExpr("$strawberries.count").Get<int>(session));

        session.Strawberries.Add(new EntityID("test", 1));
        Assert.Equal(1, TestUtils.CreateExpr("$strawberries.count").Get<int>(session));

        session.Strawberries.Add(new EntityID("test", 2));
        session.Strawberries.Add(new EntityID("blah", 3));
        Assert.Equal(2, TestUtils.CreateExpr("$strawberries.sum($s => $s.roomName == \"test\")").Get<int>(session));
    }
}