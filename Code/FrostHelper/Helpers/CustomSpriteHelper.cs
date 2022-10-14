namespace FrostHelper;

public static class CustomSpriteHelper {
    private static Dictionary<string, SpriteData> cache = new();

    /// <summary>
    /// Creates a <see cref="Sprite"/> like <see cref="SpriteBank.Create(string)"/>, except all the sprites are taken from <paramref name="customDirectory"/> instead of the path specified in Spites.xml
    /// </summary>
    public static Sprite CreateCustomSprite(string id, string customDirectory) {
        if (cache.TryGetValue(customDirectory, out SpriteData cachedData)) {
            return cachedData.Create();
        }

        SpriteData data = GFX.SpriteBank.SpriteData[id];
        SpriteData customData = new SpriteData(data.Atlas);

        foreach (var source in data.Sources) {
            customData.Add(source.XML, customDirectory);
        }

        cache[customDirectory] = customData;
        return customData.Create();
    }
}
