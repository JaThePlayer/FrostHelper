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
    [CustomEntity("FrostHelper/SpringLeft", "FrostHelper/SpringRight", "FrostHelper/SpringFloor")]
    public class CustomSpring : Spring
    {
        string dir;

        Vector2 speedMult;

        private static Dictionary<string, Orientations> EntityDataNameToOrientation = new Dictionary<string, Orientations>()
        {
            ["FrostHelper/SpringLeft"] = Orientations.WallLeft,
            ["FrostHelper/SpringRight"] = Orientations.WallRight,
            ["FrostHelper/SpringFloor"] = Orientations.Floor
        };

        public CustomSpring(EntityData data, Vector2 offset) : this(data, offset, EntityDataNameToOrientation[data.Name]) { }

        public CustomSpring(EntityData data, Vector2 offset, Orientations orientation) : base(data.Position + offset, orientation, data.Bool("playerCanUse", true))
        {
            bool playerCanUse = data.Bool("playerCanUse", true);
            dir = data.Attr("directory", "objects/spring/");

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
            DynData<Spring> dyndata = new DynData<Spring>(this);
            Sprite spr = dyndata.Get<Sprite>("sprite");
            Remove(spr);
            Sprite sprite;
            Add(sprite = new Sprite(GFX.Game, dir));
            sprite.Add("idle", "", 0f, new int[1]);
            sprite.Add("bounce", "", 0.07f, "idle", new int[]
            {
                0,1,2,2,2,2,2,2,2,2,2,3,4,5
            });
            sprite.Add("disabled", "white", 0.07f);
            sprite.Play("idle", false, false);
            sprite.Origin.X = sprite.Width / 2f;
            sprite.Origin.Y = sprite.Height;
            Depth = -8501;

            Add(Wiggler.Create(1f, 4f, delegate (float v)
            {
                sprite.Scale.Y = 1f + v * 0.2f;
            }, false, false));

            switch (orientation)
            {
                case Orientations.Floor:
                    Collider = new Hitbox(16f, 6f, -8f, -6f);
                    pufferCollider.Collider = new Hitbox(16f, 10f, -8f, -10f);
                    break;
                case Orientations.WallLeft:
                    Collider = new Hitbox(6f, 16f, 0f, -8f);
                    pufferCollider.Collider = new Hitbox(12f, 16f, 0f, -8f);
                    sprite.Rotation = 1.57079637f;
                    break;
                case Orientations.WallRight:
                    Collider = new Hitbox(6f, 16f, -6f, -8f);
                    pufferCollider.Collider = new Hitbox(12f, 16f, -12f, -8f);
                    sprite.Rotation = -1.57079637f;
                    break;
                default:
                    throw new Exception("Orientation not supported!");
            }

            dyndata.Set<Sprite>("sprite", sprite);
            OneUse = data.Bool("oneUse", false);
            if (OneUse)
            {
                Add(new Coroutine(OneUseParticleRoutine()));
            }
        }

        private void OnCollide(Player player)
        {
            if (!(player.StateMachine.State == Player.StDreamDash || !playerCanUse))
            {
                switch (Orientation)
                {
                    case Orientations.Floor:
                        if (player.Speed.Y >= 0f)
                        {
                            BounceAnimate();
                            player.SuperBounce(Top);
                            player.Speed.Y *= speedMult.Y;

                            TryBreak();
                        }
                        break;
                    case Orientations.WallLeft:
                        if (player.SideBounce(1, Right, CenterY))
                        {
                            BounceAnimate();
                            player.Speed.X *= speedMult.X;
                            player.Speed.Y *= speedMult.Y;

                            TryBreak();
                        }
                        break;
                    case Orientations.WallRight:
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
            bool flag = h.HitSpring(this);
            if (flag)
            {
                BounceAnimate();
                TryBreak();
                if (h.Entity is Glider)
                {
                    Glider glider = h.Entity as Glider;
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
                    TheoCrystal theo = h.Entity as TheoCrystal;//.Speed = theoSpeed;
                    if (Orientation == Orientations.Floor)
                    {
                        theo.Speed.Y *= speedMult.Y;
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
                    case Orientations.Floor:
                        SceneAs<Level>().Particles.Emit(P_Crumble_Up, 2, Position, Vector2.One * 3f);
                        break;
                    case Orientations.WallRight:
                        SceneAs<Level>().Particles.Emit(P_Crumble_Right, 2, Position, Vector2.One * 2f);
                        break;
                    case Orientations.WallLeft:
                        SceneAs<Level>().Particles.Emit(P_Crumble_Left, 2, Position, Vector2.One * 2f);
                        break;
                }
                yield return 0.25f;
            }
        }

        private bool playerCanUse;
        public bool OneUse;
    }
}
