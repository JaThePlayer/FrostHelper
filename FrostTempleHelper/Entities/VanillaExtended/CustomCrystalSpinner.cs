using System;
using System.Collections;
using System.Collections.Generic;
using Celeste;
using Celeste.Mod;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System.Linq;
using System.Reflection;

namespace FrostHelper
{
    [Tracked(false)]
    public class CustomSpinner : Entity
    {
        // Hooks
        public static void LoadHooks()
        {
            On.Celeste.Mod.Entities.CrystalShatterTrigger.OnEnter += CrystalShatterTrigger_OnEnter;
        }
        
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

        public Color Tint;

        public int ID;

        private void OnChangeMode(Session.CoreModes coreMode)
        {
            this.iceModeNext = (coreMode == Session.CoreModes.Cold);
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
            if (iceMode)
            {
                bgDirectory = directory + "/bg";
                fgDirectory = directory + "/fg";
            }
            else
            {
                bgDirectory = directory + "/hot/bg";
                fgDirectory = directory + "/hot/fg";
            }
            ClearSprites();
            CreateSprites();
            expanded = false;
            orig_Awake(base.Scene);
        }

        public CustomSpinner(EntityData data, Vector2 position, bool attachToSolid, string directory, string destroyColor, bool isCore, string tint) : base(data.Position + position)
        {
            ID = data.ID;
            DashThrough = data.Bool("dashThrough", false);
            this.tint = tint;
            if (tint == "")
                tint = "FFFFFF";
            Tint = Calc.HexToColor(tint);
            this.directory = directory;
            bgDirectory = directory + "/bg";
            fgDirectory = directory + "/fg";
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
            if (attachToSolid)
            {
                base.Add(new StaticMover
                {
                    OnShake = new Action<Vector2>(OnShake),
                    SolidChecker = new Func<Solid, bool>(IsRiding),
                    OnDestroy = new Action(base.RemoveSelf)
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
        
        public override void Awake(Scene scene)
        {
            if (isCore)
            {
                base.Add(new CoreModeListener(new Action<Session.CoreModes>(OnChangeMode)));
                if ((scene as Level).CoreMode == Session.CoreModes.Cold)
                {
                    bgDirectory = directory + "/bg";
                    fgDirectory = directory + "/fg";
                }
                else
                {
                    bgDirectory = directory + "/hot/bg";
                    fgDirectory = directory + "/hot/fg";
                }
            }
            orig_Awake(scene);
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
                if (base.Scene.OnInterval(0.25f, offset) && !InView())
                {
                    Visible = false;
                }
                if (base.Scene.OnInterval(0.05f, offset))
                {
                    Player entity = base.Scene.Tracker.GetEntity<Player>();
                    if (entity != null)
                    {
                        Collidable = (Math.Abs(entity.X - base.X) < 128f && Math.Abs(entity.Y - base.Y) < 128f);
                    }
                }
            }
            if (filler != null)
            {
                filler.Position = Position;
            }

            if (moveWithWind)
            {
                float move = Calc.ClampedMap(Math.Abs((base.Scene as Level).Wind.X), 0f, 800f, 0f, 5f);
                if ((base.Scene as Level).Wind.X < 0) move -= move * 2;
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
            Camera camera = (base.Scene as Level).Camera;
            return base.X > camera.X - 16f && base.Y > camera.Y - 16f && base.X < camera.X + 336f && base.Y < camera.Y + 196f;
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

                if (!SolidCheck(new Vector2(base.X - 4f, base.Y - 4f)))
                {
                    c1 = true;
                    imgCount++;
                }
                if (!SolidCheck(new Vector2(base.X + 4f, base.Y - 4f)))
                {
                    c2 = true;
                    imgCount++;
                }
                if (!SolidCheck(new Vector2(base.X + 4f, base.Y + 4f)))
                {
                    c3 = true;
                    imgCount++;
                }
                if (!SolidCheck(new Vector2(base.X - 4f, base.Y + 4f)))
                {
                    c4 = true;
                    imgCount++;
                }
                // technically this solution is twice as fast! Unfortunately it has side-effects that make this not usable
                /*
                image = new Image(mtexture).CenterOrigin();
                image.Color = Calc.HexToColor(tint);
                Add(image); */
                foreach (Entity entity in base.Scene.Tracker.GetEntities<CustomSpinner>())
                {
                    CustomSpinner crystalStaticSpinner = (CustomSpinner)entity;
                    // crystalStaticSpinner != this
                    if (crystalStaticSpinner.ID > ID && crystalStaticSpinner.AttachToSolid == AttachToSolid && (crystalStaticSpinner.Position - Position).LengthSquared() < 576f)
                    {
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
        private void AddSprite(Vector2 offset)
        {
            if (filler == null)
            {
                base.Scene.Add(filler = new Entity(Position));
                filler.Depth = base.Depth + 1;
            }
            //List<MTexture> atlasSubtextures = GFX.Game.GetAtlasSubtextures(CrystalStaticSpinner.bgTextureLookup[color]);
            List<MTexture> atlasSubtextures = GFX.Game.GetAtlasSubtextures(bgDirectory);
            Image image = new Image(Calc.Random.Choose(atlasSubtextures));
            image.Position = offset;
            image.Rotation = (float)Calc.Random.Choose(0, 1, 2, 3) * 1.57079637f;
            image.CenterOrigin();
            image.Color = Tint;
            filler.Add(image);
        }
        
        private bool SolidCheck(Vector2 position)
        {
            if (AttachToSolid || moveWithWind)
            {
                return false;
            }
            using (List<Solid>.Enumerator enumerator = base.Scene.CollideAll<Solid>(position).GetEnumerator())
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
            filler = null;
            if (border != null)
            {
                border.RemoveSelf();
            }
            border = null;
            //foreach (Image image in base.Components.GetAll<Image>())
            var img = base.Components.GetAll<Image>().ToArray();
            for (int i = img.Length-1; i > -1; i--)
            {
                img[i].RemoveSelf();
            }
            expanded = false;
        }

        private void OnShake(Vector2 pos)
        {
            foreach (Component component in base.Components)
            {
                if (component is Image)
                {
                    (component as Image).Position = pos;
                }
            }
        }

        private bool IsRiding(Solid solid)
        {
            return base.CollideCheck(solid);
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
            
            // Cache things
            /*
            if (AttachToSolid)
            {
                Remove(Get<StaticMover>());
            }
            SpeenCache.Push(this); */
        }

        // Token: 0x060011CF RID: 4559 RVA: 0x00042164 File Offset: 0x00040364
        public void Destroy(bool boss = false)
        {
            if (InView())
            {
                Audio.Play("event:/game/06_reflection/fall_spike_smash", Position);
                Color color = Calc.HexToColor(destroyColor);

                CrystalDebris.Burst(Position, color, boss, 8);
            }
            base.RemoveSelf();
        }

        // Token: 0x060011D1 RID: 4561 RVA: 0x00042250 File Offset: 0x00040450
        public void orig_Awake(Scene scene)
        {
            base.Awake(scene);
            if (InView())
            {
                CreateSprites();
            }
        }
        
        public bool iceMode;
        public string directory;
        public string coldDirectory;
        public string destroyColor;
        public bool isCore;
        // Token: 0x04000BA5 RID: 2981
        public static ParticleType P_Move;

        // Token: 0x04000BA6 RID: 2982
        public const float ParticleInterval = 0.02f;
        
        public bool AttachToSolid;
        
        private Entity filler;
        
        private CustomSpinner.Border border;
        
        private float offset;
        
        private bool expanded;
        
        private int randomSeed;

        private class Border : Entity
        {
            private Image fg;
            private Entity fill;
            private Entity parent;

            public Border(Image fg, Entity fill, Entity parent)
            {
                this.fg = fg;
                this.fill = fill;
                this.parent = parent;
                Depth = parent.Depth + 2;
            }

            public override void Render()
            {
                if (!parent.Visible)
                {
                    return;
                }
                if (fg != null)
                {
                    // new method, faster, only used if the spinner has 4 sprites due to edge cases
                    DrawBorder(fg);
                } else
                {
                    // old method, slower
                    foreach (Component c in parent.Components)
                    {
                        if (c is Image img)
                        {
                            DrawBorder(img);
                        }
                    }
                }
                
                if (fill != null)
                    foreach (Component c in fill.Components)
                    {
                        if (c is Image img)
                        {
                            DrawBorder(img);
                        }  
                    }
            }

            private void DrawBorder(Image image)
            {
                
                Vector2 position = image.Position;

                Color color = image.Color;
                image.Color = Color.Black;

                image.Position = position + new Vector2(0f, -1f);
                image.Render();
                image.Position.Y += 2f; //= position + new Vector2(0f, 1f);
                image.Render();
                image.Position = position + new Vector2(-1f, 0f);
                image.Render();
                image.Position.X += 2f; //= position + new Vector2(1f, 0f);
                image.Render();

                image.Position = position;
                image.Color = color;
            }
        }

        /* private class Border : Entity
        {

            public Border(Entity parent, Entity filler)
            {
                drawing = new Entity[2];
                drawing[0] = parent;
                drawing[1] = filler;
                Depth = parent.Depth + 2;
            }
            
            public override void Render()
            {
                if (!drawing[0].Visible)
                {
                    return;
                }
                DrawBorder(drawing[0]);
                if (drawing[1] != null)
                    DrawBorder(drawing[1]);
            }

            private void DrawBorder(Entity entity)
            {
                foreach (Component component in entity.Components)
                {
                    if (component is Image image)
                    {
                        Color color = image.Color;
                        Vector2 position = image.Position;
                        image.Color = Color.Black;
                        
                        image.Position = position + new Vector2(0f, -1f);
                        image.Render();
                        image.Position = position + new Vector2(0f, 1f);
                        image.Render();
                        image.Position = position + new Vector2(-1f, 0f);
                        image.Render();
                        image.Position = position + new Vector2(1f, 0f);
                        image.Render();
                        image.Color = color;
                        image.Position = position;
                    }
                }
            }
            
            private Entity[] drawing;
        } */
    }
}