#define SPINNER_BORDERS_USE_RENDER_TARGET
#if SPINNER_BORDERS_USE_RENDER_TARGET
using FrostHelper.ModIntegration;
#endif
using System.Runtime.CompilerServices;

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

        On.Celeste.Mod.Entities.CrystalShatterTrigger.OnEnter += CrystalShatterTrigger_OnEnter;
        On.Celeste.Player.SummitLaunchUpdate += Player_SummitLaunchUpdate;
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

        On.Celeste.Mod.Entities.CrystalShatterTrigger.OnEnter -= CrystalShatterTrigger_OnEnter;
        On.Celeste.Player.SummitLaunchUpdate -= Player_SummitLaunchUpdate;
    }

    // smh
    private static FieldInfo CrystalShatterTrigger_mode = typeof(CrystalShatterTrigger).GetField("mode", BindingFlags.NonPublic | BindingFlags.Instance);
    private static void CrystalShatterTrigger_OnEnter(On.Celeste.Mod.Entities.CrystalShatterTrigger.orig_OnEnter orig, CrystalShatterTrigger self, Player player) {
        var list = self.Scene.Tracker.SafeGetEntities<CustomSpinner>();
        if (list.Count > 0) {
            CrystalShatterTrigger.Modes mode = (CrystalShatterTrigger.Modes) CrystalShatterTrigger_mode.GetValue(self);
            if (mode == CrystalShatterTrigger.Modes.All) {
                Audio.Play("event:/game/06_reflection/boss_spikes_burst");
            }
            foreach (CustomSpinner crystalStaticSpinner in list) {
                if (mode == CrystalShatterTrigger.Modes.All || self.CollideCheck(crystalStaticSpinner)) {
                    crystalStaticSpinner.Destroy(false);
                }
            }
        }
        orig(self, player);
    }
    #endregion

    public string bgDirectory;
    public string fgDirectory;
    public bool iceModeNext;
    public bool moveWithWind;
    public bool DashThrough;
    public string SpritePathSuffix = "";
    public Color Tint;
    public Color BorderColor;
    public int ID;
    public bool Rainbow;
    public bool HasCollider;
    public bool RenderBorder;
    public bool HasDeco;
    public int DestroyDebrisCount;

    public bool RegisteredToRenderers = false;
    public bool SingleFGImage;
    public int AttachGroup;

    private void OnChangeMode(Session.CoreModes coreMode) {
        iceModeNext = coreMode == Session.CoreModes.Cold;
    }

    public CustomSpinner(EntityData data, Vector2 offset) : this(data, offset, data.Bool("attachToSolid", false), data.Attr("directory", "danger/FrostHelper/icecrystal"), data.Attr("destroyColor", "639bff"), data.Bool("isCore", false), data.Attr("tint", "ffffff")) { }

    public CustomSpinner(EntityData data, Vector2 position, bool attachToSolid, string directory, string destroyColor, bool isCore, string tint) : base(data.Position + position) {
        LoadIfNeeded();
        ID = data.ID;

        Rainbow = data.Bool("rainbow", false);
        RenderBorder = data.Bool("drawOutline", true);
        DestroyDebrisCount = data.Int("debrisCount", 8);
        DashThrough = data.Bool("dashThrough", false);
        Tint = ColorHelper.GetColor(tint);
        BorderColor = ColorHelper.GetColor(data.Attr("borderColor", "000000"));
        // for VivHelper compatibility
        SpritePathSuffix = data.Attr("spritePathSuffix", "");

        // Always use a single FG image, which can cause parts of the texture to clip out of solids.
        // Much better for performance.
        // Note that frost helper spinners will attempt to consolidate the sprites anyway,
        // so this is useless for spinners that are not near a solid. 
        SingleFGImage = data.Bool("singleFGImage", false);

        this.directory = directory;
        UpdateDirectoryFields(false);
        moveWithWind = data.Bool("moveWithWind", false);

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

        HasCollider = data.Bool("collidable", true);
        if (HasCollider) {
            Collider = new ColliderList(new Collider[] {
                new Circle(6f, 0f, 0f),
                new Hitbox(16f, 4f, -8f, -3f)
            });
            Add(new PlayerCollider(OnPlayer, null, null));
            Add(new HoldableCollider(OnHoldable, null));
            Add(new LedgeBlocker(null));
        } else {
            Collidable = false;
        }
        Visible = false;
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

            Add(mover);
        }

        randomSeed = Calc.Random.Next();
        if (isCore) {
            Add(new CoreModeListener(OnChangeMode));
        }
        float bloomAlpha = data.Float("bloomAlpha", 0.0f);
        if (bloomAlpha != 0.0f)
            Add(new BloomPoint(Collidable ? Collider.Center : Position + new Vector2(8f, 8f), bloomAlpha, data.Float("bloomRadius", 0f)));


        if (GFX.Game.Has(GetBGSpritePath(false) + "Deco00")) {
            HasDeco = true;
        }
    }

    private string GetBGSpritePath(bool hotCoreMode) {
        return directory + (hotCoreMode ? "/hot/bg" : "/bg") + SpritePathSuffix;
    }

    private string GetFGSpritePath(bool hotCoreMode) {
        return directory + (hotCoreMode ? "/hot/fg" : "/fg") + SpritePathSuffix;
    }

    private void UpdateDirectoryFields(bool hotCoreMode) {
        bgDirectory = GetBGSpritePath(hotCoreMode);
        fgDirectory = GetFGSpritePath(hotCoreMode);
    }

    public static bool ConnectorRendererJustAdded = false;
    public override void Added(Scene scene) {
        base.Added(scene);
        if (!ConnectorRendererJustAdded) {
            GetConnectorRenderer();
            GetBorderRenderer();
            if (HasDeco)
                GetDecoRenderer();
            ConnectorRendererJustAdded = true;
        }
    }

    public override void Awake(Scene scene) {
        ConnectorRendererJustAdded = false;
        if (isCore) {
            Add(new CoreModeListener(OnChangeMode));
            if ((scene as Level)!.CoreMode == Session.CoreModes.Cold) {
                UpdateDirectoryFields(false);
            } else {
                UpdateDirectoryFields(true);
            }
        }
        base.Awake(scene);
        if (InView()) {
            CreateSprites();
        }
    }

    public void UpdateHue() {
        ColorHelper.SetGetHueScene(Scene);
        foreach (Component component in Components) {
            if (component is Image image) {
                image.Color = ColorHelper.GetHue(Position + image.Position);
            }
        }
        if (filler != null) {
            foreach (Component component2 in filler.Components) {
                if (component2 is Image image2) {
                    image2.Color = ColorHelper.GetHue(Position + image2.Position);
                }
            }
        }
    }

    public void ForceInstantiate() {
        CreateSprites();
        Visible = true;
    }

    public override void Update() {
        if (!Visible) {
            Collidable = false;
            if (InView()) {
                RegisterToRenderers();
                Visible = true;
                if (!expanded) {
                    CreateSprites();
                }
                if (Rainbow)
                    UpdateHue();
            }
        } else {
            base.Update();
            if (Rainbow && Scene.OnInterval(0.08f, offset))
                UpdateHue();

            if (Scene.OnInterval(0.25f, offset) && !InView()) {
                Visible = false;
                UnregisterFromRenderers();
            }
            if (HasCollider && Scene.OnInterval(0.05f, offset)) {
                Player player = Scene.Tracker.GetEntity<Player>();
                if (player != null) {
                    Collidable = Math.Abs(player.X - X) < 128f && Math.Abs(player.Y - Y) < 128f;
                }
            }
        }

        if (filler != null) {
            filler.Position = Position;
        }

        if (moveWithWind) {
            float move = Calc.ClampedMap(Math.Abs((Scene as Level)!.Wind.X), 0f, 800f, 0f, 5f);
            if ((Scene as Level)!.Wind.X < 0)
                move -= move * 2;
            MoveH(move);
        }
    }

    public Vector2 Speed = Vector2.Zero;
    public Vector2 LiftSpeed;

    public void MoveHExact(int move) {
        Position.X += move;
        if (HasCollider)
            Collider.Position.X += move;
    }

    public void MoveH(float moveV) {
        if (Engine.DeltaTime == 0f) {
            LiftSpeed.X = 0f;
        } else {
            LiftSpeed.X = moveV / Engine.DeltaTime;
        }

        int num = (int) moveV;
        if (num != 0) {
            MoveHExact(num);
        }
    }

    private bool InView() {
        var camera = (Scene as Level)!.Camera;
        return X > camera.X - 16f && Y > camera.Y - 16f && X < camera.X + 336f && Y < camera.Y + 196f;
    }


    private void UnregisterFromRenderers() {
        if (RegisteredToRenderers) {
            Scene.Tracker.SafeGetEntity<SpinnerConnectorRenderer>()?.Remove(this);
            if (RenderBorder)
                Scene.Tracker.SafeGetEntity<SpinnerBorderRenderer>()?.Remove(this);
            if (HasDeco)
                Scene.Tracker.SafeGetEntity<SpinnerDecoRenderer>()?.Spinners.Remove(this);
            RegisteredToRenderers = false;
        }

    }

    private void RegisterToRenderers() {
        if (!RegisteredToRenderers) {
            Scene.Tracker.SafeGetEntity<SpinnerConnectorRenderer>()?.Add(this);
            if (RenderBorder)
                Scene.Tracker.SafeGetEntity<SpinnerBorderRenderer>()?.Add(this);
            if (HasDeco)
                Scene.Tracker.SafeGetEntity<SpinnerDecoRenderer>()?.Spinners.Add(this);
            RegisteredToRenderers = true;
        }
    }

    private void CreateSprites() {
        if (!expanded) {
            UnregisterFromRenderers();
            RegisterToRenderers();

            Calc.PushRandom(randomSeed);
            Image image;

            List<MTexture> fgSubtextures = GFX.Game.GetAtlasSubtextures(fgDirectory);
            MTexture fgTexture = Calc.Random.Choose(fgSubtextures);

            foreach (Entity entity in Scene.Tracker.SafeGetEntities<CustomSpinner>()) {
                CustomSpinner crystalStaticSpinner = (CustomSpinner) entity;
                if (crystalStaticSpinner.ID > ID && crystalStaticSpinner.AttachGroup == AttachGroup && crystalStaticSpinner.AttachToSolid == AttachToSolid && (crystalStaticSpinner.Position - Position).LengthSquared() < 24f * 24f) {
                    AddSprite((Position + crystalStaticSpinner.Position) / 2f - Position);
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
                image = new Image(fgTexture).CenterOrigin();
                image.Color = Tint;
                Add(image);
                image.Active = false;
            } else {
                // only spawn quarter images if it's needed to avoid edge cases
                AddCornerImages(fgTexture, topLeft, topRight, bottomLeft, bottomRight);
            }
            #endregion

            if (HasDeco) {
                deco ??= new Entity(Position);

                var decoAtlasSubtextures = GFX.Game.GetAtlasSubtextures(fgDirectory + "Deco");
                Image decoImage = new Image(decoAtlasSubtextures[fgSubtextures.IndexOf(fgTexture)]) {
                    Color = Tint,
                    Active = false
                };
                decoImage.CenterOrigin();
                deco.Add(decoImage);
            }

            expanded = true;
            Calc.PopRandom();
        }
    }

    private void AddCornerImages(MTexture mtexture, bool topLeft, bool topRight, bool bottomLeft, bool bottomRight) {
        Image image;

        if (topLeft && topRight) {
            image = new Image(mtexture.GetSubtexture(0, 0, 28, 14, null)).SetOrigin(12f, 12f);
            image.Color = Tint;
            Add(image);
            image.Active = false;
        } else {
            if (topLeft) {
                image = new Image(mtexture.GetSubtexture(0, 0, 14, 14, null)).SetOrigin(12f, 12f);
                image.Color = Tint;
                Add(image);
                image.Active = false;
            }
            if (topRight) {
                image = new Image(mtexture.GetSubtexture(10, 0, 14, 14, null)).SetOrigin(2f, 12f);
                image.Color = Tint;
                Add(image);
                image.Active = false;
            }
        }

        if (bottomLeft && bottomRight) {
            image = new Image(mtexture.GetSubtexture(0, 10, 28, 14, null)).SetOrigin(12f, 2f);
            image.Color = Tint;
            Add(image);
            image.Active = false;
        } else {
            if (bottomLeft) {
                image = new Image(mtexture.GetSubtexture(10, 10, 14, 14, null)).SetOrigin(2f, 2f);
                image.Color = Tint;
                Add(image);
                image.Active = false;
            }
            if (bottomRight) {
                image = new Image(mtexture.GetSubtexture(0, 10, 14, 14, null)).SetOrigin(12f, 2f);
                image.Color = Tint;
                Add(image);
                image.Active = false;
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

    public void AddSprite(Vector2 offset) {
        offset = offset.Floor();

        if (filler == null) {
            filler = new Entity(Position) {
                Depth = Depth + 1,
                Active = false
            };
        }
        List<MTexture> atlasSubtextures = GFX.Game.GetAtlasSubtextures(bgDirectory);
        Image image = new Image(Calc.Random.Choose(atlasSubtextures)) {
            Position = offset,
            Rotation = Calc.Random.Choose(0, 1, 2, 3) * 1.57079637f,
            Color = Tint,
            Active = false
        };
        image.CenterOrigin();
        if (Rainbow)
            image.Color = ColorHelper.GetHue(Scene, Position + offset);
        filler.Add(image);

        if (HasDeco) {
            if (deco is null) {
                deco = new Entity(Position);
            }
            var decoAtlasSubtextures = GFX.Game.GetAtlasSubtextures(bgDirectory + "Deco");
            Image decoImage = new Image(decoAtlasSubtextures[atlasSubtextures.IndexOf(image.Texture)]) {
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
        if (AttachToSolid || moveWithWind) {
            return false;
        }
        foreach (var a in Scene.CollideAll<Solid>(position)) {
            if (a is SolidTiles) {
                return true;
            }
        }
        return false;
    }

    private void OnShake(Vector2 amount) {
        foreach (Component component in Components) {
            if (component is Image image) {
                // change from vanilla: instead of setting the position, add to it.
                image.Position += amount;
            }
        }

        // addition from vanilla: also shake spinner connectors.
        if (filler != null) {
            foreach (Component component in filler.Components) {
                if (component is Image image) {
                    image.Position += amount;
                }
            }
        }
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
        if (filler != null && filler.Scene == scene) {
            filler.RemoveSelf();
            filler = null;
        }

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

    public bool iceMode;
    public string directory;
    public string destroyColor;
    public bool isCore;
    public static ParticleType P_Move => CrystalStaticSpinner.P_Move;
    public const float ParticleInterval = 0.02f;

    public bool AttachToSolid;

    public Entity? filler;
    public Entity? deco;

    private float offset;

    private bool expanded;

    private int randomSeed;
}

[Tracked]
public class SpinnerConnectorRenderer : Entity {
    private HashSet<CustomSpinner> Spinners = new HashSet<CustomSpinner>();

    public SpinnerConnectorRenderer() : base() {
        Active = false;
        Depth = -8500 + 1;
        Tag = Tags.Persistent;
    }

    public override void Render() {
//#if SPINNER_BORDERS_USE_RENDER_TARGET
//        if (Scene.Tracker.GetEntity<SpinnerBorderRenderer>() is { CanUseRenderTargetRender: true })
//            return;
//#endif

        ForceRender();
    }

    public void ForceRender() {
        foreach (var spinner in Spinners) {
            //item.filler?.Render();
            // Entity.Render is hooked by some mods, and has a lot of indirection, let's just do this manually...
            if (spinner.Visible && spinner.filler is { } filler) {
                var fillerComponents = filler.Components;
                Image image = (fillerComponents[0] as Image)!;
                Texture2D texture = image.Texture.Texture.Texture_Safe;
                Rectangle? clipRect = new Rectangle?(image.Texture.ClipRect);
                float scaleFix = image.Texture.ScaleFix;
                Vector2 origin = (image.Origin - image.Texture.DrawOffset) / scaleFix;

                foreach (Image img in fillerComponents) {
                    Draw.SpriteBatch.Draw(texture, img.RenderPosition, clipRect, img.Color, img.Rotation, origin, scaleFix, SpriteEffects.None, 0f);
                }
            }
        }
    }

    public void ForceRenderWithColor(Color c) {
        foreach (var spinner in Spinners) {
            //item.filler?.Render();
            // Entity.Render is hooked by some mods, and has a lot of indirection, let's just do this manually...
            if (spinner.Visible && spinner.filler is { } filler) {
                DrawWithColor(filler, c);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void DrawWithColor(Entity filler, Color color) {
        var fillerComponents = filler.Components;
        Image image = (fillerComponents[0] as Image)!;
        Texture2D texture = image.Texture.Texture.Texture_Safe;
        Rectangle? clipRect = new Rectangle?(image.Texture.ClipRect);
        float scaleFix = image.Texture.ScaleFix;
        Vector2 origin = (image.Origin - image.Texture.DrawOffset) / scaleFix;

        foreach (Image img in fillerComponents) {
            Draw.SpriteBatch.Draw(texture, img.RenderPosition, clipRect, color, img.Rotation, origin, scaleFix, SpriteEffects.None, 0f);
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
    public HashSet<CustomSpinner> Spinners = new HashSet<CustomSpinner>();

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
    public HashSet<CustomSpinner> Spinners = new HashSet<CustomSpinner>();

#if SPINNER_BORDERS_USE_RENDER_TARGET
    private Color? _firstBorderColor = null;
    internal bool CanUseRenderTargetRender;
#endif

    public void Add(CustomSpinner item) {
#if SPINNER_BORDERS_USE_RENDER_TARGET
        if (_firstBorderColor == null) {
            _firstBorderColor = item.BorderColor;
            CanUseRenderTargetRender = true;
        } else if (CanUseRenderTargetRender && (_firstBorderColor != item.BorderColor)) {
            CanUseRenderTargetRender = false;
        }
#endif

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
#if SPINNER_BORDERS_USE_RENDER_TARGET
        if (CanUseRenderTargetRender) {
            var target = RenderTargetHelper<SpinnerBorderRenderer>.Get(true, true);
            var connectorRenderer = Scene.Tracker.SafeGetEntity<SpinnerConnectorRenderer>();
            var batch = Draw.SpriteBatch;
            var borderColor = _firstBorderColor!.Value;

            GameplayRenderer.End();
            Engine.Instance.GraphicsDevice.SetRenderTarget(target);
            Engine.Instance.GraphicsDevice.Clear(Color.Transparent);

            GameplayRenderer.Begin();
            connectorRenderer?.ForceRenderWithColor(borderColor);
            foreach (var spinner in Scene.Tracker.SafeGetEntities<CustomSpinner>()) {
                if (spinner.Visible)
                    foreach (var component in spinner.Components) {
                        if (component is Image img) {
                            var c = img.Color;
                            img.Color = borderColor;
                            img.Render();
                            img.Color = c;
                        }
                    }
            }
            GameplayRenderer.End();

            Engine.Instance.GraphicsDevice.SetRenderTarget(GameplayBuffers.Gameplay);
            GameplayRenderer.Begin();

            // border
            var renderPos = SceneAs<Level>().Camera.Position.Floor();
            
            float scale = 1f / HDlesteCompat.Scale;
            batch.Draw(target, renderPos - Vector2.UnitY, null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            batch.Draw(target, renderPos + Vector2.UnitY, null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            batch.Draw(target, renderPos - Vector2.UnitX, null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            batch.Draw(target, renderPos + Vector2.UnitX, null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

            // might as well render everything now
            // since the outlines are now rendered in black, this won't work anymore
            // TODO:(Perf) use this method when using black outlines
            //batch.Draw(target, renderPos, null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

            return;
        }
#endif

        foreach (var item in Spinners) {
            var color = item.BorderColor;
            var spinnerComponents = item.Components;

            foreach (var component in spinnerComponents) {
                if (component is Image img) {
                    // todo: figure out the offsets properly so that OutlineHelper can be used
                    DrawBorder(img, color);
                }
            }

            if (item.filler != null) {
                item.filler.Position = item.Position;
                var fillerComponents = item.filler.Components;

                Image image = (fillerComponents[0] as Image)!;
                Texture2D texture = image.Texture.Texture.Texture_Safe;
                Rectangle? clipRect = new Rectangle?(image.Texture.ClipRect);
                float scaleFix = image.Texture.ScaleFix;
                Vector2 origin = (image.Origin - image.Texture.DrawOffset) / scaleFix;
                foreach (Image img in fillerComponents) {

                    Vector2 drawPos = img.RenderPosition;
                    float rotation = img.Rotation;
                    Draw.SpriteBatch.Draw(texture, drawPos - Vector2.UnitY, clipRect, color, rotation, origin, scaleFix, SpriteEffects.None, 0f);
                    Draw.SpriteBatch.Draw(texture, drawPos + Vector2.UnitY, clipRect, color, rotation, origin, scaleFix, SpriteEffects.None, 0f);
                    Draw.SpriteBatch.Draw(texture, drawPos - Vector2.UnitX, clipRect, color, rotation, origin, scaleFix, SpriteEffects.None, 0f);
                    Draw.SpriteBatch.Draw(texture, drawPos + Vector2.UnitX, clipRect, color, rotation, origin, scaleFix, SpriteEffects.None, 0f);
                    //img.DrawOutline(color);
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawBorder(Image image, Color color) {
        Texture2D texture = image.Texture.Texture.Texture_Safe;
        Rectangle? clipRect = new Rectangle?(image.Texture.ClipRect);
        float scaleFix = image.Texture.ScaleFix;
        Vector2 origin = (image.Origin - image.Texture.DrawOffset) / scaleFix;
        Vector2 drawPos = image.RenderPosition;
        float rotation = image.Rotation;
        Draw.SpriteBatch.Draw(texture, drawPos - Vector2.UnitY, clipRect, color, rotation, origin, scaleFix, SpriteEffects.None, 0f);
        Draw.SpriteBatch.Draw(texture, drawPos + Vector2.UnitY, clipRect, color, rotation, origin, scaleFix, SpriteEffects.None, 0f);
        Draw.SpriteBatch.Draw(texture, drawPos - Vector2.UnitX, clipRect, color, rotation, origin, scaleFix, SpriteEffects.None, 0f);
        Draw.SpriteBatch.Draw(texture, drawPos + Vector2.UnitX, clipRect, color, rotation, origin, scaleFix, SpriteEffects.None, 0f);
    }
}