using FrostHelper.Helpers;
using System.Runtime.CompilerServices;

namespace FrostHelper;

[CustomEntity("FrostHelper/CustomZipMover")]
[Tracked]
public sealed class CustomZipMover : Solid {
    internal sealed class SpriteSource {
        public static SpriteSource LegacyRed => Get("objects/FrostHelper/customZipMover/redcog");
        public static SpriteSource LegacyBlue => Get("objects/FrostHelper/customZipMover/redcog/cold");
        public static SpriteSource LegacyBlack => Get("objects/FrostHelper/customZipMover/blackcog");
        public static SpriteSource LegacyNormal => Get("objects/zipmover");
        
        private static readonly Dictionary<string, SpriteSource> Cache = new();
        public static SpriteSource Get(string dir) {
            if (Cache.TryGetValue(dir, out var cached))
                return cached;

            return Cache[dir] = new(dir);
        }
        
        public string Directory { get; }
        
        private string CogPath => $"{Directory}/cog";
        private string InnerCogPath => $"{Directory}/innercog";
        private string BlockPath => $"{Directory}/block";
        
        // cached, as its needed per zipper to create a Sprite
        public string LightPath { get; }

        private MTexture[,] BlockTextures;

        private List<MTexture> InnerCogTextures;

        private MTexture CogTexture;

        // we bake the pixel texture into the same packed texture, to assure the gpu can render the entire zipper path in 1 draw call.
        private MTexture PixelTexture;

        private SpriteSource(string dir) {
            Directory = dir;
            LightPath = $"{Directory}/light";
            
            Bake();
        }

        private void Bake() {
            var packedGroups = TexturePackHelper.CreatePackedGroups([
                    GFX.Game.GetAtlasSubtextures(InnerCogPath),
                    [GFX.Game[BlockPath], GFX.Game[CogPath], Draw.Pixel]
                ],
                $"customZipMover.{Directory}", out _);

            InnerCogTextures = packedGroups[0];
            BlockTextures = CreateNineSlice(packedGroups[1][0]);
            CogTexture = packedGroups[1][1];
            PixelTexture = packedGroups[1][2];
        }

        private static MTexture[,] CreateNineSlice(MTexture block)
        {
            var edges = new MTexture[3, 3];
            for (int i = 0; i < 3; i++) {
                for (int j = 0; j < 3; j++) {
                    edges[i, j] = block.GetSubtexture(i * 8, j * 8, 8, 8, null);
                }
            }

            return edges;
        }

        public List<MTexture> InnerCogs() => InnerCogTextures;

        public MTexture[,] BlockNineSlice() => BlockTextures;

        public MTexture Cog() => CogTexture;

        public MTexture Pixel() => PixelTexture;
    }
    
    public enum LineColor {
        Red,
        Blue,
        Black,
        Normal,
        Core,
        Custom
    }
    
    private bool isCore = false;
    private bool iceModeNext = false;
    private bool iceMode = false;

    private Color ColdLineColor;
    private Color ColdLightLineColor;
    private Color HotLineColor;
    private Color HotLightLineColor;

    private bool drawLine;
    private Color tint = Color.White;
    
    private bool FillMiddle;

    private SpriteSource HotSource { get; }
    private SpriteSource? ColdSource { get; }

    private SpriteSource CurrentSource { get; set; }
    
    private void OnChangeMode(Session.CoreModes coreMode) {
        iceModeNext = coreMode == Session.CoreModes.Cold;
    }

    private void CheckModeChange() {
        if (iceModeNext != iceMode) {
            iceMode = iceModeNext;
            ToggleSprite();
        }
    }

    private void ToggleSprite() {
        if (iceMode) {
            ropeColor = ColdLineColor;
            ropeLightColor = ColdLightLineColor;
            CurrentSource = ColdSource ?? HotSource;
            SpeedMult = SpeedMultIce;
        } else {
            ropeColor = HotLineColor;
            ropeLightColor = HotLightLineColor;
            CurrentSource = HotSource;
            SpeedMult = SpeedMultNormal;
        }

        CreateSprites();
    }

