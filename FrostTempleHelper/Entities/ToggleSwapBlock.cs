using System;
using System.Collections;
using System.Collections.Generic;
using Celeste;
using Celeste.Mod;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Monocle;

namespace FrostTempleHelper
{
    [Tracked(false)]
    public class ToggleSwapBlock : Solid
    {
        bool distanceBasedSpeed = false;
        string directory = "objects/swapblock";
        bool renderBG = false;
        public ToggleSwapBlock(EntityData data, Vector2 offset) : base(data.Position + offset, (float)data.Width, (float)data.Height, true)
        {
            this.directory = data.Attr("directory", "objects/swapblock"); 
            if (directory == "objects/swapblock")
            {
                directory = data.Attr("sprite", "objects/swapblock");
            }
            this.renderBG = data.Bool("renderBG", false);
            Vector2 node = data.Nodes[0] + offset;
            this.redAlpha = 1f;
            this.start = this.Position;
            this.end = node;
            //this.distanceBasedSpeed = data.Bool("distanceBasedSpeed", true);
            this.maxForwardSpeed = data.Float("speed", 360f) / Vector2.Distance(this.start, this.end);
            //if (this.distanceBasedSpeed) this.maxForwardSpeed = this.maxForwardSpeed / Vector2.Distance(this.start, this.end);
            this.maxBackwardSpeed = this.maxForwardSpeed * 0.4f;
            //this.maxForwardSpeed = 360f / Vector2.Distance(this.start, this.end);
            //this.maxBackwardSpeed = this.maxForwardSpeed * 0.4f;
            this.Direction.X = (float)Math.Sign(this.end.X - this.start.X);
            this.Direction.Y = (float)Math.Sign(this.end.Y - this.start.Y);
            base.Add(new DashListener
            {
                OnDash = new Action<Vector2>(this.OnDash)
            });
            int num = (int)MathHelper.Min(base.X, node.X);
            int num2 = (int)MathHelper.Min(base.Y, node.Y);
            int num3 = (int)MathHelper.Max(base.X + base.Width, node.X + base.Width);
            int num4 = (int)MathHelper.Max(base.Y + base.Height, node.Y + base.Height);
            this.moveRect = new Rectangle(num, num2, num3 - num, num4 - num2);
            //MTexture mtexture = GFX.Game["objects/swapblock/block"];
            //MTexture mtexture2 = GFX.Game["objects/swapblock/blockRed"];
            //MTexture mtexture3 = GFX.Game["objects/swapblock/target"];
            MTexture mtexture = GFX.Game[directory + "/block"];
            MTexture mtexture2 = GFX.Game[directory + "/blockRed"];
            MTexture mtexture3 = GFX.Game[directory + "/target"];
            this.nineSliceGreen = new MTexture[3, 3];
            this.nineSliceRed = new MTexture[3, 3];
            this.nineSliceTarget = new MTexture[3, 3];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    this.nineSliceGreen[i, j] = mtexture.GetSubtexture(new Rectangle(i * 8, j * 8, 8, 8));
                    this.nineSliceRed[i, j] = mtexture2.GetSubtexture(new Rectangle(i * 8, j * 8, 8, 8));
                    this.nineSliceTarget[i, j] = mtexture3.GetSubtexture(new Rectangle(i * 8, j * 8, 8, 8));
                }
            }
            middleGreen = new Sprite(GFX.Game, directory + "/midBlock") /*GFX.SpriteBank.Create("swapBlockLight")*/;
            middleGreen.AddLoop("idle", "", 0.08f);
            middleGreen.Justify = new Vector2(0.5f, 0.5f);
            middleGreen.Play("idle");
            Add(middleGreen);
            middleRed = new Sprite(GFX.Game, directory + "/midBlockRed");
            middleRed.AddLoop("idle", "", 0.08f);
            middleRed.Justify = new Vector2(0.5f, 0.5f);
            middleRed.Play("idle");
            
