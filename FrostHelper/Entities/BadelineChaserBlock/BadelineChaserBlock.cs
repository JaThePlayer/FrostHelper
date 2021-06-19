using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace FrostHelper
{
    [CustomEntity("FrostHelper/BadelineChaserBlock")]
    [Tracked]
    public class BadelineChaserBlock : Solid
    {
        public static MTexture SolidBlockTexture;
        public static MTexture PressedBlockTexture;
        static MTexture[,] nineSlicePressed;
        static MTexture[,] nineSliceSolid;

        public static void Load()
        {
            SolidBlockTexture = GFX.Game["objects/FrostHelper/badelineChaserBlock/solid"];
            PressedBlockTexture = GFX.Game["objects/FrostHelper/badelineChaserBlock/pressed"];
            nineSlicePressed = new MTexture[3, 3];
            nineSliceSolid = new MTexture[3, 3];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    nineSlicePressed[i, j] = PressedBlockTexture.GetSubtexture(new Rectangle(i * 8, j * 8, 8, 8));
                    nineSliceSolid[i, j] = SolidBlockTexture.GetSubtexture(new Rectangle(i * 8, j * 8, 8, 8));
                }
            }
        }

        bool Pressed;
        
        public bool Reversed;

        public Sprite Emblem;

        private void ShiftSize(int amount)
        {
            MoveV(amount);
        }
        public StaticMover StaticMover;

        public BadelineChaserBlock(EntityData data, Vector2 offset) : base(data.Position + offset, data.Width, data.Height, false)
        {
            Reversed = data.Bool("reversed", false);
            Collider = new Hitbox(data.Width, data.Height);
            Emblem = new Sprite(GFX.Game, "objects/FrostHelper/badelineChaserBlock/emblem");
            Emblem.Add("solid", !Reversed ? "solidreverse" : "solid");
            Emblem.Add("pressed", !Reversed ? "pressedreverse" : "pressed");
            Emblem.CenterOrigin();
            Emblem.Play(Reversed ? "pressed" : "solid");
            AllowStaticMovers = true;
        }

        bool justChangedState;

        public void SetState(bool state)
        {
            if (Pressed != state)
            {
                //Emblem.Play(Pressed ^ Reversed ? "solid" : "pressed");
                Pressed = state;
                ShiftSize(state ^ Reversed ? 1 : -1);
                
                
                justChangedState = true;
            }
        }
        

        public override void Awake(Scene scene)
        {
            base.Awake(scene);
            Color color = Calc.HexToColor("667da5");
            Color disabledColor = new Color(color.R / 255f * (color.R / 255f), color.G / 255f * (color.G / 255f), color.B / 255f * (color.B / 255f), 1f);
            foreach (var mover in staticMovers)
            {
                Spikes spikes = mover.Entity as Spikes;
                if (spikes != null)
                {
                    spikes.EnabledColor = Color.White;
                    spikes.DisabledColor = disabledColor;
                    spikes.VisibleWhenDisabled = true;
                }
                Spring spring = mover.Entity as Spring;
                if (spring != null)
                {
                    spring.DisabledColor = disabledColor;
                    spring.VisibleWhenDisabled = true;
                }
            }
        }

        public override void Update()
        {
            Player entity = Scene.Tracker.GetEntity<Player>();
            bool lastCollidable = Collidable;
            Collidable = true;
            if (entity != null)
            {
                if (!entity.CollideCheck(this))
                {
                    Collidable = !(Pressed ^ Reversed);
                    if (Pressed ^ Reversed)
                    {
                        DisableStaticMovers();
                    }
                    else
                    {
                        EnableStaticMovers();
                    }
                } else
                {
                    Collidable = lastCollidable;
                }
            } else
            {
                Collidable = !(Pressed ^ Reversed);
                if (Pressed ^ Reversed)
                {
                    DisableStaticMovers();
                }
                else
                {
                    EnableStaticMovers();
                }
            }
                
            if (!Collidable)
            {
                Depth = 8990;
            }
            else
            {
                //if (entity != null && entity.Top >= Bottom - 1f)
                {
                    Depth = 10;
                }
                //else
                {
                    Depth = -10;
                }
            }

            base.Update();

            
        }

        public override void Render()
        {
            base.Render();
            Emblem.Play(Collidable ? "solid" : "pressed");
            DrawBlock(Position, Width, Height, !Collidable ? nineSlicePressed : nineSliceSolid, Emblem, Color.White);
            if (justChangedState)
            {
                
                ShiftSize(Pressed ^ Reversed ? 1 : -1);
                justChangedState = false;
            }
        }

        // stolen from SwapBlock
        // TODO: Use more textures for the middle
        private void DrawBlock(Vector2 pos, float width, float height, MTexture[,] ninSlice, Sprite middle, Color tint)
        {
            int num = (int)(width / 8f);
            int num2 = (int)(height / 8f);
            ninSlice[0, 0].Draw(pos + new Vector2(0f, 0f), Vector2.Zero, tint);
            ninSlice[2, 0].Draw(pos + new Vector2(width - 8f, 0f), Vector2.Zero, tint);
            ninSlice[0, 2].Draw(pos + new Vector2(0f, height - 8f), Vector2.Zero, tint);
            ninSlice[2, 2].Draw(pos + new Vector2(width - 8f, height - 8f), Vector2.Zero, tint);
            for (int i = 1; i < num - 1; i++)
            {
                ninSlice[1, 0].Draw(pos + new Vector2(i * 8, 0f), Vector2.Zero, tint);
                ninSlice[1, 2].Draw(pos + new Vector2(i * 8, height - 8f), Vector2.Zero, tint);
            }
            for (int j = 1; j < num2 - 1; j++)
            {
                ninSlice[0, 1].Draw(pos + new Vector2(0f, j * 8), Vector2.Zero, tint);
                ninSlice[2, 1].Draw(pos + new Vector2(width - 8f, j * 8), Vector2.Zero, tint);
            }
            for (int k = 1; k < num - 1; k++)
            {
                for (int l = 1; l < num2 - 1; l++)
                {
                    ninSlice[1, 1].Draw(pos + new Vector2(k, l) * 8f, Vector2.Zero, tint);
                }
            }
            if (middle != null)
            {
                middle.Color = tint;
                middle.RenderPosition = pos + new Vector2(width / 2f, height / 2f);
                middle.Render();
            }
        }
    }
}
