using FrostHelper.Colliders;
using FrostHelper.Components;
using FrostHelper.Helpers;
using FrostHelper.ModIntegration;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FrostHelper;

[CustomEntity("FrostHelper/IceSpinner", "FrostHelperExt/CustomBloomSpinner")]
[Tracked(false)]
public class CustomSpinner : Entity {
    #region Hooks
    private static bool _hooksLoaded;

    [HookPreload]
    public static void LoadIfNeeded() {
        if (_hooksLoaded)
            return;
        _hooksLoaded = true;

        FrostModule.RegisterILHook(EasierILHook.CreatePrefixHook(typeof(CrystalShatterTrigger), nameof(CrystalShatterTrigger.OnEnter), OnCrystalShatterTriggerEnter, capturedArgsCount: 1));
        On.Celeste.Player.SummitLaunchUpdate += Player_SummitLaunchUpdate;
    }

    private static void OnCrystalShatterTriggerEnter(CrystalShatterTrigger self) {
        var spinners = self.Scene.Tracker.SafeGetEntities<CustomSpinner>();
        if (spinners.Count <= 0)
            return;
        
        CrystalShatterTrigger.Modes mode = self.mode;
        if (mode == CrystalShatterTrigger.Modes.All) {
            Audio.Play("event:/game/06_reflection/boss_spikes_burst");
        }
        
        foreach (CustomSpinner spinner in spinners) {
            if (mode == CrystalShatterTrigger.Modes.All || self.CollideCheck(spinner)) {
                spinner.Destroy(false);
            }
        }
    }

    private static int Player_SummitLaunchUpdate(On.Celeste.Player.orig_SummitLaunchUpdate orig, Player self) {
        var ret = orig(self);
        CustomSpinner crystalStaticSpinner = self.Scene.CollideFirst<CustomSpinner>(new Rectangle((int) (self.X - 4f), (int) (self.Y - 40f), 8, 12));
        if (crystalStaticSpinner != null) {
            crystalStaticSpinner.Destroy(false);
            (self.Scene as Level)!.Shake(0.3f);
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
            Celeste.Celeste.Freeze(0.01f);
        }
        return ret;
    }

    [OnUnload]
    public static void UnloadHooks() {
        if (!_hooksLoaded)
            return;
        _hooksLoaded = false;

        On.Celeste.Player.SummitLaunchUpdate -= Player_SummitLaunchUpdate;
    }
    #endregion

    internal sealed class SpinnerFills {
        public List<Fill> Fills { get; } = [];

        public record struct Fill(MTexture Texture, Texture2D Texture2D, Color Color, Vector2 Position, float Rotation);

        public Span<Fill> FillsSpan => CollectionsMarshal.AsSpan(Fills);
    }

    private CustomSpinnerSpriteSource SpriteSource;
    
    // accessed by TAS-Helper via reflection
    internal CustomSpinnerController controller;
    
    public bool MoveWithWind;
    public bool DashThrough;
    
    // used by maddie's helping hand
    public Color Tint;
    
    public Color BorderColor;
    public int ID;
    public bool Rainbow;
    public bool HasCollider;
    public bool RenderBorder;
    public int DestroyDebrisCount;

    public bool RegisteredToRenderers = false;
    public bool SingleFGImage;
    public int AttachGroup;
    
    public bool iceMode;
    [Obsolete("Unused, will be removed!")]
    public string directory;
    public string destroyColor;
    public bool isCore;
    public static ParticleType P_Move => CrystalStaticSpinner.P_Move;
    public const float ParticleInterval = 0.02f;

    // used by maddie's helping hand
    public bool AttachToSolid;

    internal SpinnerFills? filler;
    public Entity? deco;

    // accessed by TAS-Helper via reflection
    private float offset;

    private bool expanded;

    private int randomSeed;

    private readonly BloomPoint? _bloomPoint;

    internal readonly List<SealedImage> _images = new();

    internal readonly float Scale;
    internal readonly float ImageScale;

    public CustomSpinner(EntityData data, Vector2 offset) : this(data, offset, data.Bool("attachToSolid", false), data.Attr("directory", "danger/FrostHelper/icecrystal"), data.Attr("destroyColor", "639bff"), data.Bool("isCore", false), data.Attr("tint", "ffffff")) { }

