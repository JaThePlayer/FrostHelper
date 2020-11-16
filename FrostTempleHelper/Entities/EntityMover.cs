using Monocle;
using System;
using Microsoft.Xna.Framework;
using Celeste;
using Celeste.Mod.Entities;
using Celeste.Mod;
using System.Collections.Generic;
using System.Linq;

namespace FrostHelper
{
    [CustomEntity("FrostHelper/EntityMover")]
    class EntityMover : Entity
    {
        List<Type> Types;
        bool isBlacklist;
        
        Vector2 end;

        // For Tween
        Ease.Easer easer;
        float duration;

        bool mustCollide;

        public EntityMover(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            Collider = new Hitbox(data.Width, data.Height);

            Types = FrostModule.GetTypes(data.Attr("types", "")).ToList();
            isBlacklist = data.Bool("blacklist");

            if (isBlacklist)
            {
                // Some basic types we don't want to move D:
                foreach (Type type in new List<Type>() { typeof(Player), typeof(SolidTiles), typeof(BackgroundTiles), typeof(SpeedrunTimerDisplay), typeof(StrawberriesCounter) })
                    Types.Add(type);
            }

            end = data.FirstNodeNullable(offset).GetValueOrDefault();
            easer = EaseHelper.GetEase(data.Attr("easing", "CubeInOut"));
            duration = data.Float("moveDuration", 1f);
            mustCollide = data.Bool("mustCollide", true);
        }

        public override void Awake(Scene scene)
        {
            base.Awake(scene);
            foreach (Entity entity in scene.Entities)
            {
                if ((!mustCollide || Collider.Collide(entity.Position)) && (Types.Contains(entity.GetType()) != isBlacklist))
                {
                    //entities.Add(entity);
                    var t = Tween.Create(Tween.TweenMode.Looping, easer, duration, true);
                    Vector2 start = entity.Position;
                    bool moveBack = false;
                    t.OnUpdate = (Tween tw) =>
                    {
                        if (moveBack)
                        {
                            tw.Entity.Position = Vector2.Lerp(end, start, t.Eased);
                        }
                        else
                        {
                            tw.Entity.Position = Vector2.Lerp(start, end, t.Eased);
                        }
                    };
                    t.OnComplete = delegate (Tween tw)
                    {
                        moveBack = !moveBack;
                    };
                    entity.Add(t);
                }
            }
        }

        public override void Update()
        {
            base.Update();
            /*
            foreach (var entity in entities)
            {
                if (entity != null)
                {

                }
            } */
        }
    }
}
