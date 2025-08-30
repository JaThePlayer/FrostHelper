using FrostHelper.Helpers;

namespace FrostHelper.Entities;

public class LavaShape : Component {
    public int SurfaceStep { get; private set; }

    public Vector2[] EdgeVertices { get; private set; }

    public Vector3[] Fill { get; private set; }

    public float Width { get; private set; }

    public float Height { get; private set; }

    public float MinX, MaxX, MinY, MaxY;

    public LavaShape(Vector2[] edgeVertices, Vector3[] fill, int step) : base(true, true) {
        EdgeVertices = edgeVertices;
        Fill = fill;
        Fade = 16f;
        SmallWaveAmplitude = 1f;
        BigWaveAmplitude = 4f;
        CurveAmplitude = 12f;
        UpdateMultiplier = 1f;
        SurfaceColor = Color.White;
        EdgeColor = Color.LightGray;
        CenterColor = Color.DarkGray;
        timer = Calc.Random.NextFloat(100f);
        Resize(step);
    }

    public float GetFullLength() {
        float total = 0f;
        for (int i = 0; i < EdgeVertices.Length; i++) {
            total += EdgeVertices[i].Length();
        }
        return total;
    }

    private float GetSurfaceY(Vector2 currentPos) {
        float surfaceY = MinY + currentPos.Y;//MaxY - 5f;

        while (surfaceY > MinY) {
            Vector2 point = new Vector2(currentPos.X + MinX, surfaceY);
            for (int i = 0; i < EdgeVertices.Length - 1; i++) {
                if (Collide.RectToLine(point.X, point.Y, 1f, 1f, EdgeVertices[i], EdgeVertices[i + 1])) {
                    return surfaceY - MinY + 1f;
                }

            }
            if (Collide.RectToLine(point.X, point.Y, 1f, 1f, EdgeVertices[0], EdgeVertices[EdgeVertices.Length - 1])) {
                return surfaceY - MinY + 1f;
            }
            surfaceY--;
        }

        return surfaceY - MinY;
    }

    private float GetBottomY(Vector2 currentPos) {
        float surfaceY = currentPos.Y + MinY;

        while (surfaceY < MaxY) {
            Vector2 point = new Vector2(currentPos.X + MinX, surfaceY);
            for (int i = 0; i < EdgeVertices.Length - 1; i++) {
                //if (Collide.CircleToLine(point, 4f, EdgeVertices[i], EdgeVertices[i + 1]))
                if (Collide.RectToLine(point.X, point.Y, 1f, 1f, EdgeVertices[i], EdgeVertices[i + 1])) {
                    return surfaceY - MinY + 1f;
                }

            }
            if (Collide.RectToLine(point.X, point.Y, 1f, 1f, EdgeVertices[0], EdgeVertices[EdgeVertices.Length - 1]))
            //if (Collide.CircleToLine(point, 4f, EdgeVertices[0], EdgeVertices[EdgeVertices.Length - 1]))
            {
                return surfaceY - MinY + 1f;
            }
            surfaceY++;
        }

        return surfaceY - MinY;
    }

    public void Resize(int step) {
        MinX = EdgeVertices.Min((v) => v.X);
        MaxX = EdgeVertices.Max((v) => v.X);
        MinY = EdgeVertices.Min((v) => v.Y);
        MaxY = EdgeVertices.Max((v) => v.Y);
        Width = Math.Abs(MaxX - MinX);
        Height = Math.Abs(MaxY - MinY);

        SurfaceStep = step;
        dirty = true;
        float len = GetFullLength();
        int num = (int) (GetFullLength() / SurfaceStep * 2f + 4f);
        verts = new VertexPositionColor[num * 3 * 6 + 6 + Fill.Length];
        bubbles = new Bubble[(int) (len * /*0.005f*/ 0.01f)];
        surfaceBubbles = new SurfaceBubble[(int) Math.Max(4f, bubbles.Length * 0.25f)];
        for (int i = 0; i < bubbles.Length; i++) {
            float x = 1f + Calc.Random.NextFloat(Width - 2f);
            bubbles[i].Position = new Vector2(x, Calc.Random.NextFloat(Height));
            //bubbles[i].MaxY = GetSurfaceY(bubbles[i].Position);
            //bubbles[i].MinY = GetBottomY(new Vector2(bubbles[i].Position.X, bubbles[i].MaxY + 1f));
            bubbles[i].MinY = GetSurfaceY(new Vector2(bubbles[i].Position.X, MaxY - MinY + 4f));
            bubbles[i].MaxY = GetSurfaceY(new Vector2(bubbles[i].Position.X, bubbles[i].MinY - 4f));
            bubbles[i].Position.Y = float.Clamp(bubbles[i].Position.Y, bubbles[i].MaxY, bubbles[i].MinY);
            bubbles[i].Speed = Calc.Random.Range(4, 12);
            bubbles[i].Alpha = Calc.Random.Range(0.4f, 0.8f);
        }
        for (int j = 0; j < surfaceBubbles.Length; j++) {
            surfaceBubbles[j].X = -1f;
        }
        surfaceBubbleAnimations = new List<List<MTexture>> {
            GFX.Game.GetAtlasSubtextures("danger/lava/bubble_a")
        };
    }

