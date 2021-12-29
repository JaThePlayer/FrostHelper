using System.Runtime.CompilerServices;

namespace FrostHelper {
    [CustomEntity("FrostHelper/IceSpinner", "FrostHelperExt/CustomBloomSpinner")]
    [Tracked(false)]
    public class CustomSpinner : Entity {
        // Hooks
        [OnLoad]
        public static void LoadHooks() {
            On.Celeste.Mod.Entities.CrystalShatterTrigger.OnEnter += CrystalShatterTrigger_OnEnter;
            On.Celeste.Player.SummitLaunchUpdate += Player_SummitLaunchUpdate;
        }

        private static int Player_SummitLaunchUpdate(On.Celeste.Player.orig_SummitLaunchUpdate orig, Player self) {
            var ret = orig(self);
            CustomSpinner crystalStaticSpinner = self.Scene.CollideFirst<CustomSpinner>(new Rectangle((int) (self.X - 4f), (int) (self.Y - 40f), 8, 12));
            if (crystalStaticSpinner != null) {
                crystalStaticSpinner.Destroy(false);
                (self.Scene as Level).Shake(0.3f);
                Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
                Celeste.Celeste.Freeze(0.01f);
            }
            return ret;
        }

        [OnUnload]
        public static void UnloadHooks() {
            On.Celeste.Mod.Entities.CrystalShatterTrigger.OnEnter -= CrystalShatterTrigger_OnEnter;
            On.Celeste.Player.SummitLaunchUpdate -= Player_SummitLaunchUpdate;
        }

        // smh
        private static FieldInfo CrystalShatterTrigger_mode = typeof(CrystalShatterTrigger).GetField("mode", BindingFlags.NonPublic | BindingFlags.Instance);
        private static void CrystalShatterTrigger_OnEnter(On.Celeste.Mod.Entities.CrystalShatterTrigger.orig_OnEnter orig, CrystalShatterTrigger self, Player player) {
            var list = self.Scene.Tracker.GetEntities<CustomSpinner>();
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


        public string bgDirectory;
        public string fgDirectory;
        public bool iceModeNext;
        public string tint = "";
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
        public PlayerCollider PlayerCollider;
        public bool SingleFGImage;
        public int AttachGroup;

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
            UpdateDirectoryFields(iceMode);
            ClearSprites();
            CreateSprites();
            expanded = false;
            base.Awake(Scene);
            if (InView()) {
                CreateSprites();
            }
        }

        public CustomSpinner(EntityData data, Vector2 offset) : this(data, offset, data.Bool("attachToSolid", false), data.Attr("directory", "danger/FrostHelper/icecrystal"), data.Attr("destroyColor", "639bff"), data.Bool("isCore", false), data.Attr("tint", "ffffff")) { }

        public CustomSpinner(EntityData data, Vector2 position, bool attachToSolid, string directory, string destroyColor, bool isCore, string tint) : base(data.Position + position) {
            Rainbow = data.Bool("rainbow", false);
            RenderBorder = data.Bool("drawOutline", true);
            DestroyDebrisCount = data.Int("debrisCount", 8);
            ID = data.ID;
            DashThrough = data.Bool("dashThrough", false);
            this.tint = tint;
            Tint = ColorHelper.GetColor(tint);
            BorderColor = ColorHelper.GetColor(data.Attr("borderColor", "000000"));
            this.directory = directory;

            // for VivHelper compatibility
            SpritePathSuffix = data.Attr("spritePathSuffix", "");

            UpdateDirectoryFields(false);
            moveWithWind = data.Bool("moveWithWind", false);

            // funny story time: this used to exist in older versions of Frost Helper as a leftover.
            // I tried removing it in 1.20.3, but this broke some TASes due to spinner cycles.
            // So now this needs to stay here forever D:
            // List<MTexture> atlasSubtextures = GFX.Game.GetAtlasSubtextures(this.bgDirectory);
            // MTexture mtexture = Calc.Random.Choose(atlasSubtextures);
            // Actually, just calling Random.Next() is enough, so that's nice
            Calc.Random.Next();

            coldDirectory = directory;
            this.destroyColor = destroyColor;
            this.isCore = isCore;
            offset = Calc.Random.NextFloat();
            Tag = Tags.TransitionUpdate;

            HasCollider = data.Bool("collidable", true);
            if (HasCollider) {
                Collider = new ColliderList(new Collider[]
                            {
                new Circle(6f, 0f, 0f),
                new Hitbox(16f, 4f, -8f, -3f)
                            });
                PlayerCollider = new PlayerCollider(new Action<Player>(OnPlayer), null, null);
                Add(PlayerCollider);
                Add(new HoldableCollider(new Action<Holdable>(OnHoldable), null));
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
                    _ => new GroupedStaticMover(AttachGroup)
                };

                mover.OnShake = OnShake;
                mover.SolidChecker = IsRiding;
                mover.OnDestroy = RemoveSelf;

                Add(mover);
            }

