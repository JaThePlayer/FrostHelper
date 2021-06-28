using System;
using System.Collections.Generic;
using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace FrostHelper
{
    [CustomEntity("FrostHelper/Skateboard")]
    public class Skateboard : JumpThru
    {
        public enum Directions
        {
            Left,
            Right,
            Old
        }

        Directions dir;
        bool keepMoving;
        bool hasMoved = false;
        bool interactWithEntities = false;

        float speedY = 75f;
        float speedX = 0f;

        public Skateboard(EntityData entityData, Vector2 offset) : base(entityData.Position + offset + new Vector2(0, 8), 25, false)
        {
            dir = entityData.Enum("direction", Directions.Old);
            hasRoadAndBarriers = false;
            startY = entityData.Position.Y + 8;
            Depth = Depths.Pickups;
            string sprite = entityData.Attr("sprite", "objects/FrostHelper/skateboard");
            Add(bodySprite = new Image(GFX.Game[sprite]));
            bodySprite.Active = true;
            bodySprite.Scale = dir == Directions.Right ? new Vector2(-1,1) : new Vector2(1, 1);
            if (dir == Directions.Old)
            {
                bodySprite.Scale = new Vector2(-1, 1);
            }
            bodySprite.Origin = new Vector2(bodySprite.Width / 2f, bodySprite.Height);
            Collider = new Hitbox(20f, 6f, -10f, -7f);
            targetSpeedX = entityData.Float("speed", 90f);
            if (dir == Directions.Left) targetSpeedX = -targetSpeedX;
            SurfaceSoundIndex = 2;
            keepMoving = entityData.Bool("keepMoving", false);
            interactWithEntities = entityData.Bool("interactWithEntities", false);
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);
            level = SceneAs<Level>();
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
        
        private void TrySpringInteractions()
        {
            if (!interactWithEntities)
                return;

            foreach (Entity entity in FrostModule.CollideAll(this))
            {
                SkateboardInteraction interaction;
                if (entity != null && (interaction = entity.Get<SkateboardInteraction>()) != null)
                {
                    interaction.DoInteraction(entity, this);
                } else if (entity is Spring spring)
                {
                    switch (spring.Orientation)
                    {
                        case Spring.Orientations.Floor:
                            speedY = -145f;
                            break;
                        case Spring.Orientations.WallLeft:
                            speedX = Math.Abs(targetSpeedX) * 2f;
                            break;
                        case Spring.Orientations.WallRight:
                            speedX = Math.Abs(targetSpeedX) * -2f;
                            break;
                    }
                    hasMoved = true;
                    CustomSpring.Spring_BounceAnimate.Invoke(spring, null);
                }
            }
            
        }

        public override void Update()
        {
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
            if (ridden || (keepMoving && hasMoved))
            {
                if (dir == Directions.Old) 
                    targetSpeedX = Calc.Approach(targetSpeedX, 120f, 250f * Engine.DeltaTime);

                if (Math.Abs(speedX) < 0.1f)
                    MoveHCheck(targetSpeedX * Engine.DeltaTime);
                hasMoved = true;

                
            }
            TrySpringInteractions();
            speedY = Calc.Approach(speedY, 75f, 5f);
            speedX = Calc.Approach(speedX, 0f, 2.5f);
            MoveVCheck(speedY * Engine.DeltaTime);
            MoveHCheck(speedX * Engine.DeltaTime);
            //this.MoveVCheck(speedY);
            didHaveRider = ridden;
            base.Update();
        }

        public override int GetLandSoundIndex(Entity entity)
        {
            Audio.Play("event:/game/00_prologue/car_down", Position);
            return -1;
        }

        private bool MoveHCheck(float amount)
        {
            //Level level = scene as Level;
            bool flag = MoveHCollideSolidsAndBounds(level, amount, true, null);
            bool result;
            if (flag)
            {
                bool flag2 = amount < 0f && Left <= level.Bounds.Left;
                if (flag2)
                {
                    result = true;
                }
                else
                {
                    bool flag3 = amount > 0f && Right >= level.Bounds.Right;
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
                                Vector2 value = new Vector2(Math.Sign(amount), i * j);
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
                bool flag2 = amount < 0f && Top <= level.Bounds.Top;
                if (flag2)
                {
                    result = true;
                }
                else
                {
                    bool flag3 = amount > 0f && Bottom >= level.Bounds.Bottom + 32;
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
                                Vector2 value = new Vector2(i * j, Math.Sign(amount));
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
        float targetSpeedX;
        private Image bodySprite;
        private float startY;
        private bool didHaveRider;
        public bool hasRoadAndBarriers;
    }
}