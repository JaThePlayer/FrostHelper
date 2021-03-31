using System;
using System.Collections;
using System.Collections.Generic;
using Celeste;
using Celeste.Mod;
using Microsoft.Xna.Framework;
using Monocle;


namespace FrostHelper {
    [Tracked(false)]
    public class CustomZipMover : Solid
    {
    public enum LineColor
    {
        Red,
        Blue,
        Black,
        Normal,
        Core,
        Custom
    }
    string innercogstr;
    string cogstr;
    string blockstr;
    string lightstr;
    string directory;
    bool isCore = false;
    bool iceModeNext = false;
    bool iceMode = false;
    string hexcolor = "0";
    string hexlightcolor = "0";
    string coldhexcolor = "0";
    string coldhexlightcolor = "0";
    string hothexcolor = "0";
    string hothexlightcolor = "0";
        bool drawLine;
    Color tint = Color.White;

    private void OnChangeMode(Session.CoreModes coreMode)
    {
        iceModeNext = (coreMode == Session.CoreModes.Cold);
    }

    private void CheckModeChange()
    {
        if (iceModeNext != iceMode)
        {
            iceMode = iceModeNext;
            ToggleSprite();
        }
    }

    private void ToggleSprite()
    {
        if (iceMode)
        {
            //if (this.color == CustomZipMover.LineColor.Core)
            //{
               // this.hexcolor = "006bb3"; // 0 107 179
                //this.hexlightcolor = "0099ff";
               // innercogstr = "objects/FrostHelper/customZipMover/redcog/cold/innercog";
               // cogstr = "objects/FrostHelper/customZipMover/redcog/cold/cog";
               // blockstr = "objects/FrostHelper/customZipMover/redcog/cold/block";
            //}
            //else
            //{
                hexcolor = coldhexcolor;
                hexlightcolor = coldhexlightcolor;
                innercogstr = directory + "/cold/innercog";
                cogstr = directory + "/cold/cog";
                blockstr = directory + "/cold/block";
            //}
            percentage = percentage / 4;
        }
        else
        {
            //if (this.color == CustomZipMover.LineColor.Core)
            //{
            //    hexcolor = "e62e00"; // 230 46 0
            //    hexlightcolor = "ff5c33";
            //    innercogstr = "objects/FrostHelper/customZipMover/redcog/innercog";
            //    cogstr = "objects/FrostHelper/customZipMover/redcog/cog";
            //    blockstr = "objects/FrostHelper/customZipMover/redcog/block";
            //}
            //else
            //{
                hexcolor = hothexcolor;
                hexlightcolor = hothexlightcolor;
                innercogstr = directory + "/innercog";
                cogstr = directory + "/cog";
                blockstr = directory + "/block";
            //}
            percentage = percentage * 4;
        }
        ropeColor = Calc.HexToColor(hexcolor);
        ropeLightColor = Calc.HexToColor(hexlightcolor);
        innerCogs = GFX.Game.GetAtlasSubtextures(innercogstr);
    }