            randomSeed = Calc.Random.Next();
            if (isCore) {
                Add(new CoreModeListener(new Action<Session.CoreModes>(OnChangeMode)));
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
                Add(new CoreModeListener(new Action<Session.CoreModes>(OnChangeMode)));
                if ((scene as Level).CoreMode == Session.CoreModes.Cold) {
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
                    Player entity = Scene.Tracker.GetEntity<Player>();
                    if (entity != null) {
                        Collidable = Math.Abs(entity.X - X) < 128f && Math.Abs(entity.Y - Y) < 128f;
                    }
                }
            }

            if (filler != null) {
                filler.Position = Position;
            }

            if (moveWithWind) {
                float move = Calc.ClampedMap(Math.Abs((Scene as Level).Wind.X), 0f, 800f, 0f, 5f);
                if ((Scene as Level).Wind.X < 0)
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
            Camera camera = (Scene as Level).Camera;
            return X > camera.X - 16f && Y > camera.Y - 16f && X < camera.X + 336f && Y < camera.Y + 196f;
        }


        private void UnregisterFromRenderers() {
            if (RegisteredToRenderers) {
                Scene.Tracker.GetEntity<SpinnerConnectorRenderer>()?.Spinners.Remove(this);
                if (RenderBorder)
                    Scene.Tracker.GetEntity<SpinnerBorderRenderer>()?.Spinners.Remove(this);
                if (HasDeco)
                    Scene.Tracker.GetEntity<SpinnerDecoRenderer>()?.Spinners.Remove(this);
                RegisteredToRenderers = false;
            }

        }

        private void RegisterToRenderers() {
            if (!RegisteredToRenderers) {
                Scene.Tracker.GetEntity<SpinnerConnectorRenderer>()?.Spinners.Add(this);
                if (RenderBorder)
                    Scene.Tracker.GetEntity<SpinnerBorderRenderer>()?.Spinners.Add(this);
                if (HasDeco)
                    Scene.Tracker.GetEntity<SpinnerDecoRenderer>()?.Spinners.Add(this);
                RegisteredToRenderers = true;
            }
        }