    public override void Update() {
        timer += UpdateMultiplier * Engine.DeltaTime;
        if (UpdateMultiplier != 0f) {
            dirty = true;
        }
        for (int i = 0; i < bubbles.Length; i++) {
            Bubble[] array = bubbles;
            int num = i;
            array[num].Position.Y -= UpdateMultiplier * bubbles[i].Speed * Engine.DeltaTime;

            //if (bubbles[i].Position.Y < 2f - Wave((int)(bubbles[i].Position.X / (float)SurfaceStep), Width))
            if (bubbles[i].Position.Y < bubbles[i].MaxY) {
                bubbles[i].Position.Y = bubbles[i].MinY;//Height - 1f;
                if (Calc.Random.Chance(0.75f)) {
                    surfaceBubbles[surfaceBubbleIndex].X = bubbles[i].Position.X;
                    surfaceBubbles[surfaceBubbleIndex].Y = bubbles[i].MaxY - 0f;
                    surfaceBubbles[surfaceBubbleIndex].Frame = 0f;
                    surfaceBubbles[surfaceBubbleIndex].Animation = (byte) Calc.Random.Next(surfaceBubbleAnimations.Count);
                    surfaceBubbleIndex = (surfaceBubbleIndex + 1) % surfaceBubbles.Length;
                }
            }
        }
        for (int j = 0; j < surfaceBubbles.Length; j++) {
            if (surfaceBubbles[j].X >= 0f) {
                SurfaceBubble[] array2 = surfaceBubbles;
                int num2 = j;
                array2[num2].Frame = array2[num2].Frame + Engine.DeltaTime * 6f;
                if (surfaceBubbles[j].Frame >= surfaceBubbleAnimations[surfaceBubbles[j].Animation].Count) {
                    surfaceBubbles[j].X = -1f;
                }
            }
        }
        base.Update();
    }

    private float Sin(float value) {
        return (1f + (float) Math.Sin(value)) / 2f;
    }

    private float Wave(int step, float length) {
        int num = step * SurfaceStep;
        float num2 = (OnlyMode != OnlyModes.None) ? 1f : (Calc.ClampedMap(num, 0f, length * 0.1f, 0f, 1f) * Calc.ClampedMap(num, length * 0.9f, length, 1f, 0f));
        float num3 = Sin(num * 0.25f + timer * 4f) * SmallWaveAmplitude;
        num3 += Sin(num * 0.05f + timer * 0.5f) * BigWaveAmplitude;
        if (step % 2 == 0) {
            num3 += Spikey;
        }
        if (OnlyMode != OnlyModes.None) {
            num3 += (1f - Calc.YoYo(num / length)) * CurveAmplitude;
        }
        return num3 * num2;
    }

    private void Quad(ref int vert, Vector2 va, Vector2 vb, Vector2 vc, Vector2 vd, Color color) {
        Quad(ref vert, va, color, vb, color, vc, color, vd, color);
    }