            Add(middleRed);
            base.Add(new LightOcclude(0.2f));
            base.Depth = -9999;
        }

        //public ToggleSwapBlock(EntityData data, Vector2 offset) : this(data.Position + offset, (float)data.Width, (float)data.Height, data.Nodes[0] + offset)
        //{
        //}

        public override void Awake(Scene scene)
        {
            base.Awake(scene);
            scene.Add(this.path = new ToggleSwapBlock.PathRenderer(this));
        }

        public override void Removed(Scene scene)
        {
            base.Removed(scene);
            Audio.Stop(this.moveSfx, true);
            Audio.Stop(this.returnSfx, true);
        }

        public override void SceneEnd(Scene scene)
        {
            base.SceneEnd(scene);
            Audio.Stop(this.moveSfx, true);
            Audio.Stop(this.returnSfx, true);
        }

        private void OnDash(Vector2 direction)
        {
            this.Swapping = (this.lerp < 1f);
            //if (target == 1) this.target = 0; else this.target = 1;
            Audio.Stop(this.returnSfx, true);
            Audio.Stop(this.moveSfx, true);
            if (target == 1)
            {
                returnSfx = Audio.Play("event:/game/05_mirror_temple/swapblock_return", base.Center);
                target = 0;
            }
            else
            {
                moveSfx = Audio.Play("event:/game/05_mirror_temple/swapblock_move", base.Center);
                target = 1;
            }
            //target = (target == 1) ? 0 : 1;
            this.returnTimer = 0.8f;
            this.burst = (base.Scene as Level).Displacement.AddBurst(base.Center, 0.2f, 0f, 16f, 1f, null, null);
            if (this.lerp >= 0.2f)
            {
                this.speed = this.maxForwardSpeed;
            }
            else
            {
                this.speed = MathHelper.Lerp(this.maxForwardSpeed * 0.333f, this.maxForwardSpeed, this.lerp / 0.2f);
            }
            //Audio.Stop(this.returnSfx, true);
            //Audio.Stop(this.moveSfx, true);
            if (!this.Swapping)
            {
                Audio.Play("event:/game/05_mirror_temple/swapblock_move_end", base.Center);
                return;
            }
            //this.moveSfx = Audio.Play("event:/game/05_mirror_temple/swapblock_move", base.Center);
        }

        public override void Update()
        {
            base.Update();
            if (this.returnTimer > 0f)
            {
                this.returnTimer -= Engine.DeltaTime;
                if (this.returnTimer <= 0f)
                {
                    //this.target = 0;
                    this.speed = 0f;
                    //this.returnSfx = Audio.Play("event:/game/05_mirror_temple/swapblock_return", base.Center);
                }
            }
            if (this.burst != null)
            {
                this.burst.Position = base.Center;
            }
            this.redAlpha = Calc.Approach(this.redAlpha, (float)((this.target == 1) ? 0 : 1), Engine.DeltaTime * 32f);
            if (this.target == 0 && this.lerp == 0f)
            {
                this.middleRed.SetAnimationFrame(0);
                this.middleGreen.SetAnimationFrame(0);
            }
            if (this.target == 1)
            {
                this.speed = Calc.Approach(this.speed, this.maxForwardSpeed, this.maxForwardSpeed / 0.2f * Engine.DeltaTime);
            }
            else
            {
                this.speed = Calc.Approach(this.speed, this.maxBackwardSpeed, this.maxBackwardSpeed / 1.5f * Engine.DeltaTime);
            }
            float num = this.lerp;
            this.lerp = Calc.Approach(this.lerp, (float)this.target, this.speed * Engine.DeltaTime);
            if (this.lerp != num)
            {
                Vector2 vector = (this.end - this.start) * this.speed;
                Vector2 position = this.Position;
                if (this.target == 1)
                {
                    vector = (this.end - this.start) * this.maxForwardSpeed;
                }
                if (this.lerp < num)
                {
                    vector *= -1f;
                }
                if (this.target == 1 && base.Scene.OnInterval(0.02f))
                {
                    this.MoveParticles(this.end - this.start);
                }
                base.MoveTo(Vector2.Lerp(this.start, this.end, this.lerp), vector);
                if (position != this.Position)
                {
                    Audio.Position(this.moveSfx, base.Center);
                    Audio.Position(this.returnSfx, base.Center);
                    if (this.Position == this.start && this.target == 0)
                    {
                        Audio.SetParameter(this.returnSfx, "end", 1f);
                        Audio.Play("event:/game/05_mirror_temple/swapblock_return_end", base.Center);
                    }
                    else if (this.Position == this.end && this.target == 1)
                    {
                        Audio.SetParameter(this.moveSfx, "end", 1f);
                        Audio.Play("event:/game/05_mirror_temple/swapblock_move_end", base.Center);
                    }
                }
            }
            if (this.Swapping && this.lerp >= 1f)
            {
                this.Swapping = false;
            }
            //Audio.Stop(this.returnSfx, true);
            //Audio.Stop(this.moveSfx, true);
            this.StopPlayerRunIntoAnimation = (this.lerp <= 0f || this.lerp >= 1f);
        }

        private void MoveParticles(Vector2 normal)
        {
            Vector2 position;
            Vector2 vector;
            float direction;
            float num;
            if (normal.X > 0f)
            {
                position = base.CenterLeft;
                vector = Vector2.UnitY * (base.Height - 6f);
                direction = 3.14159274f;
                num = Math.Max(2f, base.Height / 14f);
            }
            else if (normal.X < 0f)
            {
                position = base.CenterRight;
                vector = Vector2.UnitY * (base.Height - 6f);
                direction = 0f;
                num = Math.Max(2f, base.Height / 14f);
            }
            else if (normal.Y > 0f)
            {
                position = base.TopCenter;
                vector = Vector2.UnitX * (base.Width - 6f);
                direction = -1.57079637f;
                num = Math.Max(2f, base.Width / 14f);
            }
            else
            {
                position = base.BottomCenter;
                vector = Vector2.UnitX * (base.Width - 6f);
                direction = 1.57079637f;
                num = Math.Max(2f, base.Width / 14f);
            }
            this.particlesRemainder += num;
            int num2 = (int)this.particlesRemainder;
            this.particlesRemainder -= (float)num2;
            vector *= 0.5f;
            base.SceneAs<Level>().Particles.Emit(SwapBlock.P_Move, num2, position, vector, direction);
        }

        public override void Render()
        {
            Vector2 vector = this.Position + base.Shake;
            if (this.lerp != (float)this.target && this.speed > 0f)
            {
                Vector2 value = (this.end - this.start).SafeNormalize();
                if (this.target == 1)
                {
                    value *= -1f;
                }
                float num = this.speed / this.maxForwardSpeed;
                float num2 = 16f * num;
                int num3 = 2;
                while ((float)num3 < num2)
                {
                    this.DrawBlockStyle(vector + value * (float)num3, base.Width, base.Height, this.nineSliceGreen, this.middleGreen, Color.White * (1f - (float)num3 / num2));
                    num3 += 2;
                }
            }
            if (this.redAlpha < 1f)
            {
                this.DrawBlockStyle(vector, base.Width, base.Height, this.nineSliceGreen, this.middleGreen, Color.White);
            }
            if (this.redAlpha > 0f)
            {
                this.DrawBlockStyle(vector, base.Width, base.Height, this.nineSliceRed, this.middleRed, Color.White * this.redAlpha);
            }
        }

        private void DrawBlockStyle(Vector2 pos, float width, float height, MTexture[,] ninSlice, Sprite middle, Color color)
        {
            int num = (int)(width / 8f);
            int num2 = (int)(height / 8f);
            ninSlice[0, 0].Draw(pos + new Vector2(0f, 0f), Vector2.Zero, color);
            ninSlice[2, 0].Draw(pos + new Vector2(width - 8f, 0f), Vector2.Zero, color);
            ninSlice[0, 2].Draw(pos + new Vector2(0f, height - 8f), Vector2.Zero, color);
            ninSlice[2, 2].Draw(pos + new Vector2(width - 8f, height - 8f), Vector2.Zero, color);
            for (int i = 1; i < num - 1; i++)
            {
                ninSlice[1, 0].Draw(pos + new Vector2((float)(i * 8), 0f), Vector2.Zero, color);
                ninSlice[1, 2].Draw(pos + new Vector2((float)(i * 8), height - 8f), Vector2.Zero, color);
            }
            for (int j = 1; j < num2 - 1; j++)
            {
                ninSlice[0, 1].Draw(pos + new Vector2(0f, (float)(j * 8)), Vector2.Zero, color);
                ninSlice[2, 1].Draw(pos + new Vector2(width - 8f, (float)(j * 8)), Vector2.Zero, color);
            }
            for (int k = 1; k < num - 1; k++)
            {
                for (int l = 1; l < num2 - 1; l++)
                {
                    ninSlice[1, 1].Draw(pos + new Vector2((float)k, (float)l) * 8f, Vector2.Zero, color);
                }
            }
            if (middle != null)
            {
                middle.Color = color;
                middle.RenderPosition = pos + new Vector2(width / 2f, height / 2f);
                middle.Render();
            }
        }

        public static ParticleType P_Move;

        private const float ReturnTime = 0.8f;

        public Vector2 Direction;

        public bool Swapping;

        private Vector2 start;

        private Vector2 end;

        private float lerp;

        private int target;

        private Rectangle moveRect;

        private float speed;

        private float maxForwardSpeed;

        private float maxBackwardSpeed;

        private float returnTimer;

        private float redAlpha;

        private MTexture[,] nineSliceGreen;

        private MTexture[,] nineSliceRed;

        private MTexture[,] nineSliceTarget;

        private Sprite middleGreen;

        private Sprite middleRed;

        private ToggleSwapBlock.PathRenderer path;

        private EventInstance moveSfx;

        private EventInstance returnSfx;

        private DisplacementRenderer.Burst burst;

        private float particlesRemainder;

        private class PathRenderer : Entity
        {
            public PathRenderer(ToggleSwapBlock block)
            {
                this.clipTexture = new MTexture();
                //base..ctor(block.Position);
                this.block = block;
                base.Depth = 8999;
                this.pathTexture = GFX.Game[block.directory +  "/path" + ((block.start.X == block.end.X) ? "V" : "H")];
                this.timer = Calc.Random.NextFloat();
            }

            public override void Update()
            {
                base.Update();
                this.timer += Engine.DeltaTime * 4f;
            }

            public override void Render()
            {
                for (int i = this.block.moveRect.Left; i < this.block.moveRect.Right; i += this.pathTexture.Width)
                {
                    for (int j = this.block.moveRect.Top; j < this.block.moveRect.Bottom; j += this.pathTexture.Height)
                    {
                        this.pathTexture.GetSubtexture(0, 0, Math.Min(this.pathTexture.Width, this.block.moveRect.Right - i), Math.Min(this.pathTexture.Height, this.block.moveRect.Bottom - j), this.clipTexture);
                        if (block.renderBG) this.clipTexture.DrawCentered(new Vector2((float)(i + this.clipTexture.Width / 2), (float)(j + this.clipTexture.Height / 2)), Color.White);
                    }
                }
                float scale = 0.5f * (0.5f + ((float)Math.Sin((double)this.timer) + 1f) * 0.25f);
                this.block.DrawBlockStyle(new Vector2((float)this.block.moveRect.X, (float)this.block.moveRect.Y), (float)this.block.moveRect.Width, (float)this.block.moveRect.Height, this.block.nineSliceTarget, null, Color.White * scale);
            }

            private ToggleSwapBlock block;

            private MTexture pathTexture;

            private MTexture clipTexture;

            private float timer;
        }
    }
}