    public CustomSpinner(EntityData data, Vector2 position, bool attachToSolid, string directory, string destroyColor, bool isCore, string tint) : base(data.Position + position) {
        LoadIfNeeded();
        ID = data.ID;

        Scale = data.Float("scale", 1f);
        ImageScale = data.Float("imageScale", 1f);

        Rainbow = data.Bool("rainbow", false);
        RenderBorder = data.Bool("drawOutline", true);
        BorderColor = RenderBorder ? ColorHelper.GetColor(data.Attr("borderColor", "000000")) : Color.Transparent;

        DestroyDebrisCount = data.Int("debrisCount", 8);
        DashThrough = data.Bool("dashThrough", false);
        Tint = ColorHelper.GetColor(tint);
        // for VivHelper compatibility
        var spritePathSuffix = data.Attr("spritePathSuffix", "");

        // Always use a single FG image, which can cause parts of the texture to clip out of solids.
        // Much better for performance.
        // Note that frost helper spinners will attempt to consolidate the sprites anyway,
        // so this is useless for spinners that are not near a solid. 
        SingleFGImage = data.Bool("singleFGImage", false);

#pragma warning disable CS0618 // Type or member is obsolete
        this.directory = directory;
#pragma warning restore CS0618 // Type or member is obsolete
        MoveWithWind = data.Bool("moveWithWind", false);

        // funny story time: this used to exist in older versions of Frost Helper as a leftover.
        // I tried removing it in 1.20.3, but this broke some TASes due to spinner cycles.
        // So now this needs to stay here forever D:
        // List<MTexture> atlasSubtextures = GFX.Game.GetAtlasSubtextures(this.bgDirectory);
        // MTexture mtexture = Calc.Random.Choose(atlasSubtextures);
        // Actually, just calling Random.Next() is enough, so that's nice
        Calc.Random.Next();

        this.destroyColor = destroyColor;
        this.isCore = isCore;
        offset = Calc.Random.NextFloat();
        Tag = Tags.TransitionUpdate;

        HasCollider = data.Bool("collidable", true) && Scale > 0f;
        var hitboxStr = data.Attr("hitbox", "C,6,0,0;R,16,4,-8,-3");
        if (string.IsNullOrWhiteSpace(hitboxStr))
            HasCollider = false;

        if (HasCollider) {
            Collider = Scale == 1f && hitboxStr == "C,6,0,0;R,16,4,-8,-3"
                ? new SpinnerCollider()
                : CustomHitbox.CreateFrom(hitboxStr, Scale);

            Add(new PlayerCollider(OnPlayer, null, null));
            Add(new HoldableCollider(OnHoldable, null));
            Add(new LedgeBlocker(null));
        } else {
            Collidable = false;
        }
        Depth = -8500;
        AttachToSolid = attachToSolid;
        AttachGroup = data.Int("attachGroup", -1);
        if (AttachToSolid) {
            var mover = AttachGroup switch {
                -1 => new StaticMover(),
                _ => new GroupedStaticMover(AttachGroup, true)
            };

            mover.OnShake = OnShake;
            mover.SolidChecker = IsRiding;
            mover.OnDestroy = RemoveSelf;
            mover.OnDisable = () => {
                if (Visible)
                    UnregisterFromRenderers();
                Active = Visible = Collidable = false;
            };
            mover.OnEnable = () => {
                Active = Collidable = true;
            };

            Add(mover);
        }

        randomSeed = Calc.Random.Next();
        if (isCore) {
            //Add(new CoreModeListener(OnChangeMode));
        }
        float bloomAlpha = data.Float("bloomAlpha", 0.0f);
        if (bloomAlpha != 0.0f)
            Add(_bloomPoint = new BloomPoint(Collidable ? Collider.Center : Position + new Vector2(8f, 8f), bloomAlpha, data.Float("bloomRadius", 0f)));

        SetVisible(false);
        
        SpriteSource = CustomSpinnerSpriteSource.Get(directory, spritePathSuffix);
    }

