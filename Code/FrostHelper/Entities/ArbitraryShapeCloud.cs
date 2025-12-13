using FrostHelper.Components;
using FrostHelper.Helpers;
using FrostHelper.ModIntegration;
using System.Diagnostics;
using System.Threading;

namespace FrostHelper.Entities;

[CustomEntity("FrostHelper/ArbitraryShapeCloud")]
[Tracked]
internal sealed class ArbitraryShapeCloud : Entity {
    private static volatile int _debugRenderTargetCount;

    [Conditional("DEBUG")]
    private static void PrintRenderTargetCount() {
        Logger.Log(LogLevel.Debug, "FrostHelper.ArbitraryShapeCloudDebug", $"RenderTargets: {_debugRenderTargetCount}");
    }

    public readonly VertexPositionColor[] Fill;
    public readonly Vector2[] Nodes;
    public readonly List<CloudTexture> Textures;
    
    public Color Color;
    public float Parallax;
    public bool Rainbow;
    public CachingOptions CachingStrategy;
    public readonly HashSet<string> CloudTags;

    private Rectangle? Bounds;
    private List<SealedImage> Images = [];

    private Rectangle RoomBounds;

    public enum CachingOptions {
        Auto,
        Never,
        RenderTarget,
    }

    private Vector2 RenderTargetRenderPos;
    private RenderTarget2D? RenderTarget;

    private readonly bool _maskToRoomBounds;

    public ArbitraryShapeCloud(EntityData data, Vector2 offset) : base(data.Position + offset) {
        Color = data.GetColor("color", "ffffff");
        Fill = ArbitraryShapeEntityHelper.GetFillVertsFromNodes(data, offset, Color.White);
        Nodes = data.GetNodesWithOffsetWithPositionAppended(offset);
        Rainbow = data.Bool("rainbow");
        CloudTags = data.GetStringHashsetTrimmed("cloudTag");

        Parallax = data.Float("parallax", 0f);
        Depth = data.Int("depth");
        Textures = ParseTextureList(data.Attr("textures", @"decals/10-farewell/clouds/cloud_c,decals/10-farewell/clouds/cloud_cc,decals/10-farewell/clouds/cloud_cd,decals/10-farewell/clouds/cloud_ce"));

        _maskToRoomBounds = data.Bool("maskToRoomBounds");
        CreateImages();

        switch (CachingStrategy = data.Enum("cache", CachingOptions.Auto)) {
            case CachingOptions.Never:
                break;
            case CachingOptions.Auto:
                CachingStrategy = CachingOptions.RenderTarget;
                HandleRenderTargetStrategy();
                break;
            case CachingOptions.RenderTarget:
                HandleRenderTargetStrategy();
                break;
        }

        Active = false;

        if (data.Bool("blockBloom", false)) {
            Add(new CustomBloomBlocker() {
                OnRender = () => DoRender(true)
            });
        }
    }

    private void HandleRenderTargetStrategy() {
        Add(new BeforeRenderHook(BeforeRender));
    }

    private void DisposeTarget() {
        if (RenderTarget is { IsDisposed: false }) {
            RenderTarget.Dispose();
            RenderTarget = null;

            Interlocked.Increment(ref _debugRenderTargetCount);
            PrintRenderTargetCount();
        }
    }

    private Rectangle CalcBounds() {
        var oldPos = Position;
        Position = default;
        var allPoses = Nodes.Concat(Images.SelectMany(i => {
            var r = i.GetRectangle();

            return new[] { r.Location.ToVector2(), new(r.Right, r.Bottom) };
        }));
        
        var ret = RectangleExt.FromPoints(allPoses);
        
        Position = oldPos;
        return ret;
    }

    public override void Awake(Scene scene) {
        base.Awake(scene);

        RoomBounds = scene.ToLevel().Bounds;
    }

