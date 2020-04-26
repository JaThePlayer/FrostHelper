using System;
using System.Collections.Generic;
using Celeste;
using Celeste.Mod;
using Microsoft.Xna.Framework;
using Monocle;

namespace FrostHelper
{
    [Tracked(false)]
    [Celeste.Mod.Entities.CustomEntity("FrostHelper/RainbowSpinner")]
    public class RainbowSpinner : Entity
    {

        public RainbowSpinner(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            this.offset = Calc.Random.NextFloat();
            this.color = CrystalColor.Rainbow;
            base.Tag = Tags.TransitionUpdate;
            base.Collider = new ColliderList(new Collider[]
            {
                new Circle(6f, 0f, 0f),
                new Hitbox(16f, 4f, -8f, -3f)
            });
            this.Visible = false;
            base.Add(new PlayerCollider(new Action<Player>(this.OnPlayer), null, null));
            base.Add(new HoldableCollider(new Action<Holdable>(this.OnHoldable), null));
            base.Add(new LedgeBlocker(null));
            base.Depth = -8500;
            this.AttachToSolid = data.Bool("attachToSolid", false);
            if (AttachToSolid)
            {
                base.Add(new StaticMover
                {
                    OnShake = new Action<Vector2>(this.OnShake),
                    SolidChecker = new Func<Solid, bool>(this.IsRiding),
                    OnDestroy = new Action(base.RemoveSelf)
                });
            }
            this.randomSeed = Calc.Random.Next();
        }