        private void CreateSprites() {
            if (!expanded) {
                UnregisterFromRenderers();
                RegisterToRenderers();

                Calc.PushRandom(randomSeed);
                Image image;

                List<MTexture> atlasSubtextures = GFX.Game.GetAtlasSubtextures(fgDirectory);
                MTexture mtexture = Calc.Random.Choose(atlasSubtextures);
                int imgCount = 0;
                bool topLeft, topRight, bottomLeft, bottomRight = false;
                topLeft = false;
                topRight = false;
                bottomLeft = false;

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
                // technically this solution is twice as fast! Unfortunately it has side-effects that make this not usable
                /*
                image = new Image(mtexture).CenterOrigin();
                image.Color = Calc.HexToColor(tint);
                Add(image); */
                foreach (Entity entity in Scene.Tracker.GetEntities<CustomSpinner>()) {
                    CustomSpinner crystalStaticSpinner = (CustomSpinner) entity;
                    if (crystalStaticSpinner.ID > ID && crystalStaticSpinner.AttachGroup == AttachGroup && crystalStaticSpinner.AttachToSolid == AttachToSolid && (crystalStaticSpinner.Position - Position).LengthSquared() < 24f * 24f) {
                        AddSprite((Position + crystalStaticSpinner.Position) / 2f - Position);
                        //crystalStaticSpinner.AddSprite((Position + crystalStaticSpinner.Position) / 2f - crystalStaticSpinner.Position);
                    }
                }
                if (imgCount == 4) {
                    image = new Image(mtexture).CenterOrigin();
                    image.Color = Tint;
                    Add(image);
                    image.Active = false;
                    SingleFGImage = true;
                } else {
                    // only spawn quarter images if it's needed to avoid edge cases
                    AddCornerImages(mtexture, topLeft, topRight, bottomLeft, bottomRight);
                    //Scene.Add(border = new Border(null, filler, this));
                }
                if (HasDeco) {
                    if (deco is null) {
                        deco = new Entity(Position);
                    }
                    var decoAtlasSubtextures = GFX.Game.GetAtlasSubtextures(fgDirectory + "Deco");
                    Image decoImage = new Image(decoAtlasSubtextures[atlasSubtextures.IndexOf(mtexture)]) {
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
            SpinnerConnectorRenderer renderer = Scene.Tracker.GetEntity<SpinnerConnectorRenderer>();
            if (renderer is null) {
                renderer = new SpinnerConnectorRenderer();
                Scene.Add(renderer);
            }
            return renderer;
        }

        public SpinnerDecoRenderer GetDecoRenderer() {
            SpinnerDecoRenderer renderer = Scene.Tracker.GetEntity<SpinnerDecoRenderer>();
            if (renderer is null) {
                renderer = new SpinnerDecoRenderer();
                Scene.Add(renderer);
            }
            return renderer;
        }

        public SpinnerBorderRenderer GetBorderRenderer() {
            SpinnerBorderRenderer renderer = Scene.Tracker.GetEntity<SpinnerBorderRenderer>();
            if (renderer is null) {
                renderer = new SpinnerBorderRenderer();
                Scene.Add(renderer);
            }
            return renderer;
        }

        public void AddSprite(Vector2 offset) {
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

        private void ClearSprites() {
            if (filler != null) {
                filler.RemoveSelf();

                filler = null;
            }
            if (border != null) {
                border.RemoveSelf();
            }
            border = null;
            foreach (Image image in Components.GetAll<Image>()) {
                image.RemoveSelf();
            }
            expanded = false;

            UnregisterFromRenderers();
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
            if (border != null && border.Scene == scene) {
                border.RemoveSelf();
                border = null;
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
        public string coldDirectory;
        public string destroyColor;
        public bool isCore;
        public static ParticleType P_Move => CrystalStaticSpinner.P_Move;
        public const float ParticleInterval = 0.02f;

        public bool AttachToSolid;

        public Entity filler;
        public Entity deco;

        private Border border;

        private float offset;

        private bool expanded;

        private int randomSeed;


        private class Border : Entity {
            private Image fg;
            private Entity fill;
            private CustomSpinner parent;

            public Border(Image fg, Entity fill, CustomSpinner parent) {
                this.fg = fg;
                this.fill = fill;
                this.parent = parent;
                Depth = parent.Depth + 2;
                Active = false;
            }

            public override void Render() {
                if (!parent.Visible) {
                    return;
                }

                if (fg != null) {
                    //OutlineHelper.RenderOutline(fg);
                    DrawBorder(fg);
                } else {
                    // old method, slower
                    foreach (Component c in parent.Components) {
                        if (c is Image img) {
                            DrawBorder(img);
                            //OutlineHelper.RenderOutline(img);
                        }
                    }
                }

                if (fill != null)
                    foreach (Component c in fill.Components) {
                        if (c is Image img) {
                            //OutlineHelper.RenderOutline(img);
                            DrawBorder(img);
                        }
                    }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void DrawBorder(Image image) {
                Texture2D texture = image.Texture.Texture.Texture_Safe;
                Rectangle? clipRect = new Rectangle?(image.Texture.ClipRect);
                float scaleFix = image.Texture.ScaleFix;
                Vector2 origin = (image.Origin - image.Texture.DrawOffset) / scaleFix;
                Vector2 drawPos = image.RenderPosition;
                float rotation = image.Rotation;
                Draw.SpriteBatch.Draw(texture, drawPos - Vector2.UnitY, clipRect, Color.Black, rotation, origin, scaleFix, SpriteEffects.None, 0f);
                Draw.SpriteBatch.Draw(texture, drawPos + Vector2.UnitY, clipRect, Color.Black, rotation, origin, scaleFix, SpriteEffects.None, 0f);
                Draw.SpriteBatch.Draw(texture, drawPos - Vector2.UnitX, clipRect, Color.Black, rotation, origin, scaleFix, SpriteEffects.None, 0f);
                Draw.SpriteBatch.Draw(texture, drawPos + Vector2.UnitX, clipRect, Color.Black, rotation, origin, scaleFix, SpriteEffects.None, 0f);
            }
        }
    }


    [Tracked]
    public class SpinnerConnectorRenderer : Entity {
        public HashSet<CustomSpinner> Spinners = new HashSet<CustomSpinner>();

        public SpinnerConnectorRenderer() : base() {
            Active = false;
            Depth = -8500 + 1;
            Tag = Tags.Persistent;
        }

        public override void Render() {
            foreach (var item in Spinners) {
                item.filler?.Render();
            }
        }
    }

    [Tracked]
    public class SpinnerDecoRenderer : Entity {
        public HashSet<CustomSpinner> Spinners = new HashSet<CustomSpinner>();

        public SpinnerDecoRenderer() : base() {
            Active = false;
            //Depth = -8500 - 1;
            Depth = -10000 - 1;
            Tag = Tags.Persistent;
        }

        public override void Render() {
            //Console.WriteLine(Spinners.Count);
            foreach (var item in Spinners) {
                item.deco?.Render();
            }
        }
    }

    [Tracked]
    public class SpinnerBorderRenderer : Entity {
        public HashSet<CustomSpinner> Spinners = new HashSet<CustomSpinner>();

        public SpinnerBorderRenderer() : base() {
            Active = false;
            Depth = -8500 + 2;
            Tag = Tags.Persistent;
        }

        public override void Render() {
            foreach (var item in Spinners) {
                var color = item.BorderColor;
                var spinnerComponents = item.Components;
                if (item.SingleFGImage) {
                    foreach (var component in spinnerComponents) {
                        if (component is Image img) {
                            OutlineHelper.RenderOutline(img, color, true);
                            break;
                        }
                    }
                } else {
                    foreach (var component in spinnerComponents) {
                        if (component is Image img) {
                            // todo: figure out the offsets properly so that OutlineHelper can be used in this case too
                            DrawBorder(img, color);
                        }
                    }
                }


                if (item.filler != null) {
                    item.filler.Position = item.Position;
                    var fillerComponents = item.filler.Components;

                    Image image = fillerComponents[0] as Image;
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

}