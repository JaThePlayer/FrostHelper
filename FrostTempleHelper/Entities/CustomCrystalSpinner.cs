using System;
using System.Collections;
using System.Collections.Generic;
using Celeste;
using Celeste.Mod;
using Microsoft.Xna.Framework;
using Monocle;


namespace FrostHelper
{
    [Tracked(false)]
    public class CrystalStaticSpinner : Entity
    {
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
            if (this.iceModeNext != this.iceMode)
            {
                this.iceMode = this.iceModeNext;
                this.ToggleSprite();
            }
        }

        private void ToggleSprite()
        {
            if (this.iceMode)
            {
                this.bgDirectory = directory + "/bg";
                this.fgDirectory = directory + "/fg";
            }
            else
            {
                this.bgDirectory = directory + "/hot/bg";
                this.fgDirectory = directory + "/hot/fg";
            }
            this.ClearSprites();
            this.CreateSprites();
            this.expanded = false;
            this.orig_Awake(base.Scene);
        }

        public CrystalStaticSpinner(EntityData data, Vector2 position, bool attachToSolid, string directory, string destroyColor, bool isCore, string tint) : base(data.Position + position)
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
            List<MTexture> atlasSubtextures = GFX.Game.GetAtlasSubtextures(this.bgDirectory);
            MTexture mtexture = Calc.Random.Choose(atlasSubtextures);
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
            Add(new PlayerCollider(new Action<Player>(this.OnPlayer), null, null));
            Add(new HoldableCollider(new Action<Holdable>(this.OnHoldable), null));
            Add(new LedgeBlocker(null));
            Depth = -8500;
            AttachToSolid = attachToSolid;
            if (attachToSolid)
            {
                base.Add(new StaticMover
                {
                    OnShake = new Action<Vector2>(this.OnShake),
                    SolidChecker = new Func<Solid, bool>(this.IsRiding),
                    OnDestroy = new Action(base.RemoveSelf)
                });
            }
            randomSeed = Calc.Random.Next();
            if (this.isCore)
            {
                Add(new CoreModeListener(new Action<Session.CoreModes>(OnChangeMode)));
            }
            float bloomAlpha = data.Float("bloomAlpha", 0.0f);
            if (bloomAlpha != 0.0f)
                Add(new BloomPoint(this.Collider.Center, bloomAlpha, data.Float("bloomRadius", 0f)));
        }
        
        public override void Awake(Scene scene)
        {
            if (this.isCore)
            {
                base.Add(new CoreModeListener(new Action<Session.CoreModes>(this.OnChangeMode)));
                if ((scene as Level).CoreMode == Session.CoreModes.Cold)
                {
                    this.bgDirectory = directory + "/bg";
                    this.fgDirectory = directory + "/fg";
                }
                else
                {
                    this.bgDirectory = directory + "/hot/bg";
                    this.fgDirectory = directory + "/hot/fg";
                }
            }
            this.orig_Awake(scene);
        }
        
        public void ForceInstantiate()
        {
            this.CreateSprites();
            this.Visible = true;
        }
        
        public override void Update()
        {
            if (!this.Visible)
            {
                this.Collidable = false;
                if (this.InView())
                {
                    this.Visible = true;
                    if (!this.expanded)
                    {
                        this.CreateSprites();
                    }
                }
            }
            else
            {
                base.Update();
                if (base.Scene.OnInterval(0.25f, this.offset) && !this.InView())
                {
                    this.Visible = false;
                }
                if (base.Scene.OnInterval(0.05f, this.offset))
                {
                    Player entity = base.Scene.Tracker.GetEntity<Player>();
                    if (entity != null)
                    {
                        this.Collidable = (Math.Abs(entity.X - base.X) < 128f && Math.Abs(entity.Y - base.Y) < 128f);
                    }
                }
            }
            if (this.filler != null)
            {
                this.filler.Position = this.Position;
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
            //ClearSprites();
            //expanded = false;
            //CreateSprites();
            
        }

        public void MoveH(float moveV)
        {
            if (Engine.DeltaTime == 0f)
            {
                this.LiftSpeed.X = 0f;
            }
            else
            {
                this.LiftSpeed.X = moveV / Engine.DeltaTime;
            }

            int num = (int)moveV;
            if (num != 0)
            {
                this.MoveHExact(num);
            }
        }

        public IEnumerator DummyWalkTo(float x, bool walkBackwards = false, float speedMultiplier = 1f, bool keepWalkingIntoWalls = false)
        {
            if (Math.Abs(this.X - x) > 4f)
            {
                Player player = Scene.Tracker.GetEntity<Player>();
                while (Math.Abs(x - this.X) > 4f && this.Scene != null && (keepWalkingIntoWalls || !this.CollideCheck<Solid>(this.Position + Vector2.UnitX * (float)Math.Sign(x - this.X))))
                {
                    player.Speed.X = Calc.Approach(player.Speed.X, (float)Math.Sign(x - player.X) * 64f * speedMultiplier, 1000f * Engine.DeltaTime);
                    this.Speed.X = Calc.Approach(this.Speed.X, (float)Math.Sign(x - this.X) * 64f * speedMultiplier, 1000f * Engine.DeltaTime);
                    Level level = Scene as Level;
                    level.Camera.Approach(new Vector2(Position.X, level.Camera.Y), 0.02f);
                    player.Position = Position;
                    yield return null;
                }
            }
            yield break;
        }

        

        private bool InView()
        {
            Camera camera = (base.Scene as Level).Camera;
            return base.X > camera.X - 16f && base.Y > camera.Y - 16f && base.X < camera.X + 336f && base.Y < camera.Y + 196f;
        }

        private void CreateSprites()
        {
            if (!this.expanded)
            {
                Calc.PushRandom(this.randomSeed);
                Image image;
                //List<MTexture> atlasSubtextures = GFX.Game.GetAtlasSubtextures(CrystalStaticSpinner.fgTextureLookup[this.color]);
                List<MTexture> atlasSubtextures = GFX.Game.GetAtlasSubtextures(this.fgDirectory);
                MTexture mtexture = Calc.Random.Choose(atlasSubtextures);
                //mtexture. = Calc.HexToColor(this.tint);
                if (!this.SolidCheck(new Vector2(base.X - 4f, base.Y - 4f)))
                {
                    //base.Add(new Image(mtexture.GetSubtexture(0, 0, 14, 14, null)).SetOrigin(12f, 12f));
                    image = new Image(mtexture.GetSubtexture(0, 0, 14, 14, null)).SetOrigin(12f, 12f);
                    image.Color = Tint;
                    base.Add(image);
                }
                if (!this.SolidCheck(new Vector2(base.X + 4f, base.Y - 4f)))
                {
                    //base.Add(new Image(mtexture.GetSubtexture(10, 0, 14, 14, null)).SetOrigin(2f, 12f));
                    image = new Image(mtexture.GetSubtexture(10, 0, 14, 14, null)).SetOrigin(2f, 12f);
                    image.Color = Tint;
                    base.Add(image);
                }
                if (!this.SolidCheck(new Vector2(base.X + 4f, base.Y + 4f)))
                {
                    //base.Add(new Image(mtexture.GetSubtexture(10, 10, 14, 14, null)).SetOrigin(2f, 2f));
                    image = new Image(mtexture.GetSubtexture(10, 10, 14, 14, null)).SetOrigin(2f, 2f);
                    image.Color = Tint;
                    base.Add(image);
                }
                if (!this.SolidCheck(new Vector2(base.X - 4f, base.Y + 4f)))
                {
                    //base.Add(new Image(mtexture.GetSubtexture(0, 10, 14, 14, null)).SetOrigin(12f, 2f));
                    image = new Image(mtexture.GetSubtexture(0, 10, 14, 14, null)).SetOrigin(12f, 2f);
                    image.Color = Tint;
                    base.Add(image);
                } 
                //image = new Image(mtexture).CenterOrigin();
                //image.Color = Calc.HexToColor(this.tint);
                //base.Add(image);
                foreach (Entity entity in base.Scene.Tracker.GetEntities<CrystalStaticSpinner>())
                {
                    CrystalStaticSpinner crystalStaticSpinner = (CrystalStaticSpinner)entity;
                    // crystalStaticSpinner != this
                    if (crystalStaticSpinner.ID > ID && crystalStaticSpinner.AttachToSolid == this.AttachToSolid && (crystalStaticSpinner.Position - this.Position).LengthSquared() < 576f)
                    {
                        this.AddSprite((this.Position + crystalStaticSpinner.Position) / 2f - this.Position);
                    }
                    
                }
                base.Scene.Add(this.border = new CrystalStaticSpinner.Border(this, this.filler));
                this.expanded = true;
                Calc.PopRandom();
            }
        }
        // Token: 0x060011C7 RID: 4551 RVA: 0x00041EDC File Offset: 0x000400DC
        private void AddSprite(Vector2 offset)
        {
            if (this.filler == null)
            {
                base.Scene.Add(this.filler = new Entity(this.Position));
                this.filler.Depth = base.Depth + 1;
            }
            //List<MTexture> atlasSubtextures = GFX.Game.GetAtlasSubtextures(CrystalStaticSpinner.bgTextureLookup[this.color]);
            List<MTexture> atlasSubtextures = GFX.Game.GetAtlasSubtextures(this.bgDirectory);
            Image image = new Image(Calc.Random.Choose(atlasSubtextures));
            image.Position = offset;
            image.Rotation = (float)Calc.Random.Choose(0, 1, 2, 3) * 1.57079637f;
            image.CenterOrigin();
            image.Color = Tint;
            this.filler.Add(image);
        }

        // Token: 0x060011C8 RID: 4552 RVA: 0x00041F84 File Offset: 0x00040184
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

        // Token: 0x060011C9 RID: 4553 RVA: 0x00041FF0 File Offset: 0x000401F0
        private void ClearSprites()
        {
            if (this.filler != null)
            {
                this.filler.RemoveSelf();
            }
            this.filler = null;
            if (this.border != null)
            {
                this.border.RemoveSelf();
            }
            this.border = null;
            foreach (Image image in base.Components.GetAll<Image>())
            {
                image.RemoveSelf();
            }
            this.expanded = false;
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

        // Token: 0x060011CD RID: 4557 RVA: 0x00042102 File Offset: 0x00040302
        private void OnHoldable(Holdable h)
        {
            h.HitSpinner(this);
        }

        // Token: 0x060011CE RID: 4558 RVA: 0x0004210C File Offset: 0x0004030C
        public override void Removed(Scene scene)
        {
            if (this.filler != null && this.filler.Scene == scene)
            {
                this.filler.RemoveSelf();
            }
            if (this.border != null && this.border.Scene == scene)
            {
                this.border.RemoveSelf();
            }
            base.Removed(scene);
        }

        // Token: 0x060011CF RID: 4559 RVA: 0x00042164 File Offset: 0x00040364
        public void Destroy(bool boss = false)
        {
            if (this.InView())
            {
                Audio.Play("event:/game/06_reflection/fall_spike_smash", this.Position);
                Color color = Calc.HexToColor(this.destroyColor);

                CrystalDebris.Burst(this.Position, color, boss, 8);
            }
            base.RemoveSelf();
        }

        // Token: 0x060011D1 RID: 4561 RVA: 0x00042250 File Offset: 0x00040450
        public void orig_Awake(Scene scene)
        {
            base.Awake(scene);
            if (this.InView())
            {
                this.CreateSprites();
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
        
        private CrystalStaticSpinner.Border border;
        
        private float offset;
        
        private bool expanded;
        
        private int randomSeed;
        
        private class Border : Entity
        {
            public Border(Entity parent, Entity filler)
            {
                this.drawing = new Entity[2];
                this.drawing[0] = parent;
                this.drawing[1] = filler;
                base.Depth = parent.Depth + 2;
            }
            
            public override void Render()
            {
                if (!this.drawing[0].Visible)
                {
                    return;
                }
                this.DrawBorder(this.drawing[0]);
                this.DrawBorder(this.drawing[1]);
            }
            
            private void DrawBorder(Entity entity)
            {
                if (entity == null)
                {
                    return;
                }
                foreach (Component component in entity.Components)
                {
                    Image image = component as Image;
                    if (image != null)
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
        }
    }
}