using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FrostHelper
{
    public class CustomSpring : Spring
    {
        string dir;
        //float pufferSpeed;
        //Vector2 jellySpeed;
        //Vector2 theoSpeed;
        //Vector2 playerSpeed;

        Vector2 speedMult;
        public CustomSpring(EntityData data, Vector2 offset, Spring.Orientations orientation) : base(data.Position + offset, orientation, data.Bool("playerCanUse", true))
        {
            bool playerCanUse = data.Bool("playerCanUse", true);
            dir = data.Attr("directory", "objects/spring/");
            //jellySpeed = FrostModule.StringToVec2(data.Attr("jellySpeed", "160,-80"));
            //theoSpeed = FrostModule.StringToVec2(data.Attr("theoSpeed", "220,-80"));
            //playerSpeed = FrostModule.StringToVec2(data.Attr("playerSpeed", "-185,240"));
            //pufferSpeed = data.Float("pufferSpeed", -185f);
            speedMult = FrostModule.StringToVec2(data.Attr("speedMult", "1"));
            Vector2 position = data.Position + offset;
            DisabledColor = Color.White;
            Orientation = orientation;
            this.playerCanUse = playerCanUse;
            Remove(Get<PlayerCollider>());
            Add(new PlayerCollider(new Action<Player>(OnCollide), null, null));
            Remove(Get<HoldableCollider>());
            Add(new HoldableCollider(new Action<Holdable>(OnHoldable), null));
            Remove(Get<PufferCollider>());
            PufferCollider pufferCollider = new PufferCollider(new Action<Puffer>(OnPuffer), null);
            Add(pufferCollider);
            DynData<Spring> dyndata = new DynData<Spring>(this as Spring);
            Sprite spr = dyndata.Get<Sprite>("sprite");
            Remove(spr);
            Sprite sprite;
            Add(sprite = new Sprite(GFX.Game, dir));
            sprite.Add("idle", "", 0f, new int[1]);
            sprite.Add("bounce", "", 0.07f, "idle", new int[]
            {
                0,
                1,
                2,
                2,
                2,
                2,
                2,
                2,
                2,
                2,
                2,
                3,
                4,
                5
            });
            sprite.Add("disabled", "white", 0.07f);
            sprite.Play("idle", false, false);
            sprite.Origin.X = sprite.Width / 2f;
            sprite.Origin.Y = sprite.Height;
            Depth = -8501;
            /*
            this.staticMover = new StaticMover();
            this.staticMover.OnAttach = delegate (Celeste.Platform p)
            {
                base.Depth = p.Depth + 1;
            };
            bool flag = orientation == Spring.Orientations.Floor;
            if (flag)
            {
                this.staticMover.SolidChecker = ((Solid s) => base.CollideCheck(s, this.Position + Vector2.UnitY));
                this.staticMover.JumpThruChecker = ((JumpThru jt) => base.CollideCheck(jt, this.Position + Vector2.UnitY));
                base.Add(this.staticMover);
            }
            else
            {
                bool flag2 = orientation == Spring.Orientations.WallLeft;
                if (flag2)
                {
                    this.staticMover.SolidChecker = ((Solid s) => base.CollideCheck(s, this.Position - Vector2.UnitX));
                    this.staticMover.JumpThruChecker = ((JumpThru jt) => base.CollideCheck(jt, this.Position - Vector2.UnitX));
                    base.Add(this.staticMover);
                }
                else
                {
                    bool flag3 = orientation == Spring.Orientations.WallRight;
                    if (flag3)
                    {
                        this.staticMover.SolidChecker = ((Solid s) => base.CollideCheck(s, this.Position + Vector2.UnitX));
                        this.staticMover.JumpThruChecker = ((JumpThru jt) => base.CollideCheck(jt, this.Position + Vector2.UnitX));
                        base.Add(this.staticMover);
                    }
                }
            } */
            Add(wiggler = Wiggler.Create(1f, 4f, delegate (float v)
            {
                sprite.Scale.Y = 1f + v * 0.2f;
            }, false, false));
            bool flag4 = orientation == Orientations.Floor;
            if (flag4)
            {
                Collider = new Hitbox(16f, 6f, -8f, -6f);
                pufferCollider.Collider = new Hitbox(16f, 10f, -8f, -10f);
            }
            else
            {
                bool flag5 = orientation == Orientations.WallLeft;
                if (flag5)
                {
                    Collider = new Hitbox(6f, 16f, 0f, -8f);
                    pufferCollider.Collider = new Hitbox(12f, 16f, 0f, -8f);
                    sprite.Rotation = 1.57079637f;
                }
                else
                {
                    bool flag6 = orientation == Orientations.WallRight;
                    if (!flag6)
                    {
                        throw new Exception("Orientation not supported!");
                    }
                    Collider = new Hitbox(6f, 16f, -6f, -8f);
                    pufferCollider.Collider = new Hitbox(12f, 16f, -12f, -8f);
                    sprite.Rotation = -1.57079637f;
                }
            }
            dyndata.Set<Sprite>("sprite", sprite);
        }
        /*
        private void OnEnable()
        {
            this.Visible = (this.Collidable = true);
            this.sprite.Color = Color.White;
            this.sprite.Play("idle", false, false);
        }

        private void OnDisable()
        {
            this.Collidable = false;
            bool visibleWhenDisabled = this.VisibleWhenDisabled;
            if (visibleWhenDisabled)
            {
                this.sprite.Play("disabled", false, false);
                this.sprite.Color = this.DisabledColor;
            }
            else
            {
                this.Visible = false;
            }
        } */

        private void OnCollide(Player player)
        {
            bool flag = player.StateMachine.State == 9 || !playerCanUse;
            if (!flag)
            {
                bool flag2 = Orientation == Orientations.Floor;
                if (flag2)
                {
                    bool flag3 = player.Speed.Y >= 0f;
                    if (flag3)
                    {
                        BounceAnimate();
                        player.SuperBounce(Top);
                        player.Speed.Y *= speedMult.Y;
                    }
                }
                else
                {
                    bool flag4 = Orientation == Orientations.WallLeft;
                    if (flag4)
                    {
                        bool flag5 = player.SideBounce(1, Right, CenterY);
                        if (flag5)
                        {
                            BounceAnimate();
                            player.Speed.X *= speedMult.X;
                            player.Speed.Y *= speedMult.Y;
                        }
                    }
                    else
                    {
                        bool flag6 = Orientation == Orientations.WallRight;
                        if (!flag6)
                        {
                            throw new Exception("Orientation not supported!");
                        }
                        bool flag7 = player.SideBounce(-1, Left, CenterY);
                        if (flag7)
                        {
                            player.Speed.X *= speedMult.X;
                            player.Speed.Y *= speedMult.Y;
                            BounceAnimate();
                        }
                    }
                }
            }
        }

        private void BounceAnimate()
        {
            Spring_BounceAnimate.Invoke(this, null);
        }
        private MethodInfo Spring_BounceAnimate = typeof(Spring).GetMethod("BounceAnimate", BindingFlags.NonPublic | BindingFlags.Instance);

        private void OnHoldable(Holdable h)
        {
            bool flag = h.HitSpring(this);
            if (flag)
            {
                BounceAnimate();
                if (h.Entity is Glider)
                {
                    Glider glider = (h.Entity as Glider);
                    if (Orientation == Orientations.Floor)
                    {
                        glider.Speed.Y *= speedMult.Y;
                    } else
                    {
                        glider.Speed *= speedMult;
                    }
                }
                else if (h.Entity is TheoCrystal)
                {
                    TheoCrystal theo = (h.Entity as TheoCrystal);//.Speed = theoSpeed;
                    if (Orientation == Orientations.Floor)
                    {
                        theo.Speed.Y *= speedMult.Y;
                    }
                    else
                    {
                        theo.Speed *= speedMult;
                    }
                }
                //((h.Entity) as Actor)
            }
        }

        private void OnPuffer(Puffer p)
        {
            bool flag = p.HitSpring(this);
            if (flag)
            {
                Vector2 pufferSpeed = (Vector2)Puffer_hitSpeed.GetValue(p);
                Puffer_hitSpeed.SetValue(p, pufferSpeed * speedMult);
                /*
                switch (Orientation)
                {
                    case Orientations.Floor:
                        // 224f * -Vector2.UnitY
                        Puffer_hitSpeed.SetValue(p, pufferSpeed * speedMult);
                        break;
                    case Orientations.WallLeft:
                        // 280f * Vector2.UnitX
                        Puffer_hitSpeed.SetValue(p, pufferSpeed * speedMult);
                        break;
                    case Orientations.WallRight:
                        // 280f * Vector2.UnitX
                        
                        break;
                } */
                BounceAnimate();
            }
        }
        private FieldInfo Puffer_hitSpeed = typeof(Puffer).GetField("hitSpeed", BindingFlags.Instance | BindingFlags.NonPublic);

        private void OnSeeker(Seeker seeker)
        {
            bool flag = seeker.Speed.Y >= -120f;
            if (flag)
            {
                BounceAnimate();
                seeker.HitSpring();
                seeker.Speed.Y *= speedMult.Y;
            }
        }

        public override void Render()
        {
            base.Render();
        }

        //private Sprite sprite;

        private Wiggler wiggler;

        private bool playerCanUse;

        //public Color DisabledColor;

        //public bool VisibleWhenDisabled;
    }
}