    private void BeforeRender() {
        if (RenderTarget is { IsDisposed: false }) {
            return;
        }
        RenderTarget?.Dispose();

        var gd = Engine.Graphics.GraphicsDevice;

        var lastPos = Position;
        Position = default;
        Bounds ??= CalcBounds();
        var bounds = Bounds.Value;

        var left = bounds.Left;
        var top = bounds.Top;
        var w = bounds.Width;
        var h = bounds.Height;

        // todo: handle huge areas
        if (w > 4096 || h > 4096) {
            CachingStrategy = CachingOptions.Never;
            return;
        }

        RenderTarget = new(gd, w, h, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        _debugRenderTargetCount++;
        PrintRenderTargetCount();

        gd.SetRenderTarget(RenderTarget);
        gd.Clear(Color.Transparent);

        // draw the fill
        var cam = Matrix.CreateTranslation(-left, -top, 0f);
        GFX.DrawVertices(cam, Fill, Fill.Length, null, BlendState.AlphaBlend);

        // draw sprites
        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, cam);
        foreach (var item in Images) {
            if (item.Visible) {
                item.Color = Color.White;
                item.Render();
            }
        }
        GameplayRenderer.End();

        gd.SetRenderTarget(null);
        Position = lastPos;
        RenderTargetRenderPos = new(left, top);

        Scene.OnEndOfFrame += () => Remove(Get<BeforeRenderHook>());
    }

    private Vector2 CalcParallaxOffset(Vector2 pos) {
        Vector2 screenCenter = FrostModule.GetCurrentLevel().Camera.Position + new Vector2(320f / 2, 180f / 2);

        if (Parallax != 0f) {
            return ((pos - screenCenter) * Parallax).Floor();
        }

        return default;
    }

    public override void Render() {
        DoRender(false);
    }

    private void DoRender(bool bloomBlocker) {
        var color = Rainbow ? ColorHelper.GetHue(Scene, Position) * (Color.A / 255f) : Color;
        if (color == default) {
            RenderTarget?.Dispose();
            RenderTarget = null;
            return;
        }
        
        if (RenderTarget is { IsDisposed: false }) {
            var pos = RenderTargetRenderPos + CalcParallaxOffset(RenderTargetRenderPos);
            if (_maskToRoomBounds) {
                var roomBounds = RoomBounds;
                // TODO: huh, why is this necessary for vertical transitions??
                roomBounds.Y -= 2;
                roomBounds.Height += 4;
                var visiblePart = CameraCullHelper.GetVisibleSection(
                    new Rectangle((int) pos.X, (int) pos.Y, RenderTarget.Width, RenderTarget.Height), roomBounds, lenience: 0);

                var ox = visiblePart.Left - (int)pos.X;
                var oy = visiblePart.Top - (int)pos.Y;
            
                Draw.SpriteBatch.Draw(RenderTarget,
                    pos + new Vector2(ox, oy),
                    new Rectangle(ox, oy, visiblePart.Width, visiblePart.Height),
                    color);
            } else {
                Draw.SpriteBatch.Draw(RenderTarget, pos, color);
            }
        } else {
            // in case of savestates, the render target will get cleared before loading a state, but we should still cache after loading back.
            if (CachingStrategy == CachingOptions.RenderTarget) {
                HandleRenderTargetStrategy();
            }
            RenderNoTarget(CalcParallaxOffset(Position), bloomBlocker, color);
        }
    }

