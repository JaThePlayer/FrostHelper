using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FrostHelper;

internal sealed class CustomLavaRect : Component {
    public Vector2 Position;
    public float Fade = 16f;
    public float Spikey;
    public OnlyModes OnlyMode;
    
    public float SmallWaveAmplitude = 1f;
    public float BigWaveAmplitude = 4f;
    public float CurveAmplitude = 12f;
    public float UpdateMultiplier = 1f;
    
    public Color SurfaceColor = Color.White;
    public Color EdgeColor = Color.LightGray;
    public Color CenterColor = Color.DarkGray;

    public bool IsRainbow;
    
    private float _timer = Calc.Random.NextFloat(100f);
    private VertexPositionColor[] _verts;
    private bool _dirty;
    private int _vertCount;
    private Bubble[]? _bubbles;
    private SurfaceBubble[]? _surfaceBubbles;
    private int _surfaceBubbleIndex;
    private List<List<MTexture>>? _surfaceBubbleAnimations;

    private readonly float _bubbleAmountMultiplier;
    private bool HasBubbles => _bubbleAmountMultiplier > 0f;

    public int SurfaceStep { get; set; }

    public float Width { get; set; }

    public float Height { get; set; }

    public CustomLavaRect(float width, float height, int step, float bubbleAmountMultiplier)
        : base(true, true) {
        _bubbleAmountMultiplier = bubbleAmountMultiplier;
        Resize(width, height, step);
    }