    private void Quad(ref int vert, Vector2 va, Color ca, Vector2 vb, Color cb, Vector2 vc, Color cc, Vector2 vd, Color cd) {
        verts[vert].Position.X = va.X;
        verts[vert].Position.Y = va.Y;
        VertexPositionColor[] array = verts;
        int num = vert;
        vert = num + 1;
        array[num].Color = ca;
        verts[vert].Position.X = vb.X;
        verts[vert].Position.Y = vb.Y;
        VertexPositionColor[] array2 = verts;
        num = vert;
        vert = num + 1;
        array2[num].Color = cb;
        verts[vert].Position.X = vc.X;
        verts[vert].Position.Y = vc.Y;
        VertexPositionColor[] array3 = verts;
        num = vert;
        vert = num + 1;
        array3[num].Color = cc;
        verts[vert].Position.X = va.X;
        verts[vert].Position.Y = va.Y;
        VertexPositionColor[] array4 = verts;
        num = vert;
        vert = num + 1;
        array4[num].Color = ca;
        verts[vert].Position.X = vc.X;
        verts[vert].Position.Y = vc.Y;
        VertexPositionColor[] array5 = verts;
        num = vert;
        vert = num + 1;
        array5[num].Color = cc;
        verts[vert].Position.X = vd.X;
        verts[vert].Position.Y = vd.Y;
        VertexPositionColor[] array6 = verts;
        num = vert;
        vert = num + 1;
        array6[num].Color = cd;
    }

    private void Edge(ref int vert, Vector2 a, Vector2 b, float fade, float insetFade, bool continueFromPrevious, bool createAfter) {
        float length = (a - b).Length();
        float num2 = (OnlyMode == OnlyModes.None) ? (insetFade / length) : 0f;
        float maxSteps = length / SurfaceStep;
        var dir = (b - a).SafeNormalize();
        Vector2 perpendicular = (b - a).SafeNormalize().Perpendicular();
        int i = 1;
        while (i <= maxSteps) {
            Vector2 prevPos = Vector2.Lerp(a, b, (i - 1) / maxSteps);
            Vector2 pos = Vector2.Lerp(a, b, i / maxSteps);
            
            float wave = Wave(i, length);
            float prevWave = Wave(i - 1, length);
            
            Vector2 inner = pos - perpendicular * wave;
            Vector2 outer = prevPos - perpendicular * prevWave;
            
            Vector2 value3 = Vector2.Lerp(a, b, Calc.ClampedMap((i - 1) / maxSteps, 0f, 1f, num2, 1f - num2));
            Vector2 value4 = Vector2.Lerp(a, b, Calc.ClampedMap(i / maxSteps, 0f, 1f, num2, 1f - num2));
            
            if (continueFromPrevious && i == 1) {
                // finish the first tri
                Vector2 loc = value3 + perpendicular * (fade - prevWave);
                verts[vert].Position.X = loc.X;
                verts[vert].Position.Y = loc.Y;
                verts[vert].Color = CenterColor;
                vert++;
                
                // make the 2nd one
                verts[vert].Position.X = outer.X + perpendicular.X;
                verts[vert].Position.Y = outer.Y + perpendicular.Y;
                verts[vert].Color = EdgeColor;
                vert++;
                verts[vert].Position.X = loc.X;
                verts[vert].Position.Y = loc.Y;
                verts[vert].Color = CenterColor;
                vert++;

                verts[vert].Position.X = verts[vert - 7].Position.X;
                verts[vert].Position.Y = verts[vert - 7].Position.Y;
                verts[vert].Color = CenterColor;
                vert++;
            }

            // gradient
            Quad(ref vert, outer + perpendicular, EdgeColor,
                           inner + perpendicular, EdgeColor,
                           value4 + perpendicular * (fade - wave), CenterColor,
                           value3 + perpendicular * (fade - prevWave), CenterColor);

            // background

            Quad(ref vert, value3 + perpendicular * (fade - prevWave),
                           value4 + perpendicular * (fade - wave),
                           value4 + perpendicular * fade,
                           value3 + perpendicular * fade, CenterColor);

            // surface
            Quad(ref vert, outer,
                           inner,
                           inner + perpendicular * 1f,
                           outer + perpendicular * 1f, SurfaceColor);

            i++;
            if (i > maxSteps && createAfter) {
                verts[vert].Position.X = inner.X + perpendicular.X;
                verts[vert].Position.Y = inner.Y + perpendicular.Y;
                verts[vert].Color = EdgeColor;
                vert++;

                Vector2 loc = value4 + perpendicular * (fade - wave);
                verts[vert].Position.X = loc.X;
                verts[vert].Position.Y = loc.Y;
                verts[vert].Color = CenterColor;
                vert++;
            }
        }
    }

