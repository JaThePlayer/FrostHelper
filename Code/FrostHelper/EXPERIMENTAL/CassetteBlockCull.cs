namespace FrostHelper.EXPERIMENTAL;

public static class CassetteBlockCull {
    [OnLoad]
    public static void Init() {


        //On.Celeste.Spikes.Render += Spikes_Render;
        //On.Celeste.SeekerBarrier.Render += SeekerBarrier_Render;
        //On.Celeste.BlackholeBG.BeforeRender += BlackholeBG_BeforeRender;
    }

    private static void BlackholeBG_BeforeRender(On.Celeste.BlackholeBG.orig_BeforeRender orig, BlackholeBG self, Scene scene) {
        if (self.Visible)
            orig(self, scene);
    }

    private static void SeekerBarrier_Render(On.Celeste.SeekerBarrier.orig_Render orig, SeekerBarrier self) {
        if (CameraCullHelper.IsRectVisible(FrostModule.GetCurrentLevel().Camera.Position, self.Position.X, self.Position.Y, self.Width, self.Height))
            orig(self);
    }

    private static void Spikes_Render(On.Celeste.Spikes.orig_Render orig, Spikes self) {
        if (CameraCullHelper.IsRectVisible(FrostModule.GetCurrentLevel().Camera.Position, self.Position.X, self.Position.Y, self.Width, self.Height))
            orig(self);
    }

    [OnUnload]
    public static void Unload() {
        On.Celeste.Spikes.Render -= Spikes_Render;
        On.Celeste.SeekerBarrier.Render -= SeekerBarrier_Render;
        On.Celeste.BlackholeBG.BeforeRender -= BlackholeBG_BeforeRender;
    }
}
