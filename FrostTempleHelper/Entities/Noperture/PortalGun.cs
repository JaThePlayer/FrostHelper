using Celeste;
using Celeste.Mod.OutbackHelper;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System.Collections.Generic;

namespace FrostTempleHelper.Entities.azcplo1k
{
    class abcdhr : Component
    {

        //Portal currentCustomPortal = null;
        public static void Load() {
            On.Celeste.Player.WallJumpCheck += Player_WallJumpCheck;
            On.Celeste.Player.ClimbCheck += Player_ClimbCheck;
        }

        

        private static bool Player_ClimbCheck(On.Celeste.Player.orig_ClimbCheck orig, Player self, int dir, int yAdd) {
            if (self.Get<abcdhr>() == null)
                return orig(self, dir, yAdd);
            return false;
        }

        private static bool Player_WallJumpCheck(On.Celeste.Player.orig_WallJumpCheck orig, Player self, int dir) {
            if (self.Get<abcdhr>() == null)
                return orig(self, dir);
            return false;
        }

        public static void Unload() {
            On.Celeste.Player.WallJumpCheck -= Player_WallJumpCheck; 
            On.Celeste.Player.ClimbCheck -= Player_ClimbCheck;
        }


        float shootingCooldown = 0f;
        public abcdhr() : base(true, true)
        {
            foreach (var item in Engine.Scene.Tracker.GetEntities<Portal>())
            {
                UpdatePortal((Portal)item);
            }
        }
    

        public override void Update()
        {
            if ((Entity as Player).Dead)
                return;
            if (updatePortalsNextFrame && !Engine.Scene.Tracker.GetEntity<Player>().Dead) {
                foreach (var item in Engine.Scene.Tracker.GetEntities<Portal>())
                {
                    UpdatePortal((Portal)item);
                }
                updatePortalsNextFrame = false;
            }
            base.Update();
            
            if (Input.Grab.Pressed && shootingCooldown <= 0f) {
                Input.Grab.ConsumePress();
                shootingCooldown = 0f;
                Vector2 aim = Input.GetAimVector((Entity as Player).Facing).EightWayNormal();
                Vector2 bulletPos = Entity.Center;
                bool collided = false;
                Color bulletColor = Color.White;
                int dist = 0;
                while (!collided && dist < 16*8)
                {
                    bulletPos.X += aim.X;
                    foreach (var solid in Scene.Tracker.GetEntities<Solid>())
                    {
                        if (solid.CollidePoint(bulletPos))
                        {
                            if (solid is uadzca ps) {
                                //(Entity as Player).Die(aim);
                                if (aim.X > 0f) {
                                    CreatePortal(new Vector2(ps.Left - 8f, Calc.Clamp(bulletPos.Y,ps.Top + 8,ps.Bottom - 8)), "Left", ps);
                                }
                                if (aim.X < 0f)
                                {
                                    CreatePortal(new Vector2(ps.Right + 8f, Calc.Clamp(bulletPos.Y, ps.Top + 8, ps.Bottom - 8)), "Right", ps);
                                }

                                bulletColor = ps.Color;
                            }
                            collided = true;
                            break;
                        }
                    }
                    if (collided)
                        break;
                    bulletPos.Y += aim.Y;
                    
                    foreach (var solid in Scene.Tracker.GetEntities<Solid>())
                    {
                        if (solid.CollidePoint(bulletPos))
                        {
                            if (solid is uadzca ps)
                            {
                                if (aim.Y > 0f)
                                {
                                    CreatePortal(new Vector2(Calc.Clamp(bulletPos.X, ps.Left + 8, ps.Right - 8), ps.Top - 8f), "Up", ps);
                                }
                                if (aim.Y < 0f)
                                {
                                    CreatePortal(new Vector2(Calc.Clamp(bulletPos.X, ps.Left + 8, ps.Right - 8), ps.Bottom + 8f), "Down", ps);
                                }
                                bulletColor = ps.Color;
                            }
                            collided = true;
                            break;
                        }
                    }
                    if (!SceneAs<Level>().IsInBounds(bulletPos))
                        break;
                    SceneAs<Level>().Particles.Emit(BadelineOldsite.P_Vanish, 1, bulletPos, Vector2.One * 1f, bulletColor);
                    dist++;
                }
                SceneAs<Level>().Particles.Emit(BadelineOldsite.P_Vanish, 8, bulletPos, Vector2.One * 8f, bulletColor);

                
            }
            shootingCooldown -= Engine.DeltaTime;
        }

        public override void Render()
        {
            Draw.Line(Entity.Center, Entity.Center + (Input.GetAimVector((Entity as Player).Facing)*24f), Color.Red);
            base.Render();
        }
        bool updatePortalsNextFrame = false;
        void CreatePortal(Vector2 pos, string dir, uadzca ps)
        {
            var data = new EntityData
            {
                Values = new Dictionary<string, object>
                {
                    { "readyColor", ps.ColorStr },
                    { "direction", dir }
                },
                Position = pos
            };
            var all = Engine.Scene.Tracker.GetEntities<Portal>();
            foreach (var item in all)
            {
                var pdata = new DynData<Portal>((Portal)item);
                if (pdata["custom"] != null /*8&& all.Count((t) => { 
                    ; 
                    return new DynData<Portal>((Portal)t)["custom"] != null;  
                }) > 1*/)
                {
                    item.RemoveSelf();
                    break;
                }
            }

            var p = new Portal(null, data, Vector2.Zero);
            var p2data = new DynData<Portal>(p);
            p2data["custom"] = true;
            //currentCustomPortal = p;
            Scene.Add(p);
            updatePortalsNextFrame = true;
        }

        public static void UpdatePortal(object por) {
            Portal p = (Portal)por;
            /*
                var portals = Engine.Scene.Tracker.GetEntities<Portal>();
                var pdata = new DynData<Portal>(p);
                int targetColor = (int)pdata["readyColor"];
                if (currentCustomPortal != null && p != currentCustomPortal) {
                    int customColor = (int)new DynData<Portal>(currentCustomPortal)["readyColor"];
                    if (targetColor == customColor)
                    {
                        pdata["otherPortal"] = currentCustomPortal;
                        return;
                    }
                }

                if (currentCustomPortal != null && p == currentCustomPortal)
                foreach (var portal in portals)
                {
                    var p2data = new DynData<Portal>((Portal)portal);
                    if ((int)p2data["readyColor"] == targetColor && portal != p) 
                    {
                        pdata["otherPortal"] = portal;
                        return;
                    }
                }*/

            var portals = Engine.Scene.Tracker.GetEntities<Portal>();
            var pdata = new DynData<Portal>(p);
            for (int i = 0; i < portals.Count; i++)
            {
                Portal portal = (Portal)portals[i];
                var p2data = new DynData<Portal>(portal);
                
                if ((int)p2data["readyColor"] == (int)pdata["readyColor"] && portal != p)
                {
                    pdata["otherPortal"] = portal;
                    return;
                }
            }
            //if ((int)new DynData<Portal>(newPortal)["readyColor"] == (int)pdata["readyColor"] && newPortal != p)
            {
                //pdata["otherPortal"] = newPortal;
                //return;
            }
            pdata["otherPortal"] = p;
        }
    }
}
