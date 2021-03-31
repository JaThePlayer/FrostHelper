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
            dir = entityData.Enum<Skateboard.Directions>("direction", Directions.Old);
            hasRoadAndBarriers = false;
            startY = entityData.Position.Y + 8;
            Depth = Depths.Pickups;
            string sprite = entityData.Attr("sprite", "objects/FrostHelper/skateboard");
            //base.Add(this.bodySprite = new Image(GFX.Game["scenery/car/body"]));
            Add(bodySprite = new Image(GFX.Game[sprite]));
            bodySprite.Active = true;
            bodySprite.Scale = dir == Directions.Right ? new Vector2(-1,1) : new Vector2(1, 1);
            if (dir == Directions.Old)
            {
                bodySprite.Scale = new Vector2(-1, 1);
            }
            bodySprite.Origin = new Vector2(bodySprite.Width / 2f, bodySprite.Height);
            Hitbox hitbox = new Hitbox(20f, 6f, -10f, -7f);
            Hitbox hitbox2 = new Hitbox(19f, 4f, 8f, -11f);
            Collider = new ColliderList(new Collider[]
            {
                hitbox,
             //   hitbox2
            });
            speedX = entityData.Float("speed", 90f);
            if (dir == Directions.Left) speedX = -speedX;
            SurfaceSoundIndex = 2;
            keepMoving = entityData.Bool("keepMoving", false);
        }

        public override void Added(Scene scene)
        {
            orig_Added(scene);
            Level level = scene as Level;
            this.level = SceneAs<Level>();
            //if (dir == Directions.Right)
            
            //speedX = (dir == Directions.Right) ? 30f : -30f;
        }

        bool HasNonGhostRider()
        {
            using (List<Entity>.Enumerator enumerator = Scene.Tracker.GetEntities<Actor>().GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    var actor = enumerator.Current;
                    if (((Actor)actor).IsRiding(this) && actor.GetType().Name != "Ghost")
                    {
                        return true;
                    }
                }
            };
            return false;
        }
        
        public override void Update()
        {
            Player player = Scene.Tracker.GetEntity<Player>();
            bool ridden = HasNonGhostRider();
            if (Y > startY && (!ridden || Y > startY + 1f))
            {
                float moveV = -10f * Engine.DeltaTime;
                MoveV(moveV);
            }
            if (Y <= startY && !didHaveRider && ridden)
            {
                MoveV(2f);
            }
            if (didHaveRider && !ridden)
            {
                Audio.Play("event:/game/00_prologue/car_up", Position);
            }
            if (ridden || (keepMoving == true && hasMoved == true))
            {
                //speedX = (dir == Directions.Right) ? Calc.Approach(speedX, 120f, 250f * Engine.DeltaTime) : Calc.Approach(speedX, -120f, -250f * Engine.DeltaTime);
                if (dir == Directions.Old) speedX = Calc.Approach(speedX, 120f, 250f * Engine.DeltaTime);
                //speedX = (dir == Directions.Right) ? speedX : -speedX;
                bool a = MoveHCheck(speedX * Engine.DeltaTime);
                //if (hit) base.MoveH(1f);
                hasMoved = true;
            }

            MoveVCheck(75f * Engine.DeltaTime);
            //this.MoveVCheck(speedY);
            didHaveRider = ridden;
            base.Update();
        }

        public override int GetLandSoundIndex(Entity entity)
        {
            Audio.Play("event:/game/00_prologue/car_down", Position);
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
            bool flag = MoveHCollideSolidsAndBounds(level, amount, true, null);
            bool result;
            if (flag)
            {
                bool flag2 = amount < 0f && Left <= (float)level.Bounds.Left;
                if (flag2)
                {
                    result = true;
                }
                else
                {
                    bool flag3 = amount > 0f && Right >= (float)level.Bounds.Right;
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
                                bool flag4 = !CollideCheck<Solid>(Position + value);
                                if (flag4)
                                {
                                    MoveVExact(i * j);
                                    MoveHExact(Math.Sign(amount));
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
            bool flag = MoveVCollideSolidsAndBounds(level, amount, true, null, true);
            bool result;
            if (flag)
            {
                bool flag2 = amount < 0f && Top <= (float)level.Bounds.Top;
                if (flag2)
                {
                    result = true;
                }
                else
                {
                    bool flag3 = amount > 0f && Bottom >= (float)(level.Bounds.Bottom + 32);
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
                                bool flag4 = !CollideCheck<Solid>(Position + value);
                                if (flag4)
                                {
                                    MoveHExact(i * j);
                                    MoveVExact(Math.Sign(amount));
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