    public CustomZipMover(Vector2 position, int width, int height, Vector2 target, float percentage, CustomZipMover.LineColor color, String linecolor, String linelightcolor, String directory, bool isCore, String coldlinecolor, String coldlinelightcolor, String tint, bool drawLine) : base(position, width, height, false)
    {
        if (tint != "")
        {
            this.tint = Calc.HexToColor(tint);
        }
        this.drawLine = drawLine;
        this.color = color;
        innercogstr = "objects/FrostHelper/customZipMover/";
        lightstr = innercogstr;
        cogstr = innercogstr;
        blockstr = innercogstr;
        switch (color) // legacy support
        {
            case LineColor.Red:
                hexcolor = "e62e00"; // 230 46 0
                hexlightcolor = "ff5c33";
                innercogstr += "redcog/innercog";
                cogstr += "redcog/cog";
                blockstr += "redcog/block";
                lightstr += "redcog/light";
                break;
            case LineColor.Blue:
                hexcolor = "006bb3"; // 0 107 179
                hexlightcolor = "0099ff";
                innercogstr += "redcog/cold/innercog";
                cogstr += "redcog/cold/cog";
                blockstr += "redcog/cold/block";
                lightstr += "redcog/cold/light";
                break;
            case LineColor.Black:
                hexcolor = "000000";
                hexlightcolor = "1a1a1a";
                innercogstr += "blackcog/innercog";
                cogstr += "blackcog/cog";
                blockstr += "blackcog/block";
                lightstr += "blackcog/light";
                break;
            case LineColor.Normal:
                hexcolor = "663931";
                hexlightcolor = "9b6157";
                innercogstr = "objects/zipmover/innercog";
                cogstr = "objects/zipmover/cog";
                blockstr = "objects/zipmover/block";
                lightstr = "objects/zipmover/light";
                break;
            case LineColor.Core:
                hexcolor = "e62e00"; // 230 46 0
                hexlightcolor = "ff5c33";
                coldhexcolor = "006bb3";
                coldhexlightcolor = "0099ff";
                hothexcolor = hexcolor;
                hothexlightcolor = hexlightcolor;
                innercogstr += "redcog/innercog";
                cogstr += "redcog/cog";
                blockstr += "redcog/block";
                lightstr += "redcog/light";
                this.isCore = true;
                this.directory = "objects/FrostHelper/customZipMover/redcog";
                break;
            case LineColor.Custom:
                hexcolor = linecolor;
                hexlightcolor = linelightcolor;
                coldhexcolor = coldlinecolor;
                coldhexlightcolor = coldlinelightcolor;
                hothexcolor = linecolor;
                hothexlightcolor = linelightcolor;
                this.directory = directory;
                // legacy support - bluecog was moved to redcog/cold to make core mode work correctly with them without hardcoding. We need this to make maps using the old directory still work. Ahorn doesn't have this however, so that new users won't accidentaly use this.
                if (this.directory == "objects/FrostHelper/customZipMover/bluecog") this.directory = "objects/FrostHelper/customZipMover/redcog/cold";
                innercogstr = this.directory + "/innercog";
                cogstr = this.directory + "/cog";
                blockstr = this.directory + "/block";
                lightstr = this.directory + "/light";
                this.isCore = isCore;
                break;
            default:
                throw new ArgumentOutOfRangeException("color", color, null);
        }
        if (this.isCore)
        {
                Add(new CoreModeListener(new Action<Session.CoreModes>(OnChangeMode)));
        }
        ropeColor = Calc.HexToColor(hexcolor);
        ropeLightColor = Calc.HexToColor(hexlightcolor);
        edges = new MTexture[3, 3];
        innerCogs = GFX.Game.GetAtlasSubtextures(innercogstr);
        temp = new MTexture();
        sfx = new SoundSource();
            //base..ctor(position, (float)width, (float)height, false);
            Depth = -9999;
        start = Position;
        this.target = target;
            Add(new Coroutine(Sequence(), true));
            Add(new LightOcclude(1f));
            //base.Add(this.streetlight = new Sprite(GFX.Game, "objects/zipmover/light"));
            Add(streetlight = new Sprite(GFX.Game, lightstr));
        streetlight.Add("frames", "", 1f);
        streetlight.Play("frames", false, false);
        streetlight.Active = false;
        streetlight.SetAnimationFrame(1);
        streetlight.Position = new Vector2(Width / 2f - streetlight.Width / 2f, 0f);
            Add(bloom = new BloomPoint(1f, 6f));
        bloom.Position = new Vector2(Width / 2f, 4f);
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                edges[i, j] = GFX.Game[blockstr].GetSubtexture(i * 8, j * 8, 8, 8, null);
            }
        }
        SurfaceSoundIndex = 7;
        sfx.Position = new Vector2(Width, Height) / 2f;
            Add(sfx);
    }


    // Token: 0x0600281C RID: 10268 RVA: 0x000DF2F9 File Offset: 0x000DD4F9
    public CustomZipMover(EntityData data, Vector2 offset, float percentage, LineColor color) : this(data.Position + offset, data.Width, data.Height, data.Nodes[0] + offset, data.Float("percentage", 100f), data.Enum<CustomZipMover.LineColor>("color", LineColor.Custom), data.Attr("lineColor", "663931"), data.Attr("lineLightColor", "ff5c33"), data.Attr("directory", "objects/zipmover"), data.Bool("isCore", false), data.Attr("coldLineColor", "663931"), data.Attr("coldLineLightColor", "663931"), data.Attr("tint", ""), data.Bool("showLine", true))
    //public CustomZipMover(EntityData data, Vector2 offset, float percentage, LineColor color) : this(data.Position + offset, data.Width, data.Height, data.Nodes[0] + offset, data.Float("percentage", 100f), data.Enum<CustomZipMover.LineColor>("preset", CustomZipMover.LineColor.Normal), data.Attr("lineColor", "663931"), data.Attr("lineLightColor", "ff5c33"), data.Attr("directory", "objects/zipmover"), data.Bool("isCore", false), data.Attr("coldLineColor", "663931"), data.Attr("coldLineLightColor", "663931"))
    {
        this.percentage = data.Float("percentage", 100f);
            FillMiddle = data.Bool("fillMiddle", true);
    }

    // Token: 0x0600281D RID: 10269 RVA: 0x000DF32C File Offset: 0x000DD52C
    public override void Added(Scene scene)
    {
        base.Added(scene);
        if (isCore)
        {
            iceModeNext = (iceMode = (SceneAs<Level>().CoreMode == Session.CoreModes.Cold));
            ToggleSprite();
        }
        scene.Add(pathRenderer = new CustomZipMover.ZipMoverPathRenderer(this));
    }

    public override void Removed(Scene scene)
    {
        scene.Remove(pathRenderer);
        pathRenderer = null;
        base.Removed(scene);
    }

    public override void Update()
    {
        base.Update();
        CheckModeChange();
        bloom.Y = (float)(streetlight.CurrentAnimationFrame * 3);
    }

    bool FillMiddle;
    public override void Render()
    {
        Vector2 position = Position;
        Position += Shake;
        if (FillMiddle)
            Draw.Rect(X, Y, Width, Height, Color.Black);
        int num = 1;
        float num2 = 0f;
        int count = innerCogs.Count;
        int num3 = 4;
        while ((float)num3 <= Height - 4f)
        {
            int num4 = num;
            int num5 = 4;
            while ((float)num5 <= Width - 4f)
            {
                int index = (int)(mod((num2 + (float)num * percent * 3.14159274f * 4f) / 1.57079637f, 1f) * (float)count);
                MTexture mtexture = innerCogs[index];
                Rectangle rectangle = new Rectangle(0, 0, mtexture.Width, mtexture.Height);
                Vector2 zero = Vector2.Zero;
                if (num5 <= 4)
                {
                    zero.X = 2f;
                    rectangle.X = 2;
                    rectangle.Width -= 2;
                }
                else if ((float)num5 >= Width - 4f)
                {
                    zero.X = -2f;
                    rectangle.Width -= 2;
                }
                if (num3 <= 4)
                {
                    zero.Y = 2f;
                    rectangle.Y = 2;
                    rectangle.Height -= 2;
                }
                else if ((float)num3 >= Height - 4f)
                {
                    zero.Y = -2f;
                    rectangle.Height -= 2;
                }
                mtexture = mtexture.GetSubtexture(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, temp);
                //mtexture.DrawCentered(this.Position + new Vector2((float)num5, (float)num3) + zero, Color.White * ((num < 0) ? 0.5f : 1f));
                mtexture.DrawCentered(Position + new Vector2((float)num5, (float)num3) + zero, tint * ((num < 0) ? 0.5f : 1f));
                num = -num;
                num2 += 1.04719758f;
                num5 += 8;
            }
            if (num4 == num)
            {
                num = -num;
            }
            num3 += 8;
        }
        int num6 = 0;
        while ((float)num6 < Width / 8f)
        {
            int num7 = 0;
            while ((float)num7 < Height / 8f)
            {
                int num8 = (num6 == 0) ? 0 : (((float)num6 == Width / 8f - 1f) ? 2 : 1);
                int num9 = (num7 == 0) ? 0 : (((float)num7 == Height / 8f - 1f) ? 2 : 1);
                if (num8 != 1 || num9 != 1)
                {
                    edges[num8, num9].Draw(new Vector2(X + (float)(num6 * 8), Y + (float)(num7 * 8)), new Vector2(0, 0), tint);
                }
                num7++;
            }
            num6++;
        }
        base.Render();
        Position = position;
    }

    // Token: 0x06002821 RID: 10273 RVA: 0x000DF688 File Offset: 0x000DD888
    private void ScrapeParticlesCheck(Vector2 to)
    {
        if (Scene.OnInterval(0.03f))
        {
            bool flag = to.Y != ExactPosition.Y;
            bool flag2 = to.X != ExactPosition.X;
            if (flag && !flag2)
            {
                int num = Math.Sign(to.Y - ExactPosition.Y);
                Vector2 value;
                if (num == 1)
                {
                    value = BottomLeft;
                }
                else
                {
                    value = TopLeft;
                }
                int num2 = 4;
                if (num == 1)
                {
                    num2 = Math.Min((int)Height - 12, 20);
                }
                int num3 = (int)Height;
                if (num == -1)
                {
                    num3 = Math.Max(16, (int)Height - 16);
                }
                if (Scene.CollideCheck<Solid>(value + new Vector2(-2f, (float)(num * -2))))
                {
                    for (int i = num2; i < num3; i += 8)
                    {
                        //base.SceneAs<Level>().ParticlesFG.Emit(CustomZipMover.P_Scrape, base.TopLeft + new Vector2(0f, (float)i + (float)num * 2f), (num == 1) ? -0.7853982f : 0.7853982f);
                    }
                }
                if (Scene.CollideCheck<Solid>(value + new Vector2(Width + 2f, (float)(num * -2))))
                {
                    for (int j = num2; j < num3; j += 8)
                    {
                        //base.SceneAs<Level>().ParticlesFG.Emit(CustomZipMover.P_Scrape, base.TopRight + new Vector2(-1f, (float)j + (float)num * 2f), (num == 1) ? -2.3561945f : 2.3561945f);
                    }
                    return;
                }
            }
            else if (flag2 && !flag)
            {
                int num4 = Math.Sign(to.X - ExactPosition.X);
                Vector2 value2;
                if (num4 == 1)
                {
                    value2 = TopRight;
                }
                else
                {
                    value2 = TopLeft;
                }
                int num5 = 4;
                if (num4 == 1)
                {
                    num5 = Math.Min((int)Width - 12, 20);
                }
                int num6 = (int)Width;
                if (num4 == -1)
                {
                    num6 = Math.Max(16, (int)Width - 16);
                }
                if (Scene.CollideCheck<Solid>(value2 + new Vector2((float)(num4 * -2), -2f)))
                {
                    for (int k = num5; k < num6; k += 8)
                    {
                        //base.SceneAs<Level>().ParticlesFG.Emit(CustomZipMover.P_Scrape, base.TopLeft + new Vector2((float)k + (float)num4 * 2f, -1f), (num4 == 1) ? 2.3561945f : 0.7853982f);
                    }
                }
                if (Scene.CollideCheck<Solid>(value2 + new Vector2((float)(num4 * -2), Height + 2f)))
                {
                    for (int l = num5; l < num6; l += 8)
                    {
                        //base.SceneAs<Level>().ParticlesFG.Emit(CustomZipMover.P_Scrape, base.BottomLeft + new Vector2((float)l + (float)num4 * 2f, 0f), (num4 == 1) ? -2.3561945f : -0.7853982f);
                    }
                }
            }
        }
    }

    // Token: 0x06002822 RID: 10274 RVA: 0x000DF9C4 File Offset: 0x000DDBC4
    private IEnumerator Sequence()
    {
        Vector2 start = Position;
        for (; ; )
        {
            if (HasPlayerRider())
            {
                sfx.Play("event:/game/01_forsaken_city/zip_mover", null, 0f);
                Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
                StartShaking(0.1f);
                yield return 0.1f;
                streetlight.SetAnimationFrame(3);
                StopPlayerRunIntoAnimation = false;
                float at = 0f;
                while (at < 1f)
                {
                    yield return null;
                    at = Calc.Approach(at, 1f, 2f * Engine.DeltaTime * (percentage / 100f));
                    percent = Ease.SineIn(at);
                    Vector2 vector = Vector2.Lerp(start, target, percent);
                    //this.ScrapeParticlesCheck(vector);
                    if (Scene.OnInterval(0.1f))
                    {
                        pathRenderer.CreateSparks();
                    }
                    MoveTo(vector);
                }
                StartShaking(0.2f);
                Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
                SceneAs<Level>().Shake(0.3f);
                StopPlayerRunIntoAnimation = true;
                yield return 0.5f;
                StopPlayerRunIntoAnimation = false;
                streetlight.SetAnimationFrame(2);
                at = 0f;
                while (at < 1f)
                {
                    yield return null;
                    at = Calc.Approach(at, 1f, 0.5f * Engine.DeltaTime);
                    percent = 1f - Ease.SineIn(at);
                    Vector2 position = Vector2.Lerp(target, start, Ease.SineIn(at));
                    MoveTo(position);
                }
                StopPlayerRunIntoAnimation = true;
                StartShaking(0.2f);
                streetlight.SetAnimationFrame(1);
                yield return 0.5f;
            }
            else
            {
                yield return null;
            }
        }
        yield break;
    }

    // Token: 0x06002823 RID: 10275 RVA: 0x0002956C File Offset: 0x0002776C
    private float mod(float x, float m)
    {
        return (x % m + m) % m;
    }

    // Token: 0x06002824 RID: 10276 RVA: 0x000DF9D3 File Offset: 0x000DDBD3
    // Note: this type is marked as 'beforefieldinit'.
    static CustomZipMover()
    {
        // CustomZipMover.ropeColor = Calc.HexToColor("663931");
        // CustomZipMover.ropeLightColor = Calc.HexToColor("9b6157");
    }

    private readonly CustomZipMover.LineColor color;
    public float percentage;
    // Token: 0x0400222D RID: 8749
    public static ParticleType P_Scrape;

    // Token: 0x0400222E RID: 8750
    public static ParticleType P_Sparks;

    // Token: 0x0400222F RID: 8751
    private MTexture[,] edges;

    // Token: 0x04002230 RID: 8752
    private Sprite streetlight;

    // Token: 0x04002231 RID: 8753
    private BloomPoint bloom;

    // Token: 0x04002232 RID: 8754
    private CustomZipMover.ZipMoverPathRenderer pathRenderer;

    // Token: 0x04002233 RID: 8755
    private List<MTexture> innerCogs;

    // Token: 0x04002234 RID: 8756
    private MTexture temp;

    // Token: 0x04002235 RID: 8757
    private Vector2 start;

    // Token: 0x04002236 RID: 8758
    private Vector2 target;

    // Token: 0x04002237 RID: 8759
    private float percent;

    // Token: 0x04002238 RID: 8760
    //private static readonly Color ropeColor;
    Color ropeColor;
    // Token: 0x04002239 RID: 8761
    Color ropeLightColor;
    // Token: 0x0400223A RID: 8762
    private SoundSource sfx;

    // Token: 0x0200065C RID: 1628
    private class ZipMoverPathRenderer : Entity
    {
        Color tint = Color.White;
        // Token: 0x06002825 RID: 10277 RVA: 0x000DF9F4 File Offset: 0x000DDBF4
        public ZipMoverPathRenderer(CustomZipMover CustomZipMover)
        {
            //this.cog = GFX.Game[CustomZipMover.cogstr];
            //if (CustomZipMover.iceMode & CustomZipMover.isCore)
            //{
            //    this.cog = GFX.Game["objects/FrostHelper/customZipMover/bluecog/cog"];
            //}
            // base..ctor();
            tint = CustomZipMover.tint;
            cog = GFX.Game[CustomZipMover.cogstr];
                Depth = 5000;
            this.CustomZipMover = CustomZipMover;
            from = this.CustomZipMover.start + new Vector2(this.CustomZipMover.Width / 2f, this.CustomZipMover.Height / 2f);
            to = this.CustomZipMover.target + new Vector2(this.CustomZipMover.Width / 2f, this.CustomZipMover.Height / 2f);
            sparkAdd = (from - to).SafeNormalize(5f).Perpendicular();
            float num = (from - to).Angle();
            sparkDirFromA = num + 0.3926991f;
            sparkDirFromB = num - 0.3926991f;
            sparkDirToA = num + 3.14159274f - 0.3926991f;
            sparkDirToB = num + 3.14159274f + 0.3926991f;
        }

        // Token: 0x06002826 RID: 10278 RVA: 0x000DFB28 File Offset: 0x000DDD28
        public void CreateSparks()
        {
            //base.SceneAs<Level>().ParticlesBG.Emit(CustomZipMover.P_Sparks, this.from + this.sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), this.sparkDirFromA);
            //base.SceneAs<Level>().ParticlesBG.Emit(CustomZipMover.P_Sparks, this.from - this.sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), this.sparkDirFromB);
            //base.SceneAs<Level>().ParticlesBG.Emit(CustomZipMover.P_Sparks, this.to + this.sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), this.sparkDirToA);
            //base.SceneAs<Level>().ParticlesBG.Emit(CustomZipMover.P_Sparks, this.to - this.sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), this.sparkDirToB);
        }

        // Token: 0x06002827 RID: 10279 RVA: 0x000DFC60 File Offset: 0x000DDE60
        public override void Render()
        {
            cog = GFX.Game[CustomZipMover.cogstr];
            //if (CustomZipMover.iceMode & CustomZipMover.isCore)
            //{
            //   this.cog = GFX.Game["objects/FrostHelper/customZipMover/bluecog/cog"];
            //}
            DrawCogs(Vector2.UnitY, new Color?(Color.Black));
            DrawCogs(Vector2.Zero, null);
                if (CustomZipMover.FillMiddle)
                    Draw.Rect(new Rectangle((int)(CustomZipMover.X - 1f), (int)(CustomZipMover.Y - 1f), (int)CustomZipMover.Width + 2, (int)CustomZipMover.Height + 2), Color.Black);
        }

        // Token: 0x06002828 RID: 10280 RVA: 0x000DFCE8 File Offset: 0x000DDEE8
        private void DrawCogs(Vector2 offset, Color? colorOverride = null)
        {
                if (CustomZipMover.drawLine)
                {
                    Vector2 vector = (to - from).SafeNormalize();
                    Vector2 value = vector.Perpendicular() * 3f;
                    Vector2 value2 = -vector.Perpendicular() * 4f;
                    float rotation = CustomZipMover.percent * 3.14159274f * 2f;
                    Draw.Line(from + value + offset, to + value + offset, (colorOverride != null) ? colorOverride.Value : CustomZipMover.ropeColor);
                    Draw.Line(from + value2 + offset, to + value2 + offset, (colorOverride != null) ? colorOverride.Value : CustomZipMover.ropeColor);
                    for (float num = 4f - CustomZipMover.percent * 3.14159274f * 8f % 4f; num < (to - from).Length(); num += 4f)
                    {
                        Vector2 value3 = from + value + vector.Perpendicular() + vector * num;
                        Vector2 value4 = to + value2 - vector * num;
                        Draw.Line(value3 + offset, value3 + vector * 2f + offset, (colorOverride != null) ? colorOverride.Value : CustomZipMover.ropeLightColor);
                        Draw.Line(value4 + offset, value4 - vector * 2f + offset, (colorOverride != null) ? colorOverride.Value : CustomZipMover.ropeLightColor);
                    }
                    //this.cog.DrawCentered(this.from + offset, (colorOverride != null) ? colorOverride.Value : Color.White, 1f, rotation); // White
                    //this.cog.DrawCentered(this.to + offset, (colorOverride != null) ? colorOverride.Value : Color.White, 1f, rotation);
                    cog.DrawCentered(from + offset, (colorOverride != null) ? colorOverride.Value : tint, 1f, rotation); // White
                    cog.DrawCentered(to + offset, (colorOverride != null) ? colorOverride.Value : tint, 1f, rotation);
                }
        }

        // Token: 0x0400223B RID: 8763
        public CustomZipMover CustomZipMover;

        // Token: 0x0400223C RID: 8764
        private MTexture cog;

        // Token: 0x0400223D RID: 8765
        private Vector2 from;

        // Token: 0x0400223E RID: 8766
        private Vector2 to;

        // Token: 0x0400223F RID: 8767
        private Vector2 sparkAdd;

        // Token: 0x04002240 RID: 8768
        private float sparkDirFromA;

        // Token: 0x04002241 RID: 8769
        private float sparkDirFromB;

        // Token: 0x04002242 RID: 8770
        private float sparkDirToA;

        // Token: 0x04002243 RID: 8771
        private float sparkDirToB;
    }
}
}