using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace FrostHelper
{
    [CustomEntity("FrostHelper/SpringLeft", "FrostHelper/SpringRight", "FrostHelper/SpringFloor", "FrostHelper/SpringCeiling")]
    public class CustomSpring : Spring
    {
        [OnLoad]
        public static void Load()
        {
            On.Celeste.TheoCrystal.HitSpring += TheoCrystal_HitSpring;
            On.Celeste.Glider.HitSpring += Glider_HitSpring;
            On.Celeste.Puffer.HitSpring += Puffer_HitSpring;
        }

        private static bool Puffer_HitSpring(On.Celeste.Puffer.orig_HitSpring orig, Puffer self, Spring spring)
        {
            if (spring is CustomSpring customSpring && customSpring.Orientation == CustomOrientations.Ceiling)
            {
                if (self.GetValue<Vector2>("hitSpeed").Y <= 0f)
                {
                    self.Invoke("GotoHitSpeed", 224f * Vector2.UnitY);
                    self.MoveTowardsX(spring.CenterX, 4f, null);
                    self.GetValue<Wiggler>("bounceWiggler").Start();
                    self.Invoke("Alert", true, false);
                    return true;
                }
                return false;
            }
            else
            {
                return orig(self, spring);
            }
        }

        private static bool Glider_HitSpring(On.Celeste.Glider.orig_HitSpring orig, Glider self, Spring spring)
        {
            if (spring is CustomSpring customSpring && customSpring.Orientation == CustomOrientations.Ceiling)
            {
                if (!self.Hold.IsHeld && self.Speed.Y <= 0f)
                {
                    self.Speed.X *= 0.5f;
                    self.Speed.Y = -160f;
                    self.SetValue("noGravityTimer", 0.15f);
                    self.GetValue<Wiggler>("wiggler").Start();
                    return true;
                }
                return false;
            }
            else
            {
                return orig(self, spring);
            }
        }

        private static bool TheoCrystal_HitSpring(On.Celeste.TheoCrystal.orig_HitSpring orig, TheoCrystal self, Spring spring)
        {
            if (spring is CustomSpring customSpring && customSpring.Orientation == CustomOrientations.Ceiling)
            {
                if (!self.Hold.IsHeld && self.Speed.Y <= 0f)
                {
                    self.Speed.X *= 0.5f;
                    self.Speed.Y = -160f;
                    self.SetValue("noGravityTimer", 0.15f);
                    return true;
                }
                return false;
            } else
            {
                return orig(self, spring);
            }
        }

        [OnUnload]
        public static void Unload()
        {
            On.Celeste.TheoCrystal.HitSpring -= TheoCrystal_HitSpring;
            On.Celeste.Glider.HitSpring -= Glider_HitSpring;
            On.Celeste.Puffer.HitSpring -= Puffer_HitSpring;
        }


        public enum CustomOrientations {
            Floor,
            WallLeft,
            WallRight,
            Ceiling
        }

        public new CustomOrientations Orientation;
        public bool RenderOutline;
        public Sprite Sprite;

        string dir;

        Vector2 speedMult;

        private static Dictionary<string, CustomOrientations> EntityDataNameToOrientation = new Dictionary<string, CustomOrientations>()
        {
            ["FrostHelper/SpringLeft"] = CustomOrientations.WallLeft,
            ["FrostHelper/SpringRight"] = CustomOrientations.WallRight,
            ["FrostHelper/SpringFloor"] = CustomOrientations.Floor,
            ["FrostHelper/SpringCeiling"] = CustomOrientations.Ceiling,
        };

        private static Dictionary<CustomOrientations, Orientations> CustomToRegularOrientation = new Dictionary<CustomOrientations, Orientations>()
        {
            [CustomOrientations.WallLeft] = Orientations.WallLeft,
            [CustomOrientations.WallRight] = Orientations.WallRight,
            [CustomOrientations.Floor] = Orientations.Floor,
            [CustomOrientations.Ceiling] = Orientations.Floor,
        };

        public CustomSpring(EntityData data, Vector2 offset) : this(data, offset, EntityDataNameToOrientation[data.Name]) { }

        public CustomSpring(EntityData data, Vector2 offset, CustomOrientations orientation) : base(data.Position + offset, CustomToRegularOrientation[orientation], data.Bool("playerCanUse", true))
        {
            bool playerCanUse = data.Bool("playerCanUse", true);
            dir = data.Attr("directory", "objects/spring/");
            RenderOutline = data.Bool("renderOutline", true);
            speedMult = FrostModule.StringToVec2(data.Attr("speedMult", "1"));
            Vector2 position = data.Position + offset;
            DisabledColor = Color.White;
            Orientation = orientation;
            base.Orientation = CustomToRegularOrientation[orientation];
            this.playerCanUse = playerCanUse;
            Remove(Get<PlayerCollider>());
            Add(new PlayerCollider(new Action<Player>(OnCollide), null, null));
            Remove(Get<HoldableCollider>());
            Add(new HoldableCollider(new Action<Holdable>(OnHoldable), null));
            Remove(Get<PufferCollider>());
            PufferCollider pufferCollider = new PufferCollider(new Action<Puffer>(OnPuffer), null);
            Add(pufferCollider);
            DynData<Spring> dyndata = new DynData<Spring>(this);
            Sprite spr = dyndata.Get<Sprite>("sprite");
            Remove(spr);
            Add(Sprite = new Sprite(GFX.Game, dir));
            Sprite.Add("idle", "", 0f, new int[1]);
            Sprite.Add("bounce", "", 0.07f, "idle", new int[]
            {
                0,1,2,2,2,2,2,2,2,2,2,3,4,5
            });
            Sprite.Add("disabled", "white", 0.07f);
            Sprite.Play("idle", false, false);
            Sprite.Origin.X = Sprite.Width / 2f;
            Sprite.Origin.Y = Sprite.Height;
            Depth = -8501;

            Add(Wiggler.Create(1f, 4f, delegate (float v)
            {
                Sprite.Scale.Y = 1f + v * 0.2f;
            }, false, false));

            switch (orientation)
            {
                case CustomOrientations.Floor:
                    Collider = new Hitbox(16f, 6f, -8f, -6f);
                    pufferCollider.Collider = new Hitbox(16f, 10f, -8f, -10f);
                    break;
                case CustomOrientations.WallLeft:
                    Collider = new Hitbox(6f, 16f, 0f, -8f);
                    pufferCollider.Collider = new Hitbox(12f, 16f, 0f, -8f);
                    Sprite.Rotation = MathHelper.PiOver2;
                    break;
                case CustomOrientations.WallRight:
                    Collider = new Hitbox(6f, 16f, -6f, -8f);
                    pufferCollider.Collider = new Hitbox(12f, 16f, -12f, -8f);
                    Sprite.Rotation = -MathHelper.PiOver2;
                    break;
                case CustomOrientations.Ceiling:
                    Collider = new Hitbox(16f, 6f, -8f, 0);
                    pufferCollider.Collider = new Hitbox(16f, 10f, -8f, -4f);
                    Sprite.Rotation = MathHelper.Pi;
                    this.GetValue<StaticMover>("staticMover").SolidChecker = (Solid s) => CollideCheck(s, Position - Vector2.UnitY);
                    this.GetValue<StaticMover>("staticMover").JumpThruChecker = (JumpThru jt) => CollideCheck(jt, Position - Vector2.UnitY);
                    break;
                default:
                    throw new Exception("Orientation not supported!");
            }

            dyndata.Set("sprite", Sprite);
            OneUse = data.Bool("oneUse", false);
            if (OneUse)
            {
                Add(new Coroutine(OneUseParticleRoutine()));
            }
        }

        public override void Render()
        {
            if (Collidable && !RenderOutline)
            {
                Sprite.Render();
            } else
            {
                base.Render();
            }
        }

        private void OnCollide(Player player)
        {
            if (!(player.StateMachine.State == Player.StDreamDash || !playerCanUse))
            {
                switch (Orientation)
                {
                    case CustomOrientations.Floor:
                        if (player.Speed.Y >= 0f)
                        {
                            BounceAnimate();
                            player.SuperBounce(Top);
                            player.Speed.Y *= speedMult.Y;

                            TryBreak();
                        }
                        break;
                    case CustomOrientations.Ceiling:
                        if (player.Speed.Y <= 0f)
                        {
                            BounceAnimate();
                            player.SuperBounce(Bottom + player.Height);
                            player.Speed.Y *= -speedMult.Y;
                            Player_varJumpSpeed.SetValue(player, player.Speed.Y);
                            TryBreak();

                            TimeBasedClimbBlocker.NoClimbTimer = 4f / 60f;
                        }
                        break;
                    case CustomOrientations.WallLeft:
                        if (player.SideBounce(1, Right, CenterY))
                        {
                            BounceAnimate();
                            player.Speed.X *= speedMult.X;
                            player.Speed.Y *= speedMult.Y;

                            TryBreak();
                        }
                        break;
                    case CustomOrientations.WallRight:
                        if (player.SideBounce(-1, Left, CenterY))
                        {
                            player.Speed.X *= speedMult.X;
                            player.Speed.Y *= speedMult.Y;
                            BounceAnimate();

                            TryBreak();
                        }
                        break;
                    default:
                        throw new Exception("Orientation not supported!");
                }
            }
        }

        private static FieldInfo Player_varJumpSpeed = typeof(Player).GetField("varJumpSpeed", BindingFlags.NonPublic | BindingFlags.Instance);

        public void TryBreak()
        {
            if (OneUse)
            {
                Add(new Coroutine(BreakRoutine()));
            }
        }

        public IEnumerator BreakRoutine()
        {
            Collidable = false;
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
            Audio.Play("event:/game/general/platform_disintegrate", Center);
            foreach (Image image in Components.GetAll<Image>())
            {
                SceneAs<Level>().Particles.Emit(CrumblePlatform.P_Crumble, 2, Position + image.Position, Vector2.One * 3f);
            }

            float t = 1f;
            while (t > 0f)
            {
                foreach (Image image in Components.GetAll<Image>())
                {
                    image.Scale = Vector2.One * t;
                    image.Rotation += Engine.DeltaTime * 4;
                }
                t -= Engine.DeltaTime * 4;
                yield return null;
            }
            Visible = false;
            RemoveSelf();
            yield break;
        }

        private void BounceAnimate()
        {
            Spring_BounceAnimate.Invoke(this, null);
        }
        public static MethodInfo Spring_BounceAnimate = typeof(Spring).GetMethod("BounceAnimate", BindingFlags.NonPublic | BindingFlags.Instance);

        private void OnHoldable(Holdable h)
        {
            if (h.HitSpring(this))
            {
                BounceAnimate();
                TryBreak();
                if (h.Entity is Glider)
                {
                    Glider glider = h.Entity as Glider;
                    if (Orientation == CustomOrientations.Floor)
                    {
                        glider.Speed.Y *= speedMult.Y;
                    }
                    else if(Orientation == CustomOrientations.Ceiling)
                    {
                        glider.Speed.Y *= -speedMult.Y;
                    }
                    else
                    {
                        glider.Speed *= speedMult;
                    }
                }
                else if (h.Entity is TheoCrystal)
                {
                    TheoCrystal theo = h.Entity as TheoCrystal;
                    if (Orientation == CustomOrientations.Floor)
                    {
                        theo.Speed.Y *= speedMult.Y;
                    } 
                    else if (Orientation == CustomOrientations.Ceiling)
                    {
                        theo.Speed.Y *= -speedMult.Y;
                    }
                    else
                    {
                        theo.Speed *= speedMult;
                    }
                }
            }
        }

        private void OnPuffer(Puffer p)
        {
            if (p.HitSpring(this))
            {
                Vector2 pufferSpeed = (Vector2)Puffer_hitSpeed.GetValue(p);
                Puffer_hitSpeed.SetValue(p, pufferSpeed * speedMult);
                BounceAnimate();
                TryBreak();
            }
        }
        private FieldInfo Puffer_hitSpeed = typeof(Puffer).GetField("hitSpeed", BindingFlags.Instance | BindingFlags.NonPublic);

        private static ParticleType P_Crumble_Up = new ParticleType
        {
            Color = Calc.HexToColor("847E87"),
            FadeMode = ParticleType.FadeModes.Late,
            Size = 1f,
            Direction = MathHelper.PiOver2,
            SpeedMin = -5f,
            SpeedMax = -25f,
            LifeMin = 0.8f,
            LifeMax = 1f,
            Acceleration = Vector2.UnitY * -20f
        };

        private static ParticleType P_Crumble_Down = new ParticleType
        {
            Color = Calc.HexToColor("847E87"),
            FadeMode = ParticleType.FadeModes.Late,
            Size = 1f,
            Direction = MathHelper.PiOver2,
            SpeedMin = 5f,
            SpeedMax = 25f,
            LifeMin = 0.8f,
            LifeMax = 1f,
            Acceleration = Vector2.UnitY * 20f
        };

        private static ParticleType P_Crumble_Left = new ParticleType
        {
            Color = Calc.HexToColor("847E87"),
            FadeMode = ParticleType.FadeModes.Late,
            Size = 1f,
            Direction = 0f,
            SpeedMin = 5f,
            SpeedMax = 25f,
            LifeMin = 0.8f,
            LifeMax = 1f,
            Acceleration = Vector2.UnitY * 20f
        };

        private static ParticleType P_Crumble_Right = new ParticleType
        {
            Color = Calc.HexToColor("847E87"),
            FadeMode = ParticleType.FadeModes.Late,
            Size = 1f,
            Direction = 0f,
            SpeedMin = -5f,
            SpeedMax = -25f,
            LifeMin = 0.8f,
            LifeMax = 1f,
            Acceleration = Vector2.UnitY * -20f
        };

        private IEnumerator OneUseParticleRoutine()
        {
            while (true)
            {
                switch (Orientation)
                {
                    case CustomOrientations.Floor:
                        SceneAs<Level>().Particles.Emit(P_Crumble_Up, 2, Position, Vector2.One * 3f);
                        break;
                    case CustomOrientations.WallRight:
                        SceneAs<Level>().Particles.Emit(P_Crumble_Right, 2, Position, Vector2.One * 2f);
                        break;
                    case CustomOrientations.WallLeft:
                        SceneAs<Level>().Particles.Emit(P_Crumble_Left, 2, Position, Vector2.One * 2f);
                        break;
                    case CustomOrientations.Ceiling:
                        SceneAs<Level>().Particles.Emit(P_Crumble_Down, 2, Position, Vector2.One * 2f);
                        break;
                }
                yield return 0.25f;
            }
        }

        private bool playerCanUse;
        public bool OneUse;
    }
}
