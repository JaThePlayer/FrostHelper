using Celeste;
using Celeste.Mod.Entities;
using FrostHelper.Components;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;


namespace FrostHelper {
    [CustomEntity("FrostHelper/CustomZipMover")]
    [Tracked(false)]
    public class CustomZipMover : Solid {
        public enum LineColor {
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

        private void OnChangeMode(Session.CoreModes coreMode) {
            iceModeNext = coreMode == Session.CoreModes.Cold;
        }

        private void CheckModeChange() {
            if (iceModeNext != iceMode) {
                iceMode = iceModeNext;
                ToggleSprite();
            }
        }

        private void ToggleSprite() {
            if (iceMode) {
                hexcolor = coldhexcolor;
                hexlightcolor = coldhexlightcolor;
                innercogstr = directory + "/cold/innercog";
                cogstr = directory + "/cold/cog";
                blockstr = directory + "/cold/block";
                percentage /= 4;
            } else {
                hexcolor = hothexcolor;
                hexlightcolor = hothexlightcolor;
                innercogstr = directory + "/innercog";
                cogstr = directory + "/cog";
                blockstr = directory + "/block";
                percentage *= 4;
            }
            ropeColor = ColorHelper.GetColor(hexcolor);
            ropeLightColor = ColorHelper.GetColor(hexlightcolor);
            innerCogs = GFX.Game.GetAtlasSubtextures(innercogstr);

            CreateSprites();
        }

        public bool Rainbow;

        public CustomZipMover(EntityData data, Vector2 offset) : base(data.Position + offset, data.Width, data.Height, false) {
            tint = ColorHelper.GetColor(data.Attr("tint", "ffffff"));
            Rainbow = data.Bool("rainbow", false);
            drawLine = data.Bool("showLine", true);
            innercogstr = "objects/FrostHelper/customZipMover/";
            lightstr = innercogstr;
            cogstr = innercogstr;
            blockstr = innercogstr;
            switch (data.Enum("color", LineColor.Custom)) // legacy support
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
                    isCore = true;
                    directory = "objects/FrostHelper/customZipMover/redcog";
                    break;
                case LineColor.Custom:
                    hexcolor = data.Attr("lineColor", "663931");
                    hexlightcolor = data.Attr("lineLightColor", "ff5c33");
                    coldhexcolor = data.Attr("coldLineColor", "663931");
                    coldhexlightcolor = data.Attr("coldLineLightColor", "663931");
                    hothexcolor = data.Attr("lineColor", "663931");
                    hothexlightcolor = data.Attr("lineLightColor", "ff5c33");
                    directory = data.Attr("directory", "objects/zipmover");
                    // legacy support - bluecog was moved to redcog/cold to make core mode work correctly with them without hardcoding. We need this to make maps using the old directory still work. Ahorn doesn't have this however, so that new users won't accidentaly use this.
                    if (directory == "objects/FrostHelper/customZipMover/bluecog")
                        directory = "objects/FrostHelper/customZipMover/redcog/cold";
                    innercogstr = directory + "/innercog";
                    cogstr = directory + "/cog";
                    blockstr = directory + "/block";
                    lightstr = directory + "/light";
                    isCore = data.Bool("isCore", false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("color", data.Enum("color", LineColor.Normal), null);
            }
            if (isCore) {
                Add(new CoreModeListener(new Action<Session.CoreModes>(OnChangeMode)));
            }
            ropeColor = ColorHelper.GetColor(hexcolor);
            ropeLightColor = ColorHelper.GetColor(hexlightcolor);
            edges = new MTexture[3, 3];
            innerCogs = GFX.Game.GetAtlasSubtextures(innercogstr);
            temp = new MTexture();
            sfx = new SoundSource();
            Depth = -9999;
            start = Position;
            target = data.Nodes[0] + offset;
            Add(new Coroutine(Sequence(), true));
            Add(new LightOcclude(1f));

            for (int i = 0; i < 3; i++) {
                for (int j = 0; j < 3; j++) {
                    edges[i, j] = GFX.Game[blockstr].GetSubtexture(i * 8, j * 8, 8, 8, null);
                }
            }

            CreateSprites();

            float bloomAlpha = data.Float("bloomAlpha", 1f);
            if (bloomAlpha != 0.0f) {
                Add(bloom = new BloomPoint(bloomAlpha, data.Float("bloomRadius", 6f)));
                bloom.Position = new Vector2(Width / 2f, 4f);
            }

            SurfaceSoundIndex = 7;
            sfx.Position = new Vector2(Width, Height) / 2f;
            Add(sfx);

            percentage = data.Float("percentage", 100f);
            FillMiddle = data.Bool("fillMiddle", true);
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            if (isCore) {
                iceModeNext = iceMode = SceneAs<Level>().CoreMode == Session.CoreModes.Cold;
                ToggleSprite();
            }
            scene.Add(pathRenderer = new ZipMoverPathRenderer(this));
        }