    public bool Rainbow;

    public CustomZipMover(EntityData data, Vector2 offset) : base(data.Position + offset, data.Width, data.Height, false) {
        tint = ColorHelper.GetColor(data.Attr("tint", "ffffff"));
        Rainbow = data.Bool("rainbow", false);
        drawLine = data.Bool("showLine", true);

        string hexcolor, hexlightcolor;
        switch (data.Enum("color", LineColor.Custom)) // legacy support
        {
            case LineColor.Red:
                hexcolor = "e62e00"; // 230 46 0
                hexlightcolor = "ff5c33";
                HotSource = SpriteSource.LegacyRed;
                break;
            case LineColor.Blue:
                hexcolor = "006bb3"; // 0 107 179
                hexlightcolor = "0099ff";
                HotSource = SpriteSource.LegacyBlue;
                break;
            case LineColor.Black:
                hexcolor = "000000";
                hexlightcolor = "1a1a1a";
                HotSource = SpriteSource.LegacyBlack;
                break;
            case LineColor.Normal:
                hexcolor = "663931";
                hexlightcolor = "9b6157";
                HotSource = SpriteSource.LegacyNormal;
                break;
            case LineColor.Core:
                hexcolor = "e62e00"; // 230 46 0
                hexlightcolor = "ff5c33";
                ColdLineColor = ColorHelper.GetColor("006bb3");
                ColdLightLineColor = ColorHelper.GetColor("0099ff");
                HotLineColor = ColorHelper.GetColor(hexcolor);
                HotLightLineColor = ColorHelper.GetColor(hexlightcolor);
                isCore = true;
                HotSource = SpriteSource.LegacyRed;
                ColdSource = SpriteSource.LegacyBlue;
                break;
            case LineColor.Custom:
                hexcolor = data.Attr("lineColor", "663931");
                hexlightcolor = data.Attr("lineLightColor", "ff5c33");
                ColdLineColor = data.GetColor("coldLineColor", "663931");
                ColdLightLineColor = data.GetColor("coldLineLightColor", "663931");
                HotLineColor = data.GetColor("lineColor", "663931");
                HotLightLineColor = data.GetColor("lineLightColor", "ff5c33");
                var directory = data.Attr("directory", "objects/zipmover");
                // legacy support - bluecog was moved to redcog/cold to make core mode work correctly with them without hardcoding. We need this to make maps using the old directory still work. Ahorn doesn't have this however, so that new users won't accidentaly use this.
                if (directory == "objects/FrostHelper/customZipMover/bluecog")
                    directory = "objects/FrostHelper/customZipMover/redcog/cold";
                
                HotSource = SpriteSource.Get(directory);
                isCore = data.Bool("isCore", false);
                if (isCore) {
                    ColdSource = SpriteSource.Get(directory + "/cold");
                }
                break;
            default:
                throw new ArgumentOutOfRangeException("color", data.Enum("color", LineColor.Normal), null);
        }
        
        CurrentSource = HotSource;
        
        if (isCore) {
            Add(new CoreModeListener(OnChangeMode));
        }
        ropeColor = ColorHelper.GetColor(hexcolor);
        ropeLightColor = ColorHelper.GetColor(hexlightcolor);
        temp = new MTexture();
        sfx = new SoundSource();
        Depth = -9999;
        start = Position;
        target = data.Nodes[0] + offset;
        Add(new Coroutine(Sequence(), true));
        Add(new LightOcclude(1f));

        CreateSprites();

        float bloomAlpha = data.Float("bloomAlpha", 1f);
        if (bloomAlpha != 0.0f) {
            Add(bloom = new BloomPoint(bloomAlpha, data.Float("bloomRadius", 6f)));
            bloom.Position = new Vector2(Width / 2f, 4f);
        }

        SurfaceSoundIndex = 7;
        sfx.Position = new Vector2(Width, Height) / 2f;
        Add(sfx);

        //percentage = data.Float("percentage", 100f);
        if (data.Has("percentage")) {
            SpeedMultNormal = data.Float("percentage", 100f) / 100f;
            SpeedMultIce = SpeedMultNormal / 4;
        } else {
            SpeedMultNormal = data.Float("speedMultiplier", 1f);
            SpeedMultIce = data.Float("coldSpeedMultiplier", 0.25f);
        }

        SpeedMult = SpeedMultNormal;

        FillMiddle = data.Bool("fillMiddle", true);
    }

