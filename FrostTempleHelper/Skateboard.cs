using System;
using System.Collections;
using System.Collections.Generic;
using Celeste;
using Celeste.Mod;
using Microsoft.Xna.Framework;
using Monocle;


namespace FrostHelper
{
    public class Skateboard : JumpThru
    {
        public enum Directions
        {
            Left,
            Right,
            Old
        }

        Skateboard.Directions dir;
        bool keepMoving;
        bool hasMoved = false;

        public Skateboard(EntityData entityData, Vector2 offset) : base(entityData.Position + offset + new Vector2(0, 8), 25, false)
        {
            this.dir = entityData.Enum<Skateboard.Directions>("direction", Skateboard.Directions.Old);
            this.hasRoadAndBarriers = false;
            this.startY = entityData.Position.Y + 8;
            base.Depth = 1;
            string sprite = entityData.Attr("sprite", "objects/FrostHelper/skateboard");
            //base.Add(this.bodySprite = new Image(GFX.Game["scenery/car/body"]));
            base.Add(this.bodySprite = new Image(GFX.Game[sprite]));
            this.bodySprite.Active = true;
            bodySprite.Scale = dir == Directions.Right ? new Vector2(-1,1) : new Vector2(1, 1);
            if (dir == Directions.Old)
            {
                bodySprite.Scale = new Vector2(-1, 1);
            }
            this.bodySprite.Origin = new Vector2(bodySprite.Width / 2f, this.bodySprite.Height);
            Hitbox hitbox = new Hitbox(20f, 6f, -10f, -7f);
            Hitbox hitbox2 = new Hitbox(19f, 4f, 8f, -11f);
            base.Collider = new ColliderList(new Collider[]
            {
                hitbox,
             //   hitbox2
            });
            this.speedX = entityData.Float("speed", 90f);
            if (dir == Directions.Left) speedX = -speedX;
            this.SurfaceSoundIndex = 2;
            keepMoving = entityData.Bool("keepMoving", false);
        }

        public override void Added(Scene scene)
        {
            this.orig_Added(scene);
            Level level = scene as Level;
            this.level = base.SceneAs<Level>();
            //if (dir == Directions.Right)
            
            //speedX = (dir == Directions.Right) ? 30f : -30f;
        }

        public override void Update()
        {
            Player player = base.Scene.Tracker.GetEntity<Player>();
            bool flag = base.HasRider();
            if (base.Y > this.startY && (!flag || base.Y > this.startY + 1f))
            {
                float moveV = -10f * Engine.DeltaTime;
                base.MoveV(moveV);
            }
            if (base.Y <= this.startY && !this.didHaveRider && flag)
            {
                base.MoveV(2f);
            }
            if (this.didHaveRider && !flag)
            {
                Audio.Play("event:/game/00_prologue/car_up", this.Position);
            }
            if (flag || (keepMoving == true && hasMoved == true))
            {
                //speedX = (dir == Directions.Right) ? Calc.Approach(speedX, 120f, 250f * Engine.DeltaTime) : Calc.Approach(speedX, -120f, -250f * Engine.DeltaTime);
                if (dir == Directions.Old) speedX = Calc.Approach(speedX, 120f, 250f * Engine.DeltaTime);
                //speedX = (dir == Directions.Right) ? speedX : -speedX;
                bool a = this.MoveHCheck(speedX * Engine.DeltaTime);
                //if (hit) base.MoveH(1f);
                hasMoved = true;
            }

            this.MoveVCheck(75f * Engine.DeltaTime);
            //this.MoveVCheck(speedY);
            this.didHaveRider = flag;
            base.Update();
        }

        public override int GetLandSoundIndex(Entity entity)
        {
            Audio.Play("event:/game/00_prologue/car_down", this.Position);
            return -1;
        }

        public void orig_Added(Scene scene)
        {
            base.Added(scene);
            Level level = scene as Level;
        }

        private bool MoveHCheck(float amount)
        {
            //Level level = scene as Level;
            bool flag = base.MoveHCollideSolidsAndBounds(this.level, amount, true, null);
            bool result;
            if (flag)
            {
                bool flag2 = amount < 0f && base.Left <= (float)this.level.Bounds.Left;
                if (flag2)
                {
                    result = true;
                }
                else
                {
                    bool flag3 = amount > 0f && base.Right >= (float)this.level.Bounds.Right;
                    if (flag3)
                    {
                        result = true;
                    }
                    else
                    {
                        for (int i = 1; i <= 4; i++)
                        {
                            for (int j = 1; j >= -1; j -= 2)
                            {
                                Vector2 value = new Vector2((float)Math.Sign(amount), (float)(i * j));
                                bool flag4 = !base.CollideCheck<Solid>(this.Position + value);
                                if (flag4)
                                {
                                    this.MoveVExact(i * j);
                                    this.MoveHExact(Math.Sign(amount));
                                    return false;
                                }
                            }
                        }
                        result = true;
                    }
                }
            }
            else
            {
                result = false;
            }
            return result;
        }

        private bool MoveVCheck(float amount)
        {
            bool flag = MoveVCollideSolidsAndBounds(this.level, amount, true, null, true);
            bool result;
            if (flag)
            {
                bool flag2 = amount < 0f && base.Top <= (float)this.level.Bounds.Top;
                if (flag2)
                {
                    result = true;
                }
                else
                {
                    bool flag3 = amount > 0f && base.Bottom >= (float)(this.level.Bounds.Bottom + 32);
                    if (flag3)
                    {
                        result = true;
                    }
                    else
                    {
                        for (int i = 1; i <= 4; i++)
                        {
                            for (int j = 1; j >= -1; j -= 2)
                            {
                                Vector2 value = new Vector2((float)(i * j), (float)Math.Sign(amount));
                                bool flag4 = !base.CollideCheck<Solid>(this.Position + value);
                                if (flag4)
                                {
                                    this.MoveHExact(i * j);
                                    this.MoveVExact(Math.Sign(amount));
                                    return false;
                                }
                            }
                        }
                        result = true;
                    }
                }
            }
            else
            {
                result = false;
            }
            return result;
        }

        private Level level;
        float speedX;
        private Image bodySprite;
        private float startY;
        private bool didHaveRider;
        public bool hasRoadAndBarriers;
    }
}