        public override void Removed(Scene scene) {
            scene.Remove(pathRenderer);
            pathRenderer = null;
            base.Removed(scene);
        }

        public override void Update() {
            base.Update();
            CheckModeChange();
            if (bloom != null)
                bloom.Y = streetlight.CurrentAnimationFrame * 3;
        }


        public MultiImage BorderImages;
        public void CreateSprites() {
            foreach (var item in Components) {
                if (item is Image || item is MultiImage || item is Sprite) {
                    Remove(item);
                }
            }

            #region Border
            {
                float widthBy8 = Width / 8f;
                float heightBy8 = Height / 8f;
                int edgeCount = (int) ((widthBy8 * 2) + (heightBy8 * 2) - 4);
                Image[] edgeImages = new Image[edgeCount];
                int imageIndex = 0;

                int i = 0;
                while (i < widthBy8) {
                    int j = 0;
                    while (j < heightBy8) {
                        int num8 = (i == 0) ? 0 : ((i == Width / 8f - 1f) ? 2 : 1);
                        int num9 = (j == 0) ? 0 : ((j == Height / 8f - 1f) ? 2 : 1);
                        if (num8 != 1 || num9 != 1) {
                            //edges[num8, num9].Draw(new Vector2(X + (i * 8), Y + (j * 8)), new Vector2(0, 0), tint);
                            edgeImages[imageIndex++] = new Image(edges[num8, num9]) {
                                Color = tint,
                                Origin = Vector2.Zero,
                                Position = new Vector2(i * 8, j * 8),
                            };
                        }
                        j++;
                    }
                    i++;
                }

                BorderImages = new MultiImage(edgeImages);
                Add(BorderImages);
            }
            #endregion

            Add(streetlight = new Sprite(GFX.Game, lightstr));
            streetlight.Add("frames", "", 1f);
            streetlight.Play("frames", false, false);
            streetlight.Active = false;
            streetlight.SetAnimationFrame(1);
            streetlight.Position = new Vector2(Width / 2f - streetlight.Width / 2f, 0f);
        }


        bool FillMiddle;
        public override void Render() {
            Vector2 position = Position;
            Position += Shake;
            if (FillMiddle)
                Draw.Rect(X, Y, Width, Height, Color.Black);

            #region Cogs
            int num = 1;
            float num2 = 0f;
            int count = innerCogs.Count;
            int num3 = 4;
            while (num3 <= Height - 4f) {
                int num4 = num;
                int num5 = 4;
                while (num5 <= Width - 4f) {
                    int index = (int) (mod((num2 + num * percent * 3.14159274f * 4f) / 1.57079637f, 1f) * count);
                    MTexture mtexture = innerCogs[index];
                    Rectangle rectangle = new Rectangle(0, 0, mtexture.Width, mtexture.Height);
                    Vector2 zero = Vector2.Zero;
                    if (num5 <= 4) {
                        zero.X = 2f;
                        rectangle.X = 2;
                        rectangle.Width -= 2;
                    } else if (num5 >= Width - 4f) {
                        zero.X = -2f;
                        rectangle.Width -= 2;
                    }
                    if (num3 <= 4) {
                        zero.Y = 2f;
                        rectangle.Y = 2;
                        rectangle.Height -= 2;
                    } else if (num3 >= Height - 4f) {
                        zero.Y = -2f;
                        rectangle.Height -= 2;
                    }
                    mtexture = mtexture.GetSubtexture(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, temp);
                    Vector2 renderPos = Position + new Vector2(num5, num3) + zero;
                    Color color = Rainbow ? ColorHelper.GetHue(Scene, renderPos) : tint;
                    mtexture.DrawCentered(renderPos, color * ((num < 0) ? 0.5f : 1f));
                    num = -num;
                    num2 += 1.04719758f;
                    num5 += 8;
                }
                if (num4 == num) {
                    num = -num;
                }
                num3 += 8;
            }
            #endregion

            if (Rainbow) {
                for (int i = 0; i < BorderImages.Images.Length; i++) {
                    BorderImages.Images[i].Color = ColorHelper.GetHue(Scene, BorderImages.Images[i].RenderPosition + Position);
                }
                streetlight.Color = ColorHelper.GetHue(Scene, streetlight.RenderPosition);

            }

            base.Render();
            Position = position;
        }