    public static bool ConnectorRendererJustAdded = false;
    public override void Added(Scene scene) {
        base.Added(scene);
        if (!ConnectorRendererJustAdded) {
            GetConnectorRenderer();
            GetBorderRenderer();
            if (SpriteSource.HasDeco)
                GetDecoRenderer();
            ConnectorRendererJustAdded = true;
        }
    }

    public override void Awake(Scene scene) {
        ConnectorRendererJustAdded = false;
        controller = ControllerHelper<CustomSpinnerController>.AddToSceneIfNeeded(scene);
        UpdateController();
        
        base.Awake(scene);

        if (GameplayBuffers.Gameplay is { } ? InView() : InView(new(320f, 180f))) {
            CreateSprites();
        }
    }

    private void UpdateController() {
        if (BorderColor != Color.Black)
            controller.CanUseBlackOutlineRenderTargetOpt = false;

        var c = controller;
        if (c.FirstBorderColor == null) {
            c.FirstBorderColor = BorderColor;
            c.CanUseRenderTargetRender = true;
        } else if (c.CanUseRenderTargetRender && (c.FirstBorderColor != BorderColor)) {
            c.CanUseRenderTargetRender = false;
        }
    }

    public void UpdateHue() {
        ColorHelper.SetGetHueScene(Scene);
        foreach (var image in CollectionsMarshal.AsSpan(_images)) {
            image.Color = ColorHelper.GetHue(Position + image.Position);
        }
        if (filler != null) {
            var fills = CollectionsMarshal.AsSpan(filler.Fills);
            foreach (ref var fill in fills) {
                fill.Color = ColorHelper.GetHue(Position + fill.Position);
            }
        }
    }

    // exposed via the API
    internal void SetColor(Color color) {
        if (Tint == color)
            return;

        Tint = color;

        foreach (var image in CollectionsMarshal.AsSpan(_images)) {
            image.Color = color;
        }

        if (filler != null) {
            foreach (ref var fill in filler.FillsSpan) {
                fill.Color = color;
            }
        }
    }

