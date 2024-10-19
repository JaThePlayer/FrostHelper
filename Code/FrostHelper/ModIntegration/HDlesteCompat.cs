namespace FrostHelper.ModIntegration;

public static class HDlesteCompat {
    public static int Scale => 1; //GameplayBuffers.Gameplay.Width / 320;

    public static Vector2 GameplayPosToHiResPos(Vector2 pos) {
        if (Engine.Scene is Level level)
            return (pos - level.Camera.Position) * Scale;
        else
            return pos;
    }

    public static float GameplayPosToHiResPos(float pos, bool Y) {
        if (Engine.Scene is Level level && Scale > 1)
            return pos - (Y ? level.Camera.Position.Y : level.Camera.Position.X) * Scale;
        else
            return pos;
    }
}
