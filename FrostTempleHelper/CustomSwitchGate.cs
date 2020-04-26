using System;
using System.Collections;
using System.Collections.Generic;
using Celeste;
using Celeste.Mod;
using Microsoft.Xna.Framework;
using Monocle;

namespace FrostHelper
{
    public class SwitchGate : Solid
    {
        // Token: 0x06002783 RID: 10115 RVA: 0x000DA5F4 File Offset: 0x000D87F4
        public SwitchGate(Vector2 position, float width, float height, Vector2 node, bool persistent, string spriteName) : base(data.Position + position)
        {
            this.inactiveColor = Calc.HexToColor("5fcde4");
            this.activeColor = Color.White;
            this.finishColor = Calc.HexToColor("f141df");
            base..ctor(position, width, height, false);
            this.node = node;
            this.persistent = persistent;
            base.Add(this.icon = new Sprite(GFX.Game, "objects/switchgate/icon"));
            this.icon.Add("spin", "", 0.1f, "spin");
            this.icon.Play("spin", false, false);
            this.icon.Rate = 0f;
            this.icon.Color = this.inactiveColor;
            this.icon.Position = (this.iconOffset = new Vector2(width / 2f, height / 2f));
            this.icon.CenterOrigin();
            base.Add(this.wiggler = Wiggler.Create(0.5f, 4f, delegate (float f)
            {
                this.icon.Scale = Vector2.One * (1f + f);
            }, false, false));
            MTexture mtexture = GFX.Game["objects/switchgate/" + spriteName];
            this.nineSlice = new MTexture[3, 3];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    this.nineSlice[i, j] = mtexture.GetSubtexture(new Rectangle(i * 8, j * 8, 8, 8));
                }
            }
            base.Add(this.openSfx = new SoundSource());
            base.Add(new LightOcclude(0.5f));
        }

        // Token: 0x06002784 RID: 10116 RVA: 0x000DA7A4 File Offset: 0x000D89A4
        public SwitchGate(EntityData data, Vector2 offset) : this(data.Position + offset, (float)data.Width, (float)data.Height, data.Nodes[0] + offset, data.Bool("persistent", false), data.Attr("sprite", "block"))
        {
        }

        // Token: 0x06002785 RID: 10117 RVA: 0x000DA800 File Offset: 0x000D8A00
        public override void Awake(Scene scene)
        {
            base.Awake(scene);
            if (Switch.CheckLevelFlag(base.SceneAs<Level>()))
            {
                base.MoveTo(this.node);
                this.icon.Rate = 0f;
                this.icon.SetAnimationFrame(0);
                this.icon.Color = this.finishColor;
                return;
            }
            base.Add(new Coroutine(this.Sequence(this.node), true));
        }

        // Token: 0x06002786 RID: 10118 RVA: 0x000DA874 File Offset: 0x000D8A74
        public override void Render()
        {
            float num = base.Collider.Width / 8f - 1f;
            float num2 = base.Collider.Height / 8f - 1f;
            int num3 = 0;
            while ((float)num3 <= num)
            {
                int num4 = 0;
                while ((float)num4 <= num2)
                {
                    int num5 = ((float)num3 < num) ? Math.Min(num3, 1) : 2;
                    int num6 = ((float)num4 < num2) ? Math.Min(num4, 1) : 2;
                    this.nineSlice[num5, num6].Draw(this.Position + base.Shake + new Vector2((float)(num3 * 8), (float)(num4 * 8)));
                    num4++;
                }
                num3++;
            }
            this.icon.Position = this.iconOffset + base.Shake;
            this.icon.DrawOutline(1);
            base.Render();
        }

        // Token: 0x06002787 RID: 10119 RVA: 0x000DA955 File Offset: 0x000D8B55
        private IEnumerator Sequence(Vector2 node)
        {
            SwitchGate.<> c__DisplayClass16_0 CS$<> 8__locals1 = new SwitchGate.<> c__DisplayClass16_0();
            CS$<> 8__locals1.<> 4__this = this;
            CS$<> 8__locals1.node = node;
            CS$<> 8__locals1.start = this.Position;
            while (!Switch.Check(this.Scene))
            {
                yield return null;
            }
            if (this.persistent)
            {
                Switch.SetLevelFlag(this.SceneAs<Level>());
            }
            yield return 0.1f;
            this.openSfx.Play("event:/game/general/touchswitch_gate_open", null, 0f);
            this.StartShaking(0.5f);
            while (this.icon.Rate < 1f)
            {
                this.icon.Color = Color.Lerp(this.inactiveColor, this.activeColor, this.icon.Rate);
                this.icon.Rate += Engine.DeltaTime * 2f;
                yield return null;
            }
            yield return 0.1f;
            int particleAt = 0;
            Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeOut, 2f, true);
            tween.OnUpdate = delegate (Tween t)
            {
                CS$<> 8__locals1.<> 4__this.MoveTo(Vector2.Lerp(CS$<> 8__locals1.start, CS$<> 8__locals1.node, t.Eased));
                if (CS$<> 8__locals1.<> 4__this.Scene.OnInterval(0.1f))
				{
                    int particleAt = particleAt;
                    particleAt++;
                    particleAt %= 2;
                    int num6 = 0;
                    while ((float)num6 < CS$<> 8__locals1.<> 4__this.Width / 8f)
					{
                        int num7 = 0;
                        while ((float)num7 < CS$<> 8__locals1.<> 4__this.Height / 8f)
						{
                            if ((num6 + num7) % 2 == particleAt)
                            {
                                CS$<> 8__locals1.<> 4__this.SceneAs<Level>().ParticlesBG.Emit(SwitchGate.P_Behind, CS$<> 8__locals1.<> 4__this.Position + new Vector2((float)(num6 * 8), (float)(num7 * 8)) + Calc.Random.Range(Vector2.One * 2f, Vector2.One * 6f));
                            }
                            num7++;
                        }
                        num6++;
                    }
                }
            };
            this.Add(tween);
            yield return 1.8f;
            bool collidable = this.Collidable;
            this.Collidable = false;
            if (CS$<> 8__locals1.node.X <= CS$<> 8__locals1.start.X)
			{
                Vector2 value = new Vector2(0f, 2f);
                int num = 0;
                while ((float)num < this.Height / 8f)
                {
                    Vector2 vector = new Vector2(this.Left - 1f, this.Top + 4f + (float)(num * 8));
                    Vector2 point = vector + Vector2.UnitX;
                    if (this.Scene.CollideCheck<Solid>(vector) && !this.Scene.CollideCheck<Solid>(point))
                    {
                        this.SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, vector + value, 3.14159274f);
                        this.SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, vector - value, 3.14159274f);
                    }
                    num++;
                }
            }
            if (CS$<> 8__locals1.node.X >= CS$<> 8__locals1.start.X)
			{
                Vector2 value2 = new Vector2(0f, 2f);
                int num2 = 0;
                while ((float)num2 < this.Height / 8f)
                {
                    Vector2 vector2 = new Vector2(this.Right + 1f, this.Top + 4f + (float)(num2 * 8));
                    Vector2 point2 = vector2 - Vector2.UnitX * 2f;
                    if (this.Scene.CollideCheck<Solid>(vector2) && !this.Scene.CollideCheck<Solid>(point2))
                    {
                        this.SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, vector2 + value2, 0f);
                        this.SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, vector2 - value2, 0f);
                    }
                    num2++;
                }
            }
            if (CS$<> 8__locals1.node.Y <= CS$<> 8__locals1.start.Y)
			{
                Vector2 value3 = new Vector2(2f, 0f);
                int num3 = 0;
                while ((float)num3 < this.Width / 8f)
                {
                    Vector2 vector3 = new Vector2(this.Left + 4f + (float)(num3 * 8), this.Top - 1f);
                    Vector2 point3 = vector3 + Vector2.UnitY;
                    if (this.Scene.CollideCheck<Solid>(vector3) && !this.Scene.CollideCheck<Solid>(point3))
                    {
                        this.SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, vector3 + value3, -1.57079637f);
                        this.SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, vector3 - value3, -1.57079637f);
                    }
                    num3++;
                }
            }
            if (CS$<> 8__locals1.node.Y >= CS$<> 8__locals1.start.Y)
			{
                Vector2 value4 = new Vector2(2f, 0f);
                int num4 = 0;
                while ((float)num4 < this.Width / 8f)
                {
                    Vector2 vector4 = new Vector2(this.Left + 4f + (float)(num4 * 8), this.Bottom + 1f);
                    Vector2 point4 = vector4 - Vector2.UnitY * 2f;
                    if (this.Scene.CollideCheck<Solid>(vector4) && !this.Scene.CollideCheck<Solid>(point4))
                    {
                        this.SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, vector4 + value4, 1.57079637f);
                        this.SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, vector4 - value4, 1.57079637f);
                    }
                    num4++;
                }
            }
            this.Collidable = collidable;
            Audio.Play("event:/game/general/touchswitch_gate_finish", this.Position);
            this.StartShaking(0.2f);
            while (this.icon.Rate > 0f)
            {
                this.icon.Color = Color.Lerp(this.activeColor, this.finishColor, 1f - this.icon.Rate);
                this.icon.Rate -= Engine.DeltaTime * 4f;
                yield return null;
            }
            this.icon.Rate = 0f;
            this.icon.SetAnimationFrame(0);
            this.wiggler.Start();
            bool collidable2 = this.Collidable;
            this.Collidable = false;
            if (!this.Scene.CollideCheck<Solid>(this.Center))
            {
                for (int i = 0; i < 32; i++)
                {
                    float num5 = Calc.Random.NextFloat(6.28318548f);
                    this.SceneAs<Level>().ParticlesFG.Emit(TouchSwitch.P_Fire, this.Position + this.iconOffset + Calc.AngleToVector(num5, 4f), num5);
                }
            }
            this.Collidable = collidable2;
            yield break;
        }

        // Token: 0x0400218E RID: 8590
        public static ParticleType P_Behind;

        // Token: 0x0400218F RID: 8591
        public static ParticleType P_Dust;

        // Token: 0x04002190 RID: 8592
        private MTexture[,] nineSlice;

        // Token: 0x04002191 RID: 8593
        private Sprite icon;

        // Token: 0x04002192 RID: 8594
        private Vector2 iconOffset;

        // Token: 0x04002193 RID: 8595
        private Wiggler wiggler;

        // Token: 0x04002194 RID: 8596
        private Vector2 node;

        // Token: 0x04002195 RID: 8597
        private SoundSource openSfx;

        // Token: 0x04002196 RID: 8598
        private bool persistent;

        // Token: 0x04002197 RID: 8599
        private Color inactiveColor;

        // Token: 0x04002198 RID: 8600
        private Color activeColor;

        // Token: 0x04002199 RID: 8601
        private Color finishColor;
    }
}