        public override void Awake(Scene scene)
        {
            if (this.color == (CrystalColor)(-1))
            {
                if ((scene as Level).CoreMode == Session.CoreModes.Cold)
                {
                    this.color = CrystalColor.Blue;
                }
                else
                {
                    this.color = CrystalColor.Red;
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
            bool flag = !this.Visible;
            if (flag)
            {
                this.Collidable = false;
                bool flag2 = this.InView();
                if (flag2)
                {
                    this.Visible = true;
                    bool flag3 = !this.expanded;
                    if (flag3)
                    {
                        this.CreateSprites();
                    }
                    bool flag4 = this.color == CrystalColor.Rainbow;
                    if (flag4)
                    {
                        this.UpdateHue();
                    }
                }
            }
            else
            {
                base.Update();
                bool flag5 = this.color == CrystalColor.Rainbow && base.Scene.OnInterval(0.08f, this.offset);
                if (flag5)
                {
                    this.UpdateHue();
                }
                bool flag6 = base.Scene.OnInterval(0.25f, this.offset) && !this.InView();
                if (flag6)
                {
                    this.Visible = false;
                }
                bool flag7 = base.Scene.OnInterval(0.05f, this.offset);
                if (flag7)
                {
                    Player entity = base.Scene.Tracker.GetEntity<Player>();
                    bool flag8 = entity != null;
                    if (flag8)
                    {
                        this.Collidable = (Math.Abs(entity.X - base.X) < 128f && Math.Abs(entity.Y - base.Y) < 128f);
                    }
                }
            }
            bool flag9 = this.filler != null;
            if (flag9)
            {
                this.filler.Position = this.Position;
            }
        }

        private void UpdateHue()
        {
            foreach (Component component in base.Components)
            {
                Image image = component as Image;
                bool flag = image != null;
                if (flag)
                {
                    image.Color = this.GetHue(this.Position + image.Position);
                }
            }
            bool flag2 = this.filler != null;
            if (flag2)
            {
                foreach (Component component2 in this.filler.Components)
                {
                    Image image2 = component2 as Image;
                    bool flag3 = image2 != null;
                    if (flag3)
                    {
                        image2.Color = this.GetHue(this.Position + image2.Position);
                    }
                }
            }
        }

        private bool InView()
        {
            Camera camera = (base.Scene as Level).Camera;
            return base.X > camera.X - 16f && base.Y > camera.Y - 16f && base.X < camera.X + 320f + 16f && base.Y < camera.Y + 180f + 16f;
        }

        private void CreateSprites()
        {
            bool flag = !this.expanded;
            if (flag)
            {
                Calc.PushRandom(this.randomSeed);
                List<MTexture> atlasSubtextures = GFX.Game.GetAtlasSubtextures("danger/crystal/fg_white");
                MTexture mtexture = Calc.Random.Choose(atlasSubtextures);
                Color color = Color.White;
                bool flag2 = this.color == CrystalColor.Rainbow;
                if (flag2)
                {
                    color = this.GetHue(this.Position);
                }
                bool flag3 = !this.SolidCheck(new Vector2(base.X - 4f, base.Y - 4f));
                if (flag3)
                {
                    base.Add(new Image(mtexture.GetSubtexture(0, 0, 14, 14, null)).SetOrigin(12f, 12f).SetColor(color));
                }
                bool flag4 = !this.SolidCheck(new Vector2(base.X + 4f, base.Y - 4f));
                if (flag4)
                {
                    base.Add(new Image(mtexture.GetSubtexture(10, 0, 14, 14, null)).SetOrigin(2f, 12f).SetColor(color));
                }
                bool flag5 = !this.SolidCheck(new Vector2(base.X + 4f, base.Y + 4f));
                if (flag5)
                {
                    base.Add(new Image(mtexture.GetSubtexture(10, 10, 14, 14, null)).SetOrigin(2f, 2f).SetColor(color));
                }
                bool flag6 = !this.SolidCheck(new Vector2(base.X - 4f, base.Y + 4f));
                if (flag6)
                {
                    base.Add(new Image(mtexture.GetSubtexture(0, 10, 14, 14, null)).SetOrigin(12f, 2f).SetColor(color));
                }
                List<Entity> entities = base.Scene.Tracker.GetEntities<RainbowSpinner>();
                foreach (Entity entity in entities)
                {
                    RainbowSpinner crystalStaticSpinner = (RainbowSpinner)entity;
                    bool flag7 = crystalStaticSpinner != this && crystalStaticSpinner.AttachToSolid == this.AttachToSolid && crystalStaticSpinner.X >= base.X && (crystalStaticSpinner.Position - this.Position).Length() < 24f;
                    if (flag7)
                    {
                        this.AddSprite((this.Position + crystalStaticSpinner.Position) / 2f - this.Position);
                    }
                }
                base.Scene.Add(this.border = new RainbowSpinner.Border(this, this.filler));
                this.expanded = true;
                Calc.PopRandom();
            }
        }

        private void AddSprite(Vector2 offset)
        {
            bool flag = this.filler == null;
            if (flag)
            {
                base.Scene.Add(this.filler = new Entity(this.Position));
                this.filler.Depth = base.Depth + 1;
            }
            List<MTexture> atlasSubtextures = GFX.Game.GetAtlasSubtextures("danger/crystal/bg_white");
            Image image = new Image(Calc.Random.Choose(atlasSubtextures));
            image.Position = offset;
            image.Rotation = (float)Calc.Random.Choose(0, 1, 2, 3) * 1.57079637f;
            image.CenterOrigin();
            bool flag2 = this.color == CrystalColor.Rainbow;
            if (flag2)
            {
                image.Color = this.GetHue(this.Position + offset);
            }
            this.filler.Add(image);
        }

        private bool SolidCheck(Vector2 position)
        {
            bool attachToSolid = this.AttachToSolid;
            bool result;
            if (attachToSolid)
            {
                result = false;
            }
            else
            {
                List<Solid> list = base.Scene.CollideAll<Solid>(position);
                foreach (Solid solid in list)
                {
                    bool flag = solid is SolidTiles;
                    if (flag)
                    {
                        return true;
                    }
                }
                result = false;
            }
            return result;
        }

        private void ClearSprites()
        {
            bool flag = this.filler != null;
            if (flag)
            {
                this.filler.RemoveSelf();
            }
            this.filler = null;
            bool flag2 = this.border != null;
            if (flag2)
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
                bool flag = component is Image;
                if (flag)
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
            player.Die((player.Position - this.Position).SafeNormalize(), false, true);
        }

        private void OnHoldable(Holdable h)
        {
            h.HitSpinner(this);
        }

        public override void Removed(Scene scene)
        {
            bool flag = this.filler != null && this.filler.Scene == scene;
            if (flag)
            {
                this.filler.RemoveSelf();
            }
            bool flag2 = this.border != null && this.border.Scene == scene;
            if (flag2)
            {
                this.border.RemoveSelf();
            }
            base.Removed(scene);
        }

        public void Destroy(bool boss = false)
        {
            bool flag = this.InView();
            if (flag)
            {
                Audio.Play("event:/game/06_reflection/fall_spike_smash", this.Position);
                Color color = Color.White;
                bool flag2 = this.color == CrystalColor.Red;
                if (flag2)
                {
                    color = Calc.HexToColor("ff4f4f");
                }
                else
                {
                    bool flag3 = this.color == CrystalColor.Blue;
                    if (flag3)
                    {
                        color = Calc.HexToColor("639bff");
                    }
                    else
                    {
                        bool flag4 = this.color == CrystalColor.Purple;
                        if (flag4)
                        {
                            color = Calc.HexToColor("ff4fef");
                        }
                    }
                }
                CrystalDebris.Burst(this.Position, color, boss, 8);
            }
            base.RemoveSelf();
        }

        private Color GetHue(Vector2 position)
        {
            float num = 280f;
            //float value = (position.Length() + base.Scene.TimeActive * 50f) % num / num;
            //float hue = 0.4f + Calc.YoYo(value) * 0.4f;
            int nextIndex = (currentColorIndex + 1) % (Colors.Count);
            //Logger.Log("a", $"{currentColorIndex} -> {(currentColorIndex + 1) % (Colors.Count - 1)}");
            Color color = Color.Lerp(Colors[currentColorIndex], Colors[nextIndex], 1/16);
            if (color.PackedValue == Colors[nextIndex].PackedValue)
            {
                Logger.Log("a", $"{currentColorIndex} => {nextIndex}");
                currentColorIndex = nextIndex;
            }
            currentColor = color;
            return color;
            //return Calc.HsvToColor(hue, 0.4f, 0.9f);
        }

        Color currentColor = Colors[0];
        public void orig_Awake(Scene scene)
        {
            base.Awake(scene);
            bool flag3 = this.InView();
            if (flag3)
            {
                this.CreateSprites();
            }
        }

        /// <summary>
        /// To be edited by a trigger
        /// </summary>
        public static List<Color> Colors = new List<Color>() { Color.White, Color.Yellow, Color.Blue };
        public int currentColorIndex = 0;
        public static ParticleType P_Move;

        public const float ParticleInterval = 0.02f;

        public bool AttachToSolid;

        private Entity filler;

        private RainbowSpinner.Border border;

        private float offset;

        private bool expanded;

        private int randomSeed;

        private CrystalColor color = CrystalColor.Rainbow;

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
                bool flag = !this.drawing[0].Visible;
                if (!flag)
                {
                    this.DrawBorder(this.drawing[0]);
                    this.DrawBorder(this.drawing[1]);
                }
            }

            private void DrawBorder(Entity entity)
            {
                bool flag = entity == null;
                if (!flag)
                {
                    foreach (Component component in entity.Components)
                    {
                        Image image = component as Image;
                        bool flag2 = image != null;
                        if (flag2)
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
            }

            private Entity[] drawing;
        }
    }
}