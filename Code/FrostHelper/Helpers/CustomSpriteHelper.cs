namespace FrostHelper;

public static class CustomSpriteHelper {
    private static readonly Dictionary<string, SpriteData> Cache = new();

    /// <summary>
    /// Creates a <see cref="Sprite"/> like <see cref="SpriteBank.Create(string)"/>, except all the sprites are taken from <paramref name="customDirectory"/> instead of the path specified in Spites.xml
    /// </summary>
    public static Sprite CreateCustomSprite(string id, string customDirectory) {
        if (Cache.TryGetValue(customDirectory, out var cachedData)) {
            return cachedData.Create();
        }

        SpriteData data = GFX.SpriteBank.SpriteData[id];
        SpriteData customData = new SpriteData(data.Atlas);

        foreach (var source in data.Sources) {
            customData.Add(source.XML, customDirectory);
        }

        Cache[customDirectory] = customData;
        return customData.Create();
    }
}
