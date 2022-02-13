namespace FrostHelper;

/// <summary>
/// A <see cref="Solid"/> that's not collidable by the player
/// </summary>
[CustomEntity("FrostHelper/FallingBlockBlocker")]
[Tracked]
public class NonPlayerSolid : Solid {
    #region Hooks
    private static bool _hooksLoaded = false;
    public static void Load() {
        if (!_hooksLoaded) {
            _hooksLoaded = true;

            On.Celeste.Player.Update += Player_Update;
            On.Celeste.Player.OnSquish += Player_OnSquish;
        }
    }

    [OnUnload]
    public static void Unload() {
        On.Celeste.Player.Update -= Player_Update;
        On.Celeste.Player.OnSquish -= Player_OnSquish;

        _hooksLoaded = false;
    }

    private static void Player_Update(On.Celeste.Player.orig_Update orig, Player self) {
        var blockers = self.Scene.Tracker.GetEntities<NonPlayerSolid>();
        foreach (var item in blockers) {
            item.Collidable = false;
        }
        orig(self);
        foreach (var item in blockers) {
            item.Collidable = true;
        }
    }

    // prevent squishing
    private static void Player_OnSquish(On.Celeste.Player.orig_OnSquish orig, Player self, CollisionData data) {
        if (data.Hit is NonPlayerSolid) {
            self.Position = data.TargetPosition;
            return;
        }

        orig(self, data);
    }
    #endregion

    public NonPlayerSolid(EntityData data, Vector2 offset) : base(data.Position + offset, data.Width, data.Height, true) {
        Load();
    }
}