    void RenderNoTarget(Vector2 parallaxOffset, bool bloomBlocker, Color color) {
        Bounds ??= CalcBounds();
        if (!CameraCullHelper.IsRectangleVisible(Bounds.Value.MovedBy(parallaxOffset)))
            return;

        GameplayRenderer.End();

        var cam = SceneAs<Level>().Camera.Matrix * Matrix.CreateTranslation(parallaxOffset.X, parallaxOffset.Y, 0f);
        GFX.DrawVertices(cam, Fill, Fill.Length,
            bloomBlocker ? CustomBloomBlocker.BloomBlockVertsEffect : EffectRef.SolidColorVerts(color),
            //null,
            bloomBlocker ? CustomBloomBlocker.ReverseCutoutState : BlendState.AlphaBlend
        );

        // draw sprites
        if (bloomBlocker) {
            CustomBloomBlocker.BeginBloomBlockerBatch();
        } else {
            GameplayRenderer.Begin();
        }

        var lastPos = Position;
        Position = parallaxOffset;
        foreach (var item in Images) {
            if (item.Visible) {
                item.Color = color;
                item.Render();
            }
        }
        Position = lastPos;
    }

    public override void Removed(Scene scene) {
        base.Removed(scene);

        DisposeTarget();
    }

    public override void SceneEnd(Scene scene) {
        base.SceneEnd(scene);

        DisposeTarget();
    }

    public override void HandleGraphicsReset() {
        base.HandleGraphicsReset();

        DisposeTarget();
    }

    ~ArbitraryShapeCloud() {
        DisposeTarget();
    }

    private void CreateImages() {
        var start = Position;
        var nodes = Nodes;

        var widthFactor = 0.75f;

        for (int i = 0; i < nodes.Length; i++) {
            var n = nodes[i];
            var angle = Calc.Angle(start, n);
            var angleVec = Calc.AngleToVector(angle, 1f);

            var curr = start;
            var dist = Vector2.Distance(start, n);
            while (dist > 0) {
                var t = curr.SeededRandomFrom(Textures);

                var spr = t.Texture;
                var sprW = spr.Width * widthFactor;
                var offset = Math.Min(0, dist - sprW);
                var rot = angle + t.DefaultRotation;

                Images.Add(new SealedImage(spr) {
                    Entity = this,
                    Rotation = rot,
                    Position = (curr + angleVec * offset).Floor() + Vector2.UnitY.Rotate(rot) * 2f,
                }.JustifyOrigin(new Vector2(0f, 1f)));

                curr += angleVec * sprW;
                dist -= sprW;
            }

            start = n;
        }
    }

    /*
    public override void DebugRender(Camera camera) {
        foreach (var item in Nodes) {
            Draw.HollowRect(item + CalcParallaxOffset(item), 1, 1, Color.Red);
        }

        if (Bounds is { } bounds) {
            Draw.HollowRect(Bounds.Value.MovedBy(CalcParallaxOffset(Position)), Color.Pink);
        }

        ArbitraryShapeEntityHelper.DrawDebugWireframe(Fill, Color.Red * 0.3f, (p => {
            var v2 = new Vector2(p.Position.X, p.Position.Y);

            return v2 + CalcParallaxOffset(v2);
        }));
    }
    */
    
    public class CloudTexture {
        public string Path;
        public float DefaultRotation;
        public MTexture Texture;

        public CloudTexture(string path, float defaultRotation) {
            Path = path;
            DefaultRotation = defaultRotation;

            Texture = GFX.Game[Path];
        }
    }

    static readonly Dictionary<string, List<CloudTexture>> CloudTextureCache = new(StringComparer.Ordinal);

    public static List<CloudTexture> ParseTextureList(string list) {
        if (CloudTextureCache.TryGetValue(list, out var cached))
            return cached;

        var split = list.Trim().Split(',');

        var t = new List<CloudTexture>(split.Length);

        foreach (var texDef in split) {
            /*
            var innerSplit = texDef.Split(':');

            CloudTexture? tex = innerSplit switch {
                [var path] => new(path, 0f),
                [var path, var rotStr] => new(path, rotStr.ToSingle().ToRad()),
                _ => null,
            };

            if (tex is { })
                t.Add(tex);*/
            // initial rotation is broken currently, let's not allow it yet
            t.Add(new(texDef, 0f));
        }

        CloudTextureCache[list] = t;

        return t;
    }

}
