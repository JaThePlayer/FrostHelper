using System.Diagnostics;

namespace FrostHelper.EXPERIMENTAL;

public static class DrawTracker {

    private static object? _currentObj;

    private static Dictionary<string, long> DrawAmts;

    private static List<ILHook> Hooks;

    [Command("draw_track", "[FrostHelper] Tracks draw calls made by entities")]
    public static void CmdDrawTrack() {
        On.Monocle.EntityList.RenderExcept += EntityList_RenderExcept;
        On.Monocle.EntityList.RenderOnly += EntityList_RenderOnly;
        On.Celeste.BackdropRenderer.Render += BackdropRenderer_Render;
        On.Celeste.BackdropRenderer.BeforeRender += BackdropRenderer_BeforeRender;

        DrawAmts = new();
        Hooks = new();



        FrostModule.GetCurrentLevel().OnEndOfFrame += () => {
            foreach (var method in typeof(SpriteBatch).GetMethods()) {
                if (method.GetMethodBody() is { } && method.Name == "Draw") {
                    Hooks.Add(new(method, Track));
                }
            }

            Engine.Commands.Open = false;

            FrostModule.GetCurrentLevel().BeforeRender();
            FrostModule.GetCurrentLevel().Render();
            FrostModule.GetCurrentLevel().AfterRender();

            On.Celeste.BackdropRenderer.Render -= BackdropRenderer_Render;
            On.Celeste.BackdropRenderer.BeforeRender -= BackdropRenderer_BeforeRender;
            On.Monocle.EntityList.RenderExcept -= EntityList_RenderExcept;
            On.Monocle.EntityList.RenderOnly -= EntityList_RenderOnly;

            foreach (var item in Hooks) {
                item.Dispose();
            }
            Hooks = new();

            var longestType = DrawAmts.Max(t => t.Key.Length);
            DrawAmts
             .OrderBy(p => p.Value) // while OrderByDescending might make more sense, this ordering makes it easier to read in the console
             .Select(p => $"{p.Key}{new string(' ', longestType - p.Key.Length)} {p.Value} - {p.Value * 6} verts")
             .Foreach(Console.WriteLine);

            Console.WriteLine(DrawAmts.Sum(p => p.Value * 6));
        };
    }

    private static void BackdropRenderer_BeforeRender(On.Celeste.BackdropRenderer.orig_BeforeRender orig, BackdropRenderer self, Scene scene) {
        foreach (Backdrop backdrop in self.Backdrops) {
            _currentObj = backdrop;
            backdrop.BeforeRender(scene);
            _currentObj = null;
        }
    }

    private static void BackdropRenderer_Render(On.Celeste.BackdropRenderer.orig_Render orig, BackdropRenderer self, Scene scene) {
        BlendState blendState = BlendState.AlphaBlend;
        foreach (Backdrop backdrop in self.Backdrops) {
            if (backdrop.Visible) {
                if (backdrop is Parallax && (backdrop as Parallax)!.BlendState != blendState) {
                    self.EndSpritebatch();
                    blendState = (backdrop as Parallax)!.BlendState;
                }
                if (backdrop.UseSpritebatch && !self.usingSpritebatch) {
                    self.StartSpritebatch(blendState);
                }
                if (!backdrop.UseSpritebatch && self.usingSpritebatch) {
                    self.EndSpritebatch();
                }

                _currentObj = backdrop;
                backdrop.Render(scene);
                _currentObj = null;
            }
        }
        if (self.Fade > 0f) {
            Draw.Rect(-10f, -10f, 340f, 200f, self.FadeColor * self.Fade);
        }
        self.EndSpritebatch();
    }

    private static void EntityList_RenderOnly(On.Monocle.EntityList.orig_RenderOnly orig, EntityList self, int matchTags) {
        foreach (Entity entity in self.entities) {
            if (entity.Visible && entity.TagCheck(matchTags)) {
                _currentObj = entity;
                entity.Render();
                _currentObj = null;
            }
        }
    }

    private static void EntityList_RenderExcept(On.Monocle.EntityList.orig_RenderExcept orig, EntityList self, int excludeTags) {
        foreach (Entity entity in self.entities) {
            if (entity.Visible && !entity.TagCheck(excludeTags)) {
                _currentObj = entity;
                entity.Render();
                _currentObj = null;
            }
        }
    }

    private static void Backdrop_Render(On.Celeste.Backdrop.orig_Render orig, Backdrop self, Scene scene) {
        _currentObj = self;

        orig(self, scene);

        _currentObj = null;
    }

    private static void Track(ILContext il) {
        var cursor = new ILCursor(il);

        cursor.EmitCall(IncrCount);
    }

    private static void IncrCount() {
        var t = _currentObj?.GetType();

        var tn = t?.FullName ?? new StackTrace(2).GetFrames().First(f => 
            !f.GetMethod().Name.Contains("DMD") &&
            !f.GetMethod().Name.Contains("Trampoline") &&
            f.GetMethod().DeclaringType.Assembly != typeof(SpriteBatch).Assembly &&
            f.GetMethod().DeclaringType.Assembly != typeof(Image).Assembly
        ).GetMethod().GetID().Split('(')[0];

        if (DrawAmts.TryGetValue(tn, out var a)) {
            DrawAmts[tn] = a + 1;
        } else {
            DrawAmts[tn] = 1;
        }
    }
}
