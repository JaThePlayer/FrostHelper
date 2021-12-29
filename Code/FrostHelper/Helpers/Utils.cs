namespace FrostHelper;

public static class Utils {

    /// <summary>
    /// Creates a rectangle from floats, casting them to int
    /// </summary>
    public static Rectangle CreateRect(float x, float y, float width, float height) {
        return new((int) x, (int) y, (int) width, (int) height);
    }
}