    // exposed via the API
    internal void SetBorderColor(Color color) {
        if (BorderColor != color) {
            BorderColor = color;

            // since border color is used for optimisations, we need to re-check whether they're valid or not
            UpdateController();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetVisible(bool visible) {
        Visible = visible;
        if (_bloomPoint is { } p)
            p.Visible = visible;
    }
    
    internal void ClearSprites() {
        if (!expanded)
            return;

        expanded = false;
        SetVisible(false);
        _images.Clear();
    }

    internal void ResetSpriteSource() {
        SpriteSource = CustomSpinnerSpriteSource.Get(SpriteSource.Directory, SpriteSource.SpritePathSuffix);
    }

    public override void Update() {
        if (!Visible) {
            Collidable = false;
            if (InView()) {
                RegisterToRenderers();
                SetVisible(true);
                if (!expanded) {
                    CreateSprites();
                }
                if (Rainbow)
                    UpdateHue();
            }
        } else {
            // No need to call base.Update, as none of the components need to get updated.
            // We dodge horrible hooks this way (+ enumeration through the component list)
            // base.Update();

            var scene = Scene;
            var offset = this.offset;

            if (Rainbow && (controller.NoCycles || scene.OnInterval(0.08f, offset)))
                UpdateHue();

            if (scene.OnInterval(0.25f, offset) && !InView()) {
                SetVisible(false);
                UnregisterFromRenderers();
            }

            if (HasCollider && (controller.NoCycles || scene.OnInterval(0.05f, offset))) {
                // grabbing the cached player from the controller is faster than the tracker.
                if ((controller.Player ??= scene.Tracker.SafeGetEntity<Player>()) is { } player) {
                    Collidable = Math.Abs(player.X - X) < 128f && Math.Abs(player.Y - Y) < 128f;
                }
            }
        }

        if (MoveWithWind) {
            float move = Calc.ClampedMap(Math.Abs((Scene as Level)!.Wind.X), 0f, 800f, 0f, 5f);
            if ((Scene as Level)!.Wind.X < 0)
                move -= move * 2;
            var num = (int) move;
            if (num != 0) {
                Position.X += num;
                if (HasCollider)
                    Collider.Position.X += num;
            }
        }
    }

    public override void Render() {
        // if we're using the black outline optimisation, then the border renderer will handle rendering the normal spinner sprites anyway
        if (controller.CanUseBlackOutlineRenderTargetOpt && controller.CanUseRenderTargetRender)
            return;

        base.Render();
        foreach (var img in CollectionsMarshal.AsSpan(_images)) {
            img.Render();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool InView() {
        var gp = GameplayBuffers.Gameplay;
        
        return InView(new(gp.Width, gp.Height));
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool InView(NumVector2 gpBufferSize) {
        var camera = (Scene as Level)?.Camera!;

        var diff = Unsafe.As<Vector2, NumVector2>(ref Position);
        diff -= Unsafe.As<Vector2, NumVector2>(ref camera.position);
        
        var cullingDistance = SpriteSource.CullingDistance * ImageScale;
        diff += cullingDistance;
        if (diff.X <= 0f || diff.Y <= 0f)
            return false;
        diff -= gpBufferSize;
        diff -= cullingDistance;
        diff -= cullingDistance;
        
        return diff.X < 0f && diff.Y < 0f;
    }

    private void UnregisterFromRenderers() {
        if (RegisteredToRenderers) {
            Scene.Tracker.SafeGetEntity<SpinnerConnectorRenderer>()?.Remove(this);
            if (RenderBorder)
                Scene.Tracker.SafeGetEntity<SpinnerBorderRenderer>()?.Remove(this);
            if (SpriteSource.HasDeco)
                Scene.Tracker.SafeGetEntity<SpinnerDecoRenderer>()?.Spinners.Remove(this);
            RegisteredToRenderers = false;
        }
    }

    private void RegisterToRenderers() {
        if (!RegisteredToRenderers) {
            Scene.Tracker.SafeGetEntity<SpinnerConnectorRenderer>()?.Add(this);
            if (RenderBorder)
                Scene.Tracker.SafeGetEntity<SpinnerBorderRenderer>()?.Add(this);
            if (SpriteSource.HasDeco)
                Scene.Tracker.SafeGetEntity<SpinnerDecoRenderer>()?.Spinners.Add(this);
            RegisteredToRenderers = true;
        }
    }

    private void AddImage(SealedImage img) {
        _images.Add(img);
        img.Entity = this;
        img.Color = Tint;
        img.Scale = new(ImageScale);
        img.Active = false;
    }
    
    private bool ShouldUseHotSprites() => isCore && (Scene as Level)!.CoreMode == Session.CoreModes.Hot;
    
    private void CreateSprites() {
        if (!expanded) {
            UnregisterFromRenderers();
            RegisterToRenderers();

            Calc.PushRandom(randomSeed);

            List<MTexture> fgSubtextures = SpriteSource.GetFgTextures(ShouldUseHotSprites());
            MTexture fgTexture = Calc.Random.Choose(fgSubtextures);

            var s = SpriteSource.ConnectionWidth;

            foreach (CustomSpinner other in Scene.Tracker.SafeGetEntities<CustomSpinner>()) {
                if (other.ID > ID 
                    && other.AttachGroup == AttachGroup 
                    && other.AttachToSolid == AttachToSolid 
                    && (other.Position - Position).LengthSquared() < s*other.SpriteSource.ConnectionWidth*float.Pow(ImageScale + other.ImageScale, 2f) / 4f) {
                    AddSprite((Position + other.Position) / 2f - Position);
                    //crystalStaticSpinner.AddSprite((Position + crystalStaticSpinner.Position) / 2f - crystalStaticSpinner.Position);
                }
            }

            #region FG image(s)
            int imgCount = 0;
            bool topLeft = false, topRight = false, bottomLeft = false, bottomRight = false;
            if (SingleFGImage) {
                imgCount = 4;
            } else {
                if (!SolidCheck(new Vector2(X - 4f, Y - 4f))) {
                    topLeft = true;
                    imgCount++;
                }
                if (!SolidCheck(new Vector2(X + 4f, Y - 4f))) {
                    topRight = true;
                    imgCount++;
                }
                if (!SolidCheck(new Vector2(X + 4f, Y + 4f))) {
                    bottomLeft = true;
                    imgCount++;
                }
                if (!SolidCheck(new Vector2(X - 4f, Y + 4f))) {
                    bottomRight = true;
                    imgCount++;
                }
            }

            if (imgCount == 4) {
                var image = new SealedImage(fgTexture).CenterOrigin();
                AddImage(image);
            } else {
                // only spawn quarter images if it's needed to avoid edge cases
                AddCornerImages(fgTexture, topLeft, topRight, bottomLeft, bottomRight);
            }
            #endregion

            if (SpriteSource.HasDeco) {
                deco ??= new Entity(Position);

                var decoAtlasSubtextures = SpriteSource.GetDecoTextures(ShouldUseHotSprites());
                var decoImage = new SealedImage(decoAtlasSubtextures[fgSubtextures.IndexOf(fgTexture)]) {
                    Color = Tint,
                    Active = false,
                    Scale = new(ImageScale)
                };
                decoImage.CenterOrigin();
                deco.Add(decoImage);
            }

            expanded = true;
            Calc.PopRandom();
        }
    }

    private void AddCornerImages(MTexture mtexture, bool topLeft, bool topRight, bool bottomLeft, bool bottomRight) {
        SealedImage image;

        var halfUnitX = mtexture.Width / 2;
        var halfUnitY = mtexture.Height / 2; 

        if (topLeft && topRight) {
            image = new SealedImage(mtexture.GetSubtexture(0, 0, mtexture.Width, halfUnitY, null)).SetOrigin(halfUnitX - 2f, halfUnitY - 2f);
            AddImage(image);
        } else {
            if (topLeft) {
                image = new SealedImage(mtexture.GetSubtexture(0, 0, halfUnitX, halfUnitY, null)).SetOrigin(halfUnitX - 2f, halfUnitY - 2f);
                AddImage(image);
            }
            if (topRight) {
                image = new SealedImage(mtexture.GetSubtexture(10, 0, halfUnitX, halfUnitY, null)).SetOrigin(2f, halfUnitY - 2f);
                AddImage(image);
            }
        }

        if (bottomLeft && bottomRight) {
            image = new SealedImage(mtexture.GetSubtexture(0, 10, mtexture.Width, halfUnitY, null)).SetOrigin(halfUnitX - 2f, 2f);
            AddImage(image);
        } else {
            if (bottomLeft) {
                image = new SealedImage(mtexture.GetSubtexture(10, 10, halfUnitX, halfUnitY, null)).SetOrigin(2f, 2f);
                AddImage(image);
            }
            if (bottomRight) {
                image = new SealedImage(mtexture.GetSubtexture(0, 10, halfUnitX, halfUnitY, null)).SetOrigin(halfUnitX - 2f, 2f);
                AddImage(image);
            }
        }

    }

    public SpinnerConnectorRenderer GetConnectorRenderer() {
        var renderer = Scene.Tracker.SafeGetEntity<SpinnerConnectorRenderer>();
        if (renderer is null) {
            renderer = new SpinnerConnectorRenderer();
            Scene.Add(renderer);
        }
        return renderer;
    }

    public SpinnerDecoRenderer GetDecoRenderer() {
        var renderer = Scene.Tracker.SafeGetEntity<SpinnerDecoRenderer>();
        if (renderer is null) {
            renderer = new SpinnerDecoRenderer();
            Scene.Add(renderer);
        }
        return renderer;
    }

    public SpinnerBorderRenderer GetBorderRenderer() {
        var renderer = Scene.Tracker.SafeGetEntity<SpinnerBorderRenderer>();
        if (renderer is null) {
            renderer = new SpinnerBorderRenderer();
            Scene.Add(renderer);
        }
        return renderer;
    }

    private static Vector2 FixPos(Vector2 pos, Vector2 origin, float rotation) {
        Vector2 vector = origin.Rotate(rotation.ToRad());
        return pos.Round() + vector - vector.Round();
    }

    public void AddSprite(Vector2 offset) {
        offset = offset.Floor();
        
        filler ??= new();

        List<MTexture> atlasSubtextures = SpriteSource.GetBgTextures(ShouldUseHotSprites());

        var color = Rainbow ? ColorHelper.GetHue(Scene, Position + offset) : Tint;
        var texture = Calc.Random.Choose(atlasSubtextures);
        var rotation = Calc.Random.Choose(0, 1, 2, 3) * 1.57079637f;
        var image = new SpinnerFills.Fill(texture, texture.Texture.Texture_Safe, color, FixPos(offset, texture.Center, rotation), rotation);
        filler.Fills.Add(image);

        if (SpriteSource.HasDeco) {
            if (deco is null) {
                deco = new Entity(Position);
            }
            var decoAtlasSubtextures = SpriteSource.GetBgDecoTextures(ShouldUseHotSprites());
            var decoImage = new SealedImage(decoAtlasSubtextures[atlasSubtextures.IndexOf(image.Texture)]) {
                Position = offset,
                Rotation = image.Rotation,
                Color = Tint,
                Active = false
            };
            decoImage.CenterOrigin();
            deco.Add(decoImage);
        }
    }

    private bool SolidCheck(Vector2 position) {
        if (AttachToSolid || MoveWithWind) {
            return false;
        }
        foreach (var a in Scene.CollideAll<Solid>(position)) {
            if (a is SolidTiles) {
                return true;
            }
        }
        return false;
    }

    internal Vector2 ShakeAmt;
    
    private void OnShake(Vector2 amount) {
        foreach (var image in CollectionsMarshal.AsSpan(_images)) {
            // change from vanilla: instead of setting the position, add to it.
            image.Position += amount;
        }

        ShakeAmt += amount;
    }

    private bool IsRiding(Solid solid) {
        return CollideCheck(solid);
    }

    private void OnPlayer(Player player) {
        if (!(DashThrough && player.DashAttacking)) {
            player.Die((player.Position - Position).SafeNormalize(), false, true);
        }

    }

    private void OnHoldable(Holdable h) {
        h.HitSpinner(this);
    }

    public override void Removed(Scene scene) {
        UnregisterFromRenderers();
        base.Removed(scene);
    }

    public override void SceneEnd(Scene scene) {
        base.SceneEnd(scene);
        UnregisterFromRenderers();
    }

    public void Destroy(bool boss = false) {
        if (InView()) {
            Audio.Play("event:/game/06_reflection/fall_spike_smash", Position);
            Color color = Rainbow ? ColorHelper.GetHue(Scene, Position) : Calc.HexToColor(destroyColor);

            FastCrystalDebris.Burst(Position, color, boss, DestroyDebrisCount);
        }
        RemoveSelf();
    }
}

[Tracked]
public class SpinnerConnectorRenderer : Entity {
    private List<CustomSpinner> Spinners = new();

    public SpinnerConnectorRenderer() : base() {
        Active = false;
        Depth = -8500 + 1;
        Tag = Tags.Persistent;
    }

    public override void Render() {
        if (Spinners.Count == 0)
            return;

        if (Spinners[0].controller is { } b && b.CanUseBlackOutlineRenderTargetOpt && b.CanUseRenderTargetRender)
            return;

        ForceRender();
    }

    private interface IFillColorGetter {
        public Color GetColor(ref CustomSpinner.SpinnerFills.Fill fill);
    }

    private struct FromFillColor : IFillColorGetter {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Color GetColor(ref CustomSpinner.SpinnerFills.Fill fill) => fill.Color;
    }
    
    private struct SetColor(Color color) : IFillColorGetter {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Color GetColor(ref CustomSpinner.SpinnerFills.Fill fill) => color;
    }

    public void ForceRender() {
        DrawFills(new FromFillColor());
    }

    public void ForceRenderWithColor(Color c) {
        DrawFills(new SetColor(c));
    }

    private void DrawFills<T>(T colorGetter) where T : struct, IFillColorGetter {
        var spinners = CollectionsMarshal.AsSpan(Spinners);
        var batch = Draw.SpriteBatch;
        
        foreach (var spinner in spinners) {
            if (spinner is not { Visible: true, filler: { } filler })
                continue;
            
            var pos = spinner.Position + spinner.ShakeAmt;
            var fillerComponents = filler.FillsSpan;
            if (fillerComponents.Length == 0)
                continue;

            ref var image = ref fillerComponents[0];
            var mtexture = image.Texture;
            Texture2D texture = image.Texture2D;
            Rectangle? clipRect = new Rectangle?(mtexture.ClipRect);
            float scaleFix = mtexture.ScaleFix;
            Vector2 origin = (mtexture.Center - mtexture.DrawOffset) / scaleFix;
            scaleFix *= spinner.ImageScale;

            foreach (ref var c in fillerComponents) {
                batch.Draw(texture, c.Position + pos, clipRect, colorGetter.GetColor(ref c), c.Rotation, origin, scaleFix, SpriteEffects.None, 0f);
            }
        }
    }

    public void Add(CustomSpinner spinner) {
        Spinners.Add(spinner);
    }

    public void Remove(CustomSpinner spinner) {
        Spinners.Remove(spinner);
    }
}

[Tracked]
public class SpinnerDecoRenderer : Entity {
    public List<CustomSpinner> Spinners = new();

    public SpinnerDecoRenderer() : base() {
        Active = false;
        Depth = -10000 - 1;
        Tag = Tags.Persistent;
    }

    public override void Render() {
        foreach (var item in Spinners) {
            item.deco?.Render();
        }
    }
}

[Tracked]
public class SpinnerBorderRenderer : Entity {
    public List<CustomSpinner> Spinners = new();

    public void Add(CustomSpinner item) {
        Spinners.Add(item);
    }

    public void Remove(CustomSpinner item) {
        Spinners.Remove(item);
    }

    public SpinnerBorderRenderer() : base() {
        Active = false;
        Depth = -8500 + 2;
        Tag = Tags.Persistent;
    }

    public override void Render() {
        if (Spinners.Count == 0)
            return;

        var controller = Spinners[0].controller;

        if (controller.OutlineShader is { } outlineShader) {
            var cam = GameplayRenderer.instance.Camera;
            var eff = outlineShader.ApplyStandardParameters(cam);

            if (controller.CanUseRenderTargetRender) {
                RenderTargetRender(controller, eff);
                return;
            }

            GameplayRenderer.End();
            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, eff, cam.Matrix);

            NormalRender();

            GameplayRenderer.End();
            GameplayRenderer.Begin();
            return;
        }


        if (controller.CanUseRenderTargetRender) {
            RenderTargetRender(controller, null);

            // might as well render everything now
            // since the outlines are now rendered in black, this won't work anymore
            // TODO:(Perf) use this method when using black outlines
            //batch.Draw(target, renderPos, null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

            return;
        }

        NormalRender();
    }

    private void RenderTargetRender(CustomSpinnerController controller, Effect? effect) {
        // Renders all spinners into a render target, then renders that target 4 times, massively reducing the amount of sprites drawn.

        var target = RenderTargetHelper.RentFullScreenBuffer();
        var connectorRenderer = Scene.Tracker.SafeGetEntity<SpinnerConnectorRenderer>();
        var batch = Draw.SpriteBatch;
        var borderColor = controller.FirstBorderColor!.Value;

        // For black outlines, we can first render all sprites into the target using their normal tint,
        // then simply tint the target black afterwards.
        // Thanks to this, we can bypass the ConnectorRenderer - rendering the target without tinting will render the spinners properly.
        var blackOutlineOpt = controller.CanUseBlackOutlineRenderTargetOpt;

        // whether to use normal tints on the sprites, or use the border color
        // While using a border shader, we need to use normal tints, as we'll need to then tint the render target in the border color, so that the shader knows what that color is.
        var useImageColor = effect is { } || blackOutlineOpt;

        GameplayRenderer.End();
        Engine.Instance.GraphicsDevice.SetRenderTarget(target);
        Engine.Instance.GraphicsDevice.Clear(Color.Transparent);

        GameplayRenderer.Begin();

        var spinners = CollectionsMarshal.AsSpan(Spinners);
        if (useImageColor) {
            connectorRenderer?.ForceRender();
            
            foreach (var spinner in spinners) {
                foreach (var img in CollectionsMarshal.AsSpan(spinner._images)) {
                    img.Render();
                }
            }
        } else {
            connectorRenderer?.ForceRenderWithColor(borderColor);
            
            foreach (var spinner in spinners) {
                foreach (var img in CollectionsMarshal.AsSpan(spinner._images)) {
                    img.RenderWithColor(borderColor);
                }
            }
        }

        GameplayRenderer.End();

        Engine.Instance.GraphicsDevice.SetRenderTarget(GameplayBuffers.Gameplay);

        var cam = SceneAs<Level>().Camera;

        if (effect is { }) {
            batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, effect, cam.Matrix);
        } else {
            GameplayRenderer.Begin();
        }

        // border
        var renderPos = cam.Position.Floor();
        var finalColor = useImageColor ? borderColor : Color.White;

        batch.Draw(target, renderPos - Vector2.UnitY, null, finalColor);
        batch.Draw(target, renderPos + Vector2.UnitY, null, finalColor);
        batch.Draw(target, renderPos - Vector2.UnitX, null, finalColor);
        batch.Draw(target, renderPos + Vector2.UnitX, null, finalColor);

        if (effect is { }) {
            GameplayRenderer.End();
            GameplayRenderer.Begin();
        }

        if (blackOutlineOpt) {
            batch.Draw(target, renderPos, null, Color.White);
        }
        
        RenderTargetHelper.ReturnFullScreenBuffer(target);
    }

    private void NormalRender() {
        var batch = Draw.SpriteBatch;
        
        var spinners = CollectionsMarshal.AsSpan(Spinners);
        foreach (var item in spinners) {
            var color = item.BorderColor;
            var spinnerComponents = CollectionsMarshal.AsSpan(item._images);

            foreach (var img in spinnerComponents) {
                // todo: figure out the offsets properly so that OutlineHelper can be used
                DrawBorder(img, color, item.ImageScale);
            }

            if (item.filler == null)
                continue;
            
            var fillerComponents = item.filler.FillsSpan;

            ref var image = ref fillerComponents[0];
            var mtexture = image.Texture;
            Texture2D texture = image.Texture2D;
            Rectangle? clipRect = new Rectangle?(mtexture.ClipRect);
            float scaleFix = mtexture.ScaleFix;
            Vector2 origin = (mtexture.Center - mtexture.DrawOffset) / scaleFix;
            scaleFix *= item.ImageScale;
            var pos = item.Position + item.ShakeAmt;

            foreach (ref var img in fillerComponents) {
                Vector2 drawPos = img.Position + pos;
                float rotation = img.Rotation;
                batch.Draw(texture, drawPos - Vector2.UnitY, clipRect, color, rotation, origin, scaleFix, SpriteEffects.None, 0f);
                batch.Draw(texture, drawPos + Vector2.UnitY, clipRect, color, rotation, origin, scaleFix, SpriteEffects.None, 0f);
                batch.Draw(texture, drawPos - Vector2.UnitX, clipRect, color, rotation, origin, scaleFix, SpriteEffects.None, 0f);
                batch.Draw(texture, drawPos + Vector2.UnitX, clipRect, color, rotation, origin, scaleFix, SpriteEffects.None, 0f);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void DrawBorder(SealedImage image, Color color, float imgScale) {
        Texture2D texture = image.Texture.Texture.Texture_Safe;
        Rectangle? clipRect = new Rectangle?(image.Texture.ClipRect);
        float scaleFix = image.Texture.ScaleFix;
        Vector2 origin = (image.Origin - image.Texture.DrawOffset) / scaleFix;
        Vector2 drawPos = image.RenderPosition;
        float rotation = image.Rotation;
        scaleFix *= imgScale;
        Draw.SpriteBatch.Draw(texture, drawPos - Vector2.UnitY, clipRect, color, rotation, origin, scaleFix, SpriteEffects.None, 0f);
        Draw.SpriteBatch.Draw(texture, drawPos + Vector2.UnitY, clipRect, color, rotation, origin, scaleFix, SpriteEffects.None, 0f);
        Draw.SpriteBatch.Draw(texture, drawPos - Vector2.UnitX, clipRect, color, rotation, origin, scaleFix, SpriteEffects.None, 0f);
        Draw.SpriteBatch.Draw(texture, drawPos + Vector2.UnitX, clipRect, color, rotation, origin, scaleFix, SpriteEffects.None, 0f);
    }
}