    public void Resize(float width, float height, int step) {
        Width = width;
        Height = height;
        SurfaceStep = step;
        _dirty = true;
        _verts = new VertexPositionColor[(int) (width / (double) SurfaceStep * 2.0 +
                                                height / (double) SurfaceStep * 2.0 + 4.0) * 3 * 6 + 6];

        if (HasBubbles) {
            _bubbles = new Bubble[(int) (width * (double) height * 0.004999999888241291 * _bubbleAmountMultiplier)];
            _surfaceBubbles = new SurfaceBubble[(int) Math.Max(4.0, _bubbles.Length * 0.25)];
            
            for (int index = 0; index < _bubbles.Length; ++index) {
                _bubbles[index].Position =
                    new Vector2(1f + Calc.Random.NextFloat(Width - 2f), Calc.Random.NextFloat(Height));
                _bubbles[index].Speed = Calc.Random.Range(4, 12);
                _bubbles[index].Alpha = Calc.Random.Range(0.4f, 0.8f);
            }

            for (int index = 0; index < _surfaceBubbles.Length; ++index)
                _surfaceBubbles[index].X = -1f;
            _surfaceBubbleAnimations = [
                GFX.Game.GetAtlasSubtextures("danger/lava/bubble_a")
            ];
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override void Update() {
        _timer += UpdateMultiplier * Engine.DeltaTime;
        if (UpdateMultiplier != 0.0)
            _dirty = true;

        if (HasBubbles) {
            var bubbles = _bubbles!;
            for (int index = 0; index < bubbles.Length; ++index) {
                ref var bubble = ref bubbles[index];
                bubble.Position.Y -= UpdateMultiplier * bubble.Speed * Engine.DeltaTime;
                if (bubble.Position.Y < 2.0 - Wave((int) (bubble.Position.X / (double) SurfaceStep), Width)) {
                    bubble.Position.Y = Height - 1f;
                    if (Calc.Random.Chance(0.75f)) {
                        ref var surfaceBubble = ref _surfaceBubbles![_surfaceBubbleIndex];
                        surfaceBubble.X = bubble.Position.X;
                        surfaceBubble.Frame = 0.0f;
                        surfaceBubble.Animation = (byte) Calc.Random.Next(_surfaceBubbleAnimations!.Count);
                        _surfaceBubbleIndex = (_surfaceBubbleIndex + 1) % _surfaceBubbles.Length;
                    }
                }
            }

            var surfaceBubbles = _surfaceBubbles!;
            for (int index = 0; index < surfaceBubbles.Length; ++index) {
                ref var surfaceBubble = ref surfaceBubbles[index];
                if (surfaceBubble.X >= 0.0) {
                    surfaceBubble.Frame += Engine.DeltaTime * 6f;
                    if (surfaceBubble.Frame >= _surfaceBubbleAnimations![surfaceBubble.Animation].Count)
                        surfaceBubble.X = -1f;
                }
            }
        }

        base.Update();
    }

    public float Sin(float value) => (float) ((1.0 + Math.Sin(value)) / 2.0);

    public float Wave(int step, float length) {
        int val = step * SurfaceStep;
        float num1 = OnlyMode != OnlyModes.All
            ? 1f
            : Calc.ClampedMap(val, 0.0f, length * 0.1f) * Calc.ClampedMap(val, length * 0.9f, length, 1f, 0.0f);
        float num2 = Sin((float) (val * 0.25 + _timer * 4.0)) * SmallWaveAmplitude +
                     Sin((float) (val * 0.05000000074505806 + _timer * 0.5)) * BigWaveAmplitude;
        if (step % 2 == 0)
            num2 += Spikey;
        if (OnlyMode != OnlyModes.All)
            num2 += (1f - Calc.YoYo(val / length)) * CurveAmplitude;
        return num2 * num1;
    }

    public void Quad(ref int vert, NumVector2 va, NumVector2 vb, NumVector2 vc, NumVector2 vd, Color color) {
        Quad(ref vert, va, color, vb, color, vc, color, vd, color);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct VertexPositionColorNumerics {
        public NumVector2 Position;
        public float PositionZ;
        public Color Color;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void Quad(
        ref int vert,
        NumVector2 va,
        Color ca,
        NumVector2 vb,
        Color cb,
        NumVector2 vc,
        Color cc,
        NumVector2 vd,
        Color cd) {

        fixed (VertexPositionColor* verts = &_verts[vert]) {
            var v = (VertexPositionColorNumerics*)verts;
            v->Position = va;
            v->Color = ca;
            v++;

            v->Position = vb;
            v->Color = cb;
            v++;

            v->Position = vc;
            v->Color = cc;
            v++;

            v->Position = va;
            v->Color = ca;
            v++;

            v->Position = vc;
            v->Color = cc;
            v++;

            v->Position = vd;
            v->Color = cd;
        }

        vert += 6;
    }

    public void Edge(ref int vert, NumVector2 a, NumVector2 b, float fade, float insetFade) {
        float length = (a - b).Length();
        float newMin = OnlyMode == OnlyModes.All ? insetFade / length : 0.0f;
        float steps = length / SurfaceStep;
        var inset = (b - a).SafeNormalize().Perpendicular();

        float prevWave = Wave(0, length);
        var va = a - (inset * prevWave);
        var vaColor = GetSurfaceColor(va);
        var vaInsetColor = GetSurfaceColor(va + inset);
        var vaInsetEdgeColor = GetEdgeColor(va + inset);
        var prevOtherPos = NumVector2.Lerp(a, b, newMin);
        var prevOtherPosCenterColor = GetCenterColor(prevOtherPos + inset * (fade - prevWave));
        
        for (int step = 1; step <= steps; ++step) {
            var percent = step / steps;
            if (percent > 1f)
                percent = 1f;
            
            var pos = NumVector2.Lerp(a, b, percent);
            float wave = Wave(step, length);
            var vb = pos - (inset * wave);
            var otherPos = NumVector2.Lerp(a, b, Calc.ClampedMap(percent, 0.0f, 1f, newMin, 1f - newMin));

            var vC = otherPos + inset * (fade - wave);
            var cC = GetCenterColor(vC);
            var vD = prevOtherPos + inset * (fade - prevWave);
            
            Quad(ref vert, 
                va + inset, vaInsetEdgeColor,
                vb + inset, vaInsetEdgeColor = GetEdgeColor(vb + inset),
                vC, cC,
                vD, prevOtherPosCenterColor
            );
            Quad(ref vert,
                vD, prevOtherPosCenterColor,
                vC, cC,
                otherPos + inset * fade, GetCenterColor(prevOtherPos + inset * fade), 
                prevOtherPos + inset * fade, GetCenterColor(prevOtherPos + inset * fade)
            );

            var vbInsetColor = GetSurfaceColor(vb + inset);
            Quad(ref vert, 
                va, vaColor,
                vb, vaColor = GetSurfaceColor(vb),
                vb + inset, vbInsetColor,
                va + inset, vaInsetColor
            );

            prevWave = wave;
            va = vb;
            vaInsetColor = vbInsetColor;
            prevOtherPos = otherPos;
            prevOtherPosCenterColor = cC;
        }
    }

    public override void Render() {
        GameplayRenderer.End();
        if (_dirty) {
            NumVector2 topLeft = default;
            NumVector2 topRight = new(Width, 0f);
            NumVector2 botLeft = new(0f, Height);
            NumVector2 botRight = new(Width, Height);
            NumVector2 fadeCenter = new(Math.Min(Fade, Width / 2f), Math.Min(Fade, Height / 2f));
            
            _vertCount = 0;
            if (OnlyMode == OnlyModes.All) {
                Edge(ref _vertCount, topLeft, topRight, fadeCenter.Y, fadeCenter.X);
                Edge(ref _vertCount, topRight, botRight, fadeCenter.X, fadeCenter.Y);
                Edge(ref _vertCount, botRight, botLeft, fadeCenter.Y, fadeCenter.X);
                Edge(ref _vertCount, botLeft, topLeft, fadeCenter.X, fadeCenter.Y);
                
                Quad(ref _vertCount,
                    topLeft + fadeCenter, GetCenterColor(topLeft + fadeCenter), 
                    topRight + new NumVector2(-fadeCenter.X, fadeCenter.Y), GetCenterColor(topRight + new NumVector2(-fadeCenter.X, fadeCenter.Y)),
                    botRight - fadeCenter, GetCenterColor(botRight - fadeCenter), 
                    botLeft + new NumVector2(fadeCenter.X, -fadeCenter.Y), GetCenterColor(botLeft + new NumVector2(fadeCenter.X, -fadeCenter.Y)));
            } else if (OnlyMode == OnlyModes.OnlyTop) {
                Edge(ref _vertCount, topLeft, topRight, fadeCenter.Y, 0.0f);
                Quad(ref _vertCount, topLeft + new NumVector2(0.0f, fadeCenter.Y),
                    topRight + new NumVector2(0.0f, fadeCenter.Y), botRight, botLeft, CenterColor);
            } else if (OnlyMode == OnlyModes.OnlyBottom) {
                Edge(ref _vertCount, botRight, botLeft, fadeCenter.Y, 0.0f);
                Quad(ref _vertCount, topLeft, topRight, botRight + new NumVector2(0.0f, -fadeCenter.Y),
                    botLeft + new NumVector2(0.0f, -fadeCenter.Y), CenterColor);
            }

            _dirty = false;
        }

        Camera camera = (Scene as Level)!.Camera;
        Vector2 basePos = Entity.Position + Position;
        var buffer = RenderTargetHelper.RentFullScreenBuffer();
        var targets = Draw.SpriteBatch.GraphicsDevice.GetRenderTargets();
        Draw.SpriteBatch.GraphicsDevice.SetRenderTarget(buffer);
        Draw.SpriteBatch.GraphicsDevice.Clear(Color.Transparent);
        
        GFX.DrawVertices(Matrix.CreateTranslation(new Vector3(basePos, 0.0f)) * camera.Matrix, _verts, _vertCount, blendState: BlendState.Opaque);

        Draw.SpriteBatch.GraphicsDevice.SetRenderTargets(targets);
        GameplayRenderer.Begin();
        Draw.SpriteBatch.Draw(buffer, camera.position, Color.White);
        RenderTargetHelper.ReturnFullScreenBuffer(buffer);
        if (HasBubbles) {
            MTexture bubbleTexture = GFX.Game["particles/bubble"];
            for (int index = 0; index < _bubbles!.Length; ++index) {
                var pos = basePos + _bubbles[index].Position;
                bubbleTexture.DrawCentered(pos, GetSurfaceColor(pos.ToNumerics()) * _bubbles[index].Alpha);
            }

            for (int index = 0; index < _surfaceBubbles!.Length; ++index) {
                if (_surfaceBubbles[index].X >= 0.0) {
                    MTexture surfaceTexture = _surfaceBubbleAnimations![_surfaceBubbles[index].Animation][(int) _surfaceBubbles[index].Frame];
                    int step = (int) (_surfaceBubbles[index].X / SurfaceStep);
                    float y = 1f - Wave(step, Width);
                    var position = basePos + new Vector2(step * SurfaceStep, y);
                    surfaceTexture.DrawJustified(position, new Vector2(0.5f, 1f), GetSurfaceColor(position.ToNumerics()));
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Color GetSurfaceColor(NumVector2 pos) {
        return IsRainbow ? ColorHelper.GetHue(Scene, Entity.Position.Add(pos)) : SurfaceColor;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Color GetEdgeColor(NumVector2 pos) {
        return IsRainbow ? MultiplyNoAlpha(ColorHelper.GetHue(Scene, Entity.Position.Add(pos)), 0.8f) : EdgeColor;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Color GetCenterColor(NumVector2 pos) {
        //return MultiplyNoAlpha(ColorHelper.GetHue(Scene, Entity.Position), 0.6f);
        return CenterColor;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Color MultiplyNoAlpha(Color value, float scale)
    {
        return new Color((int) (value.R * scale), (int) (value.G * scale), (int) (value.B * scale), value.A);
    }

    public enum OnlyModes {
        All,
        OnlyTop,
        OnlyBottom,
    }

    public struct Bubble {
        public Vector2 Position;
        public float Speed;
        public float Alpha;
    }

    public struct SurfaceBubble {
        public float X;
        public float Frame;
        public byte Animation;
    }
}