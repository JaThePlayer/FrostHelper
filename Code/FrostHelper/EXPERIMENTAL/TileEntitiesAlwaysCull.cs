namespace FrostHelper.EXPERIMENTAL;

// will be implemented in everest PR
public static class TileEntitiesAlwaysCull {
    //[OnLoad]
    public static void Load() {
        //On.Monocle.TileGrid.RenderAt += TileGrid_RenderAt;
    }

    private static void TileGrid_RenderAt(On.Monocle.TileGrid.orig_RenderAt orig, TileGrid self, Vector2 position) {
        if (self.ClipCamera is null && self.Scene is Level lvl) {
            self.ClipCamera = lvl.Camera;
        }

        orig(self, position);
    }

    //[OnUnload]
    public static void Unload() {
        On.Monocle.TileGrid.RenderAt -= TileGrid_RenderAt;
    }
}
