using System;
using System.Collections.Generic;
using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System.Reflection;
using Microsoft.Xna.Framework.Graphics;
using Celeste.Mod;

namespace FrostHelper
{
    [CustomEntity("FrostHelper/IceSpinner", "FrostHelperExt/CustomBloomSpinner")]
    [Tracked(false)]
    public class CustomSpinner : Entity
    {
        // Hooks
        [OnLoad]
        public static void LoadHooks()
        {
            On.Celeste.Mod.Entities.CrystalShatterTrigger.OnEnter += CrystalShatterTrigger_OnEnter;
        }
        
        [OnUnload]
        public static void UnloadHooks()
        {
            On.Celeste.Mod.Entities.CrystalShatterTrigger.OnEnter -= CrystalShatterTrigger_OnEnter;
        }

        // smh
        private static FieldInfo CrystalShatterTrigger_mode = typeof(CrystalShatterTrigger).GetField("mode", BindingFlags.NonPublic | BindingFlags.Instance);
        private static void CrystalShatterTrigger_OnEnter(On.Celeste.Mod.Entities.CrystalShatterTrigger.orig_OnEnter orig, CrystalShatterTrigger self, Player player)
        {
            var list = self.Scene.Tracker.GetEntities<CustomSpinner>();
            if (list.Count > 0)
            {
                CrystalShatterTrigger.Modes mode = (CrystalShatterTrigger.Modes)CrystalShatterTrigger_mode.GetValue(self);
                if (mode == CrystalShatterTrigger.Modes.All)
                {
                    Audio.Play("event:/game/06_reflection/boss_spikes_burst");
                }
                foreach (CustomSpinner crystalStaticSpinner in list)
                {
                    if (mode == CrystalShatterTrigger.Modes.All || self.CollideCheck(crystalStaticSpinner))
                    {
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
        public int ID;

        private void OnChangeMode(Session.CoreModes coreMode)
        {
            iceModeNext = (coreMode == Session.CoreModes.Cold);
        }
        
        private void CheckModeChange()
        {
            if (iceModeNext != iceMode)
            {
                iceMode = iceModeNext;
                ToggleSprite();
            }
        }

        private void ToggleSprite()
        {
            UpdateDirectoryFields(iceMode);
            ClearSprites();
            CreateSprites();
            expanded = false;
            base.Awake(Scene);
            if (InView())
            {
                CreateSprites();
            }
        }

        public CustomSpinner(EntityData data, Vector2 offset) : this(data, offset, data.Bool("attachToSolid", false), data.Attr("directory", "danger/FrostHelper/icecrystal"), data.Attr("destroyColor", "639bff"), data.Bool("isCore", false), data.Attr("tint", "ffffff")) { }

        public CustomSpinner(EntityData data, Vector2 position, bool attachToSolid, string directory, string destroyColor, bool isCore, string tint) : base(data.Position + position)
        {
            ID = data.ID;
            DashThrough = data.Bool("dashThrough", false);
            this.tint = tint;
            Tint = ColorHelper.GetColor(tint);
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
            Collider = new ColliderList(new Collider[]
            {
                new Circle(6f, 0f, 0f),
                new Hitbox(16f, 4f, -8f, -3f)
            });
            Visible = false;
            Add(new PlayerCollider(new Action<Player>(OnPlayer), null, null));
            Add(new HoldableCollider(new Action<Holdable>(OnHoldable), null));
            Add(new LedgeBlocker(null));
            Depth = -8500;

            AttachToSolid = attachToSolid;
            if (AttachToSolid)
            {
                Add(new StaticMover
                {
                    OnShake = new Action<Vector2>(OnShake),
                    SolidChecker = new Func<Solid, bool>(IsRiding),
                    OnDestroy = new Action(RemoveSelf)
                });
            }

            randomSeed = Calc.Random.Next();
            if (isCore)
            {
                Add(new CoreModeListener(new Action<Session.CoreModes>(OnChangeMode)));
            }
            float bloomAlpha = data.Float("bloomAlpha", 0.0f);
            if (bloomAlpha != 0.0f)
                Add(new BloomPoint(Collider.Center, bloomAlpha, data.Float("bloomRadius", 0f)));
        }

        private string GetBGSpritePath(bool hotCoreMode)
        {
            return directory + (hotCoreMode ? "/hot/bg" : "/bg") + SpritePathSuffix;
        }

        private string GetFGSpritePath(bool hotCoreMode)
        {
            return directory + (hotCoreMode ? "/hot/fg" : "/fg") + SpritePathSuffix;
        }

        private void UpdateDirectoryFields(bool hotCoreMode)
        {
            bgDirectory = GetBGSpritePath(hotCoreMode);
            fgDirectory = GetFGSpritePath(hotCoreMode);
        }

        public override void Awake(Scene scene)
        {
            if (isCore)
            {
                Add(new CoreModeListener(new Action<Session.CoreModes>(OnChangeMode)));
                if ((scene as Level).CoreMode == Session.CoreModes.Cold)
                {
                    UpdateDirectoryFields(false);
                }
                else
                {
                    UpdateDirectoryFields(true);
                }
            }
            base.Awake(scene);
            if (InView())
            {
                CreateSprites();
            }
        }
        
        public void ForceInstantiate()
        {
            CreateSprites();
            Visible = true;
        }
        
        public override void Update()
        {
            if (!Visible)
            {
                Collidable = false;
                if (InView())
                {
                    Visible = true;
                    if (!expanded)
                    {
                        CreateSprites();
                    }
                }
            }
            else
            {   
                base.Update();
                if (Scene.OnInterval(0.25f, offset) && !InView())
                {
                    Visible = false;
                }
                if (Scene.OnInterval(0.05f, offset))
                {
                    Player entity = Scene.Tracker.GetEntity<Player>();
                    if (entity != null)
                    {
                        Collidable = (Math.Abs(entity.X - X) < 128f && Math.Abs(entity.Y - Y) < 128f);
                    }
                }
            }
            
            if (filler != null)
            {
                filler.Position = Position;
            }

            if (moveWithWind)
            {
                float move = Calc.ClampedMap(Math.Abs((Scene as Level).Wind.X), 0f, 800f, 0f, 5f);
                if ((Scene as Level).Wind.X < 0) move -= move * 2;
                MoveH(move);
            }
        }

        public Vector2 Speed = Vector2.Zero;
        public Vector2 LiftSpeed;

        public void MoveHExact(int move)
        {
            Position.X += move;
            Collider.Position.X += move;
        }

        public void MoveH(float moveV)
        {
            if (Engine.DeltaTime == 0f)
            {
                LiftSpeed.X = 0f;
            }
            else
            {
                LiftSpeed.X = moveV / Engine.DeltaTime;
            }

            int num = (int)moveV;
            if (num != 0)
            {
                MoveHExact(num);
            }
        }

        private bool InView()
        {
            Camera camera = (Scene as Level).Camera;
            return X > camera.X - 16f && Y > camera.Y - 16f && X < camera.X + 336f && Y < camera.Y + 196f;
        }

        private void CreateSprites()
        {
            if (!expanded)
            {
                Calc.PushRandom(randomSeed);
                Image image;

                List<MTexture> atlasSubtextures = GFX.Game.GetAtlasSubtextures(fgDirectory);
                MTexture mtexture = Calc.Random.Choose(atlasSubtextures);
                int imgCount = 0;
                bool c1,c2,c3,c4 = false;
                c1 = false;
                c2 = false;
                c3 = false;

                if (!SolidCheck(new Vector2(X - 4f, Y - 4f)))
                {
                    c1 = true;
                    imgCount++;
                }
                if (!SolidCheck(new Vector2(X + 4f, Y - 4f)))
                {
                    c2 = true;
                    imgCount++;
                }
                if (!SolidCheck(new Vector2(X + 4f, Y + 4f)))
                {
                    c3 = true;
                    imgCount++;
                }
                if (!SolidCheck(new Vector2(X - 4f, Y + 4f)))
                {
                    c4 = true;
                    imgCount++;
                }
                // technically this solution is twice as fast! Unfortunately it has side-effects that make this not usable
                /*
                image = new Image(mtexture).CenterOrigin();
                image.Color = Calc.HexToColor(tint);
                Add(image); */
                foreach (Entity entity in Scene.Tracker.GetEntities<CustomSpinner>())
                {
                    CustomSpinner crystalStaticSpinner = (CustomSpinner)entity;
                    if (crystalStaticSpinner.ID > ID && crystalStaticSpinner.AttachToSolid == AttachToSolid && (crystalStaticSpinner.Position - Position).LengthSquared() < 576f)
                    {
                        AddSprite((Position + crystalStaticSpinner.Position) / 2f - Position);
                        AddSprite((Position + crystalStaticSpinner.Position) / 2f - Position);
                    }
                }
                if (imgCount == 4)
                {
                    image = new Image(mtexture).CenterOrigin();
                    image.Color = Tint;
                    Add(image);
                    //image.Visible = false;
                    image.Active = false;
                    Scene.Add(border = new Border(image, filler, this));
                } else
                {
                    // only spawn quarter images if it's needed to avoid edge cases
                    if (c1)
                    {
                        image = new Image(mtexture.GetSubtexture(0, 0, 14, 14, null)).SetOrigin(12f, 12f);
                        image.Color = Tint;
                        Add(image);
                    }
                    if (c2)
                    {
                        image = new Image(mtexture.GetSubtexture(10, 0, 14, 14, null)).SetOrigin(2f, 12f);
                        image.Color = Tint;
                        Add(image);
                    }
                    if (c3)
                    {
                        image = new Image(mtexture.GetSubtexture(10, 10, 14, 14, null)).SetOrigin(2f, 2f);
                        image.Color = Tint;
                        Add(image);
                    }
                    if (c4)
                    {
                        image = new Image(mtexture.GetSubtexture(0, 10, 14, 14, null)).SetOrigin(12f, 2f);
                        image.Color = Tint;
                        Add(image);
                    }
                    Scene.Add(border = new Border(null, filler, this));
                }
                expanded = true;
                Calc.PopRandom();
            }
        }

        public void AddSprite(Vector2 offset)
        {
            if (filler == null)
            {
                filler = new Entity(Position)
                {
                    Depth = Depth + 1,
                    Active = false
                };
                Scene.Add(filler);
            }
            List<MTexture> atlasSubtextures = GFX.Game.GetAtlasSubtextures(bgDirectory);
            Image image = new Image(Calc.Random.Choose(atlasSubtextures))
            {
                Position = offset,
                Rotation = Calc.Random.Choose(0, 1, 2, 3) * 1.57079637f,
                Color = Tint,
                Active = false
            };
            image.CenterOrigin();
            filler.Add(image);
        }
        
        private bool SolidCheck(Vector2 position)
        {
            if (AttachToSolid || moveWithWind)
            {
                return false;
            }
            using (List<Solid>.Enumerator enumerator = Scene.CollideAll<Solid>(position).GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (enumerator.Current is SolidTiles)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        
        private void ClearSprites()
        {
            if (filler != null)
            {
                filler.RemoveSelf();
            }
            if (border != null)
            {
                border.RemoveSelf();
            }
            border = null;
            foreach (Image image in Components.GetAll<Image>())
            {
                image.RemoveSelf();
            }
            expanded = false;
        }

        private void OnShake(Vector2 pos)
        {
            foreach (Component component in Components)
            {
                if (component is Image img)
                {
                    img.Position = pos;
                }
            }
        }

        private bool IsRiding(Solid solid)
        {
            return CollideCheck(solid);
        }

        private void OnPlayer(Player player)
        {
            if (!(DashThrough && player.DashAttacking))
            {
                player.Die((player.Position - Position).SafeNormalize(), false, true);
            }
            
        }
        
        private void OnHoldable(Holdable h)
        {
            h.HitSpinner(this);
        }
        
        public override void Removed(Scene scene)
        {
            if (filler != null && filler.Scene == scene)
            {
                filler.RemoveSelf();
                filler = null;
            }
            if (border != null && border.Scene == scene)
            {
                border.RemoveSelf();
                border = null;
            } 
            base.Removed(scene);
        }
        
        public void Destroy(bool boss = false)
        {
            if (InView())
            {
                Audio.Play("event:/game/06_reflection/fall_spike_smash", Position);
                Color color = Calc.HexToColor(destroyColor);

                CrystalDebris.Burst(Position, color, boss, 8);
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
        
        private Entity filler;
        
        private Border border;
        
        private float offset;
        
        private bool expanded;
        
        private int randomSeed;
        
        private class Border : Entity
        {
            private Image fg;
            private Entity fill;
            private CustomSpinner parent;

            public Border(Image fg, Entity fill, CustomSpinner parent)
            {
                this.fg = fg;
                this.fill = fill;
                this.parent = parent;
                Depth = parent.Depth + 2;
                Active = false;
            }

            public override void Render()
            {
                if (!parent.Visible)
                {
                    return;
                }
                
                if (fg != null)
                {
                    //OutlineHelper.RenderOutline(fg);
                    DrawBorder(fg);
                }
                else
                {
                    // old method, slower
                    foreach (Component c in parent.Components)
                    {
                        if (c is Image img)
                        {
                            DrawBorder(img);
                            //OutlineHelper.RenderOutline(img);
                        }
                    }
                }
                
                if (fill != null)
                    foreach (Component c in fill.Components)
                    {
                        if (c is Image img)
                        {
                            //OutlineHelper.RenderOutline(img);
                            DrawBorder(img);
                        }
                    }
            }

            private void DrawBorder(Image image)
            {
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
}