    public override void Render() {
        GameplayRenderer.End();
        if (dirty) {
            vertCount = 0;
            for (int i = 0; i < Fill.Length; i++) {
                verts[vertCount] = new VertexPositionColor(Fill[i], CenterColor); 
                vertCount++;
            }

            Vector2 vector5 = new Vector2(Math.Min(Fade, Width / 2f), Math.Min(Fade, Height / 2f));
            
            for (int i = 0; i < EdgeVertices.Length - 1; i++) {
                Edge(ref vertCount, EdgeVertices[i], EdgeVertices[i + 1], vector5.X, vector5.Y, i > 0, true);
            }
            Edge(ref vertCount, EdgeVertices[^1], EdgeVertices[0], vector5.X, vector5.Y, true, false);
            dirty = false;
        }
        Camera camera = (Scene as Level)!.Camera;
        
        var buffer = RenderTargetHelper.RentFullScreenBuffer();
        var gd = Draw.SpriteBatch.GraphicsDevice;
        var targets = gd.StoreRenderTargets();
        gd.SetRenderTarget(buffer);
        gd.Clear(Color.Transparent);
        
        GFX.DrawVertices(Matrix.CreateTranslation(new Vector3(Position, 0f)) * camera.Matrix, verts, vertCount, null, BlendState.Opaque);
        targets.Restore();
        GameplayRenderer.Begin();
        Draw.SpriteBatch.Draw(buffer, camera.position, Color.White);
        RenderTargetHelper.ReturnFullScreenBuffer(buffer);
        
        Vector2 value = new Vector2(Entity.Position.X, MinY) + Position;
        MTexture mtexture = GFX.Game["particles/bubble"];
        for (int i = 0; i < bubbles.Length; i++) {
            mtexture.DrawCentered(value + bubbles[i].Position, SurfaceColor * bubbles[i].Alpha);
            //Draw.Pixel.Draw(Vector2.UnitX * (value.X + bubbles[i].Position.X) + Vector2.UnitY * (value.Y + bubbles[i].MaxY));
            //Draw.Pixel.Draw(Vector2.UnitX * (value.X + bubbles[i].Position.X) + Vector2.UnitY * (value.Y + bubbles[i].MinY), Vector2.Zero, Color.Blue);
        }

        for (int j = 0; j < surfaceBubbles.Length; j++) {
            if (surfaceBubbles[j].X >= 0f) {
                MTexture mtexture2 = surfaceBubbleAnimations[surfaceBubbles[j].Animation][(int) surfaceBubbles[j].Frame];
                int num = (int) (surfaceBubbles[j].X / SurfaceStep);
                float y = surfaceBubbles[j].Y;//1f - Wave(num, Width);
                mtexture2.DrawJustified(value + new Vector2(num * SurfaceStep, y), new Vector2(0.5f, 1f), SurfaceColor);
            }
        }
    }

    public Vector2 Position;

    public float Fade;

    public float Spikey;

    public OnlyModes OnlyMode;

    public float SmallWaveAmplitude;

    public float BigWaveAmplitude;

    public float CurveAmplitude;

    public float UpdateMultiplier;

    public Color SurfaceColor;

    public Color EdgeColor;

    public Color CenterColor;

    private float timer;

    private VertexPositionColor[] verts;

    private bool dirty;

    private int vertCount;

    private Bubble[] bubbles;

    private SurfaceBubble[] surfaceBubbles;

    private int surfaceBubbleIndex;

    private List<List<MTexture>> surfaceBubbleAnimations;

    public enum OnlyModes {
        None,
        OnlyTop,
        OnlyBottom
    }

    private struct Bubble {
        public Vector2 Position;

        public float MinY;

        public float MaxY;

        public float Speed;

        public float Alpha;
    }

    private struct SurfaceBubble {
        public float X;
        public float Y;

        public float Frame;

        public byte Animation;
    }
}