        private IEnumerator Sequence() {
            Vector2 start = Position;
            while (true) {
                if (HasPlayerRider()) {
                    sfx.Play("event:/game/01_forsaken_city/zip_mover", null, 0f);
                    Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
                    StartShaking(0.1f);
                    yield return 0.1f;
                    streetlight.SetAnimationFrame(3);
                    StopPlayerRunIntoAnimation = false;
                    float at = 0f;
                    while (at < 1f) {
                        yield return null;
                        at = Calc.Approach(at, 1f, 2f * Engine.DeltaTime * (percentage / 100f));
                        percent = Ease.SineIn(at);
                        Vector2 vector = Vector2.Lerp(start, target, percent);
                        //this.ScrapeParticlesCheck(vector);
                        if (Scene.OnInterval(0.1f)) {
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
                    while (at < 1f) {
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
                } else {
                    yield return null;
                }
            }
        }

        private float mod(float x, float m) {
            return (x % m + m) % m;
        }

        public float percentage;
        public static ParticleType P_Scrape;
        public static ParticleType P_Sparks;
        private MTexture[,] edges;
        private Sprite streetlight;
        private BloomPoint bloom;
        private ZipMoverPathRenderer pathRenderer;
        private List<MTexture> innerCogs;
        private MTexture temp;
        private Vector2 start;
        private Vector2 target;
        private float percent;
        Color ropeColor;
        Color ropeLightColor;
        private SoundSource sfx;

        private class ZipMoverPathRenderer : Entity {
            Color tint = Color.White;
            public ZipMoverPathRenderer(CustomZipMover CustomZipMover) {
                //this.cog = GFX.Game[CustomZipMover.cogstr];
                //if (CustomZipMover.iceMode & CustomZipMover.isCore)
                //{
                //    this.cog = GFX.Game["objects/FrostHelper/customZipMover/bluecog/cog"];
                //}
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

            public void CreateSparks() {
                SceneAs<Level>().ParticlesBG.Emit(ZipMover.P_Sparks, from + sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirFromA);
                SceneAs<Level>().ParticlesBG.Emit(ZipMover.P_Sparks, from - sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirFromB);
                SceneAs<Level>().ParticlesBG.Emit(ZipMover.P_Sparks, to + sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirToA);
                SceneAs<Level>().ParticlesBG.Emit(ZipMover.P_Sparks, to - sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirToB);
            }

            public override void Render() {
                cog = GFX.Game[CustomZipMover.cogstr];
                //if (CustomZipMover.iceMode & CustomZipMover.isCore)
                //{
                //   this.cog = GFX.Game["objects/FrostHelper/customZipMover/bluecog/cog"];
                //}
                DrawCogs(Vector2.UnitY, new Color?(Color.Black));
                DrawCogs(Vector2.Zero, null);
                if (CustomZipMover.FillMiddle)
                    Draw.Rect(new Rectangle((int) (CustomZipMover.X - 1f), (int) (CustomZipMover.Y - 1f), (int) CustomZipMover.Width + 2, (int) CustomZipMover.Height + 2), Color.Black);
            }

            private void DrawCogs(Vector2 offset, Color? colorOverride = null) {
                if (CustomZipMover.drawLine) {
                    Vector2 vector = (to - from).SafeNormalize();
                    Vector2 value = vector.Perpendicular() * 3f;
                    Vector2 value2 = -vector.Perpendicular() * 4f;
                    float rotation = CustomZipMover.percent * 3.14159274f * 2f;
                    Draw.Line(from + value + offset, to + value + offset, (colorOverride != null) ? colorOverride.Value : CustomZipMover.ropeColor);
                    Draw.Line(from + value2 + offset, to + value2 + offset, (colorOverride != null) ? colorOverride.Value : CustomZipMover.ropeColor);
                    for (float num = 4f - CustomZipMover.percent * 3.14159274f * 8f % 4f; num < (to - from).Length(); num += 4f) {
                        Vector2 value3 = from + value + vector.Perpendicular() + vector * num;
                        Vector2 value4 = to + value2 - vector * num;
                        Draw.Line(value3 + offset, value3 + vector * 2f + offset, (colorOverride != null) ? colorOverride.Value : CustomZipMover.ropeLightColor);
                        Draw.Line(value4 + offset, value4 - vector * 2f + offset, (colorOverride != null) ? colorOverride.Value : CustomZipMover.ropeLightColor);
                    }
                    Color cogColor = CustomZipMover.Rainbow ? ColorHelper.GetHue(Scene, from + offset) : tint;
                    cog.DrawCentered(from + offset, (colorOverride != null) ? colorOverride.Value : cogColor, 1f, rotation);
                    cogColor = CustomZipMover.Rainbow ? ColorHelper.GetHue(Scene, from + offset) : tint;
                    cog.DrawCentered(to + offset, (colorOverride != null) ? colorOverride.Value : cogColor, 1f, rotation);
                }
            }

            public CustomZipMover CustomZipMover;
            private MTexture cog;
            private Vector2 from;
            private Vector2 to;
            private Vector2 sparkAdd;
            private float sparkDirFromA;
            private float sparkDirFromB;
            private float sparkDirToA;
            private float sparkDirToB;
        }
    }
}