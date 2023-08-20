namespace FrostHelper.EXPERIMENTAL;

internal static class OptimiseDebugMap {
    private static bool _hooksLoaded;

    public static void Load() {
        if (_hooksLoaded)
            return;
        _hooksLoaded = true;

        On.Celeste.Editor.LevelTemplate.RenderContents += LevelTemplate_RenderContents;
        On.Celeste.Editor.LevelTemplate.RenderOutline += LevelTemplate_RenderOutline;
        On.Celeste.Editor.LevelTemplate.RenderHighlight += LevelTemplate_RenderHighlight;
    }

    private static void LevelTemplate_RenderHighlight(On.Celeste.Editor.LevelTemplate.orig_RenderHighlight orig, Celeste.Editor.LevelTemplate self, Camera camera, bool hovered, bool selected) {
        if (!CameraCullHelper.IsRectangleVisible(self.X, self.Y, self.Width, self.Height, camera: camera))
            return;

        orig(self, camera, hovered, selected);
    }

    private static void LevelTemplate_RenderOutline(On.Celeste.Editor.LevelTemplate.orig_RenderOutline orig, Celeste.Editor.LevelTemplate self, Camera camera) {
        if (!CameraCullHelper.IsRectangleVisible(self.X, self.Y, self.Width, self.Height, camera: camera))
            return;

        orig(self, camera);
    }

    private static void LevelTemplate_RenderContents(On.Celeste.Editor.LevelTemplate.orig_RenderContents orig, Celeste.Editor.LevelTemplate self, Camera camera, List<Celeste.Editor.LevelTemplate> allLevels) {
        if (!CameraCullHelper.IsRectangleVisible(self.X, self.Y, self.Width, self.Height, camera: camera))
            return;

        orig(self, camera, allLevels);
    }

    [OnUnload]
    public static void Unload() {
        if (!_hooksLoaded)
            return;
        _hooksLoaded = false;

        On.Celeste.Editor.LevelTemplate.RenderContents -= LevelTemplate_RenderContents;
        On.Celeste.Editor.LevelTemplate.RenderOutline -= LevelTemplate_RenderOutline;
        On.Celeste.Editor.LevelTemplate.RenderHighlight -= LevelTemplate_RenderHighlight;
    }
}