    public override void Added(Scene scene) {
        base.Added(scene);
        if (isCore) {
            iceModeNext = iceMode = SceneAs<Level>().CoreMode == Session.CoreModes.Cold;
            ToggleSprite();
        }
        
        ControllerHelper<ZipMoverPathRenderer>.AddToSceneIfNeeded(scene);
    }

    public override void Update() {
        base.Update();
        CheckModeChange();
        if (bloom != null)
            bloom.Y = streetlight.CurrentAnimationFrame * 3;
    }
    
    public void CreateSprites() {
        Components.RemoveAll<Image>();
        Components.RemoveAll<Sprite>();

        Add(streetlight = new Sprite(GFX.Game, CurrentSource.LightPath));
        streetlight.Add("frames", "", 1f);
        streetlight.Play("frames", false, false);
        streetlight.Active = false;
        streetlight.SetAnimationFrame(1);
        streetlight.Position = new Vector2(Width / 2f - streetlight.Width / 2f, 0f);
    }

    public override void Render() {
        if (!CameraCullHelper.IsRectangleVisible(Position.X + Shake.X, Position.Y + Shake.Y, Width, Height))
            return;

        bool rainbow = Rainbow;
        
        Vector2 position = Position;
        Position += Shake;
        if (FillMiddle) {
            var pixel = CurrentSource.Pixel();
            // Render the rect manually, to make use of our own pixel sprite,
            // which is packed to the same atlas as all the other sprites, saving a draw call.
            // We could set Draw.Pixel and call Draw.Rect as well, but this is slightly lower overhead and roughly same code size :shrug:
            Draw.SpriteBatch.Draw(pixel.Texture.Texture,
                new Rectangle((int)Position.X, (int)Position.Y, (int)Width, (int)Height), 
                pixel.ClipRect, Color.Black
            );
        }

        var visibleRect = CameraCullHelper.GetVisibleSection(new(
            (int)Position.X, (int)Position.Y,
            (int)Width,  (int)Height
        ));
        var skipX = (visibleRect.X - (int)Position.X) / 8;
        var skipY = (visibleRect.Y - (int)Position.Y) / 8;
        var width = visibleRect.Right - Position.X;
        var height = visibleRect.Bottom - Position.Y;
        
        #region Cogs
        
        const float RotationPerX = 1.04719758f;
        int count = innerCogs.Count;
        int y = 4 + skipY * 8;
        while (y <= height) {
            int x = 4 + skipX * 8;
            
            while (x <= width) {
                // Refactored math to make sure its consistent regardless of how many iterations we skip
                var rotationOffset = ((x / 8) + (y / 8) * Width / 8) * RotationPerX;
                int rotationDir = (y / 8 % 2 == 0) ? -1 : 1;
                rotationDir *= (x / 8 % 2 == 0) ? -1 : 1;
                
                int index = (int) (mod((rotationOffset + rotationDir * percent * 3.14159274f * 4f) / 1.57079637f, 1f) * count);
                MTexture mtexture = innerCogs[index];
                Rectangle clipRect = new Rectangle(0, 0, mtexture.Width, mtexture.Height);
                Vector2 offset = Vector2.Zero;
                var isSubtexture = false;
                
                if (x <= 4) {
                    offset.X = 2f;
                    clipRect.X = 2;
                    clipRect.Width -= 2;
                    isSubtexture = true;
                } else if (x >= Width - 4f) {
                    offset.X = -2f;
                    clipRect.Width -= 2;
                    isSubtexture = true;
                }
                
                if (y <= 4) {
                    offset.Y = 2f;
                    clipRect.Y = 2;
                    clipRect.Height -= 2;
                    isSubtexture = true;
                } else if (y >= Height - 4f) {
                    offset.Y = -2f;
                    clipRect.Height -= 2;
                    isSubtexture = true;
                }
                
                mtexture = isSubtexture ? mtexture.GetSubtexture(clipRect.X, clipRect.Y, clipRect.Width, clipRect.Height, temp) : mtexture;
                Vector2 renderPos = Position + new Vector2(x, y) + offset;
                Color color = rainbow ? ColorHelper.GetHue(Scene, renderPos) : tint;
                mtexture.DrawCentered(renderPos, color * (rotationDir < 0 ? 0.5f : 1f));
                
                x += 8;
            }

            y += 8;
        }
        #endregion

        if (Rainbow) {
            streetlight.Color = ColorHelper.GetHue(Scene, streetlight.RenderPosition);
        }

        #region Border
        {
            var widthBy8 = (int)width / 8 + (width % 8 > 0 ? 1 : 0);
            var heightBy8 = (int)height / 8 + (height % 8 > 0 ? 1 : 0);
            // Border textures only exist on the edges of the zipper
            // As such, we can avoid looping through the middle of the zipper at all
            
            // Handle left border:
            if (skipX == 0) {
                DrawVerticalBorder(this, 0, skipY, heightBy8, 0f);
            }
            
            // Handle right border:
            if (widthBy8 == Width / 8f && widthBy8 > 1) {
                DrawVerticalBorder(this, 2, skipY, heightBy8, Width - 8f);
            }
            
            // Handle top middle
            if (skipY == 0) {
                DrawHorizontalBorder(this, 0, skipX, widthBy8, 0f);
            }
            
            // Handle bottom middle
            if (heightBy8 == Height / 8f && heightBy8 > 1) {
                DrawHorizontalBorder(this, 2, skipX, widthBy8, Height - 8f);
            }

            // draws inner horizontal segments. Edges are already covered by DrawVerticalBorder
            static void DrawHorizontalBorder(CustomZipMover mover, int edgeY, int skipX, int widthBy8, float yOffset) {
                var pos = mover.Position;
                if (skipX == 0)
                    skipX = 1;
                if (widthBy8 == mover.Width / 8f)
                    widthBy8 -= 1;
                
                pos.X += 8 * skipX;
                pos.Y += yOffset;
                
                int i = skipX;
                while (i < widthBy8) {
                    Draw(mover, 1, edgeY, pos);
                    pos.X += 8f;
                    i++;
                }
            }
            
            static void DrawVerticalBorder(CustomZipMover mover, int edgeX, int skipY, int heightBy8, float xOffset) {
                var pos = mover.Position;
                pos.X += xOffset;
                pos.Y += 8 * skipY;
                
                int j = skipY;
                while (j < heightBy8) {
                    int edgeY = j == 0 ? 0 : j == mover.Height / 8f - 1f ? 2 : 1;
                    Draw(mover, edgeX, edgeY, pos);
                    j++;
                    pos.Y += 8f;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void Draw(CustomZipMover mover, int edgeX, int edgeY, Vector2 renderPos) {
                var color = mover.Rainbow ? ColorHelper.GetHue(Engine.Scene, renderPos) : mover.tint;
                
                mover.edges[edgeX, edgeY].Draw(renderPos, new Vector2(0, 0), color);
            }
        }
        #endregion
        
        base.Render();
        Position = position;
    }

    private IEnumerator Sequence() {
        Vector2 start = Position;
        while (true) {
            if (HasPlayerRider()) {
                sfx.Play("event:/game/01_forsaken_city/zip_mover", null, 0f);
                Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
                StartShaking(0.1f);
                yield return 0.1f;
                streetlight.SetAnimationFrame(3);
                StopPlayerRunIntoAnimation = false;
                float at = 0f;
                while (at < 1f) {
                    yield return null;
                    at = Calc.Approach(at, 1f, 2f * Engine.DeltaTime * (SpeedMult));
                    percent = Ease.SineIn(at);
                    Vector2 vector = Vector2.Lerp(start, target, percent);
                    //this.ScrapeParticlesCheck(vector);
                    if (Scene.OnInterval(0.1f)) {
                        Scene.Tracker.SafeGetEntity<ZipMoverPathRenderer>()?.CreateSparks(this);
                    }
                    MoveTo(vector);
                }
                StartShaking(0.2f);
                Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
                SceneAs<Level>().Shake(0.3f);
                StopPlayerRunIntoAnimation = true;
                yield return 0.5f;
                StopPlayerRunIntoAnimation = false;
                streetlight.SetAnimationFrame(2);
                at = 0f;
                while (at < 1f) {
                    yield return null;
                    at = Calc.Approach(at, 1f, 0.5f * Engine.DeltaTime);
                    percent = 1f - Ease.SineIn(at);
                    Vector2 position = Vector2.Lerp(target, start, Ease.SineIn(at));
                    MoveTo(position);
                }
                StopPlayerRunIntoAnimation = true;
                StartShaking(0.2f);
                streetlight.SetAnimationFrame(1);
                yield return 0.5f;
            } else {
                yield return null;
            }
        }
    }

    private float mod(float x, float m) {
        return (x % m + m) % m;
    }

    public float SpeedMult, SpeedMultNormal, SpeedMultIce;

    //public float percentage;
    public static ParticleType P_Scrape;
    public static ParticleType P_Sparks;
    private MTexture[,] edges => CurrentSource.BlockNineSlice();
    private Sprite streetlight;
    private BloomPoint bloom;
    private List<MTexture> innerCogs => CurrentSource.InnerCogs();
    private MTexture temp;
    private Vector2 start;
    private Vector2 target;
    private float percent;
    Color ropeColor;
    Color ropeLightColor;
    private SoundSource sfx;
    
    [Tracked]
    private class ZipMoverPathRenderer : Entity {
        private Vector2 From(CustomZipMover zipper) 
            => zipper.start + new Vector2(zipper.Width / 2f, zipper.Height / 2f);
        
        private Vector2 To(CustomZipMover zipper) 
            => zipper.target + new Vector2(zipper.Width / 2f, zipper.Height / 2f);
        
        public ZipMoverPathRenderer() {
            Depth = 5000;
        }

        public void CreateSparks(CustomZipMover zipper) {
            var from = From(zipper);
            var to = To(zipper);
            
            var sparkAdd = (from - to).SafeNormalize(5f).Perpendicular();
            float num = (from - to).Angle();
            var sparkDirFromA = num + 0.3926991f;
            var sparkDirFromB = num - 0.3926991f;
            var sparkDirToA = num + 3.14159274f - 0.3926991f;
            var sparkDirToB = num + 3.14159274f + 0.3926991f;
            
            SceneAs<Level>().ParticlesBG.Emit(ZipMover.P_Sparks, from + sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirFromA);
            SceneAs<Level>().ParticlesBG.Emit(ZipMover.P_Sparks, from - sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirFromB);
            SceneAs<Level>().ParticlesBG.Emit(ZipMover.P_Sparks, to + sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirToA);
            SceneAs<Level>().ParticlesBG.Emit(ZipMover.P_Sparks, to - sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirToB);
        }

        public override void Render() {
            var zippers = Scene.Tracker.SafeGetEntities<CustomZipMover>();
            if (zippers.Count == 0)
                return;

            /*
             New render method for zipper paths:
             - All paths get rendered at once, into a temporary buffer.
             - All zipper backgrounds are created at the same time
             - Afterwards, that buffer gets rendered twice (to create the shadow effect)
             */
            
            var b = Draw.SpriteBatch;
            var gd = Engine.Graphics.GraphicsDevice;
            var target = RenderTargetHelper.RentFullScreenBuffer();
            var cam = FrostModule.GetCurrentLevel().Camera.Position;
            
            GameplayRenderer.End();
            var prevTargets = gd.GetRenderTargets();
            gd.SetRenderTarget(target);
            gd.Clear(Color.Transparent);
            GameplayRenderer.Begin();
            
            var oldPixel = Draw.Pixel;
            foreach (CustomZipMover zipper in zippers) {
                var from = From(zipper);
                var to = To(zipper);
                
                // Set the pixel so that shapes we draw can be rendered in the same draw call
                Draw.Pixel = zipper.CurrentSource.Pixel();
                
                if (CameraCullHelper.IsRectVisible(cam, from, to)) {
                    DrawCogs(zipper, Vector2.Zero, null);
                }

                if (zipper.FillMiddle) {
                    var rect = new Rectangle((int) (zipper.X + zipper.Shake.X - 1f), (int) (zipper.Y + zipper.Shake.Y - 1f),
                        (int) zipper.Width + 2, (int) zipper.Height + 2);
                    // reduce height by one, as this rect is part of the render buffer which will get rendered 1px lower afterwards for the shadow effect
                    rect.Height -= 1;
                
                    if (CameraCullHelper.IsRectangleVisible(rect))
                        Draw.Rect(rect, Color.Black);
                }

            }
            Draw.Pixel = oldPixel;
            
            GameplayRenderer.End();
            gd.SetRenderTargets(prevTargets);
            
            GameplayRenderer.Begin();
            // create a shadow. This works because its hardcoded to use Color.Black
            b.Draw(target, cam + Vector2.UnitY, Color.Black);
            b.Draw(target, cam, Color.White);
            
            RenderTargetHelper.ReturnFullScreenBuffer(target);
        }

        private void DrawCogs(CustomZipMover zipper, Vector2 offset, Color? colorOverride = null) {
            if (!zipper.drawLine)
                return;
            
            var from = From(zipper);
            var to = To(zipper);
            var tint = zipper.tint;
            var cog = zipper.CurrentSource.Cog();
            
            Vector2 vector = (to - from).SafeNormalize();
            Vector2 value = vector.Perpendicular() * 3f;
            Vector2 value2 = -vector.Perpendicular() * 4f;
            float rotation = zipper.percent * 3.14159274f * 2f;
            Draw.Line(from + value + offset, to + value + offset, colorOverride ?? zipper.ropeColor);
            Draw.Line(from + value2 + offset, to + value2 + offset, colorOverride ?? zipper.ropeColor);
            for (float num = 4f - zipper.percent * 3.14159274f * 8f % 4f; num < (to - from).Length(); num += 4f) {
                Vector2 value3 = from + value + vector.Perpendicular() + vector * num;
                Vector2 value4 = to + value2 - vector * num;
                Draw.Line(value3 + offset, value3 + vector * 2f + offset, colorOverride ?? zipper.ropeLightColor);
                Draw.Line(value4 + offset, value4 - vector * 2f + offset, colorOverride ?? zipper.ropeLightColor);
            }
            
            Color cogColor = zipper.Rainbow ? ColorHelper.GetHue(Scene, from + offset) : tint;
            cog.DrawCentered(from + offset, colorOverride ?? cogColor, 1f, rotation);
            cogColor = zipper.Rainbow ? ColorHelper.GetHue(Scene, to + offset) : tint;
            cog.DrawCentered(to + offset, colorOverride ?? cogColor, 1f, rotation);
        }
    }
}