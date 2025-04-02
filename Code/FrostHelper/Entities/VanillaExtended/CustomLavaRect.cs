using FrostHelper.Helpers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FrostHelper;

internal sealed class CustomLavaRect : Component {
    internal struct WaveData {
        private static readonly Dictionary<string, List<WaveData>> _parsedWaveCache = new();
        
        public float Amplitude, WaveNumber, Frequency, Phase;

        public WaveData(float amplitude, float waveNumber, float frequency) {
            Amplitude = amplitude;
            WaveNumber = waveNumber;
            Frequency = frequency;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Get(float val, float timer) {
            return Sin(val * WaveNumber + timer * Frequency + Phase) * Amplitude;
        }

        public static List<WaveData> ParseWaves(string waves) {
            ref var cachedRef = ref CollectionsMarshal.GetValueRefOrAddDefault(_parsedWaveCache, waves, out _);
            if (cachedRef is {})
                return cachedRef;

            var parsed = new List<WaveData>();
            var parser = new SpanParser(waves);
            while (parser.SliceUntil(';').TryUnpack(out var waveParser)) {
                WaveData waveData = default;

                if (waveParser.ReadUntil<float>(',').TryUnpack(out waveData.Amplitude)
                    && waveParser.ReadUntil<float>(',').TryUnpack(out waveData.WaveNumber)
                    && waveParser.ReadUntil<float>(',').TryUnpack(out waveData.Frequency)
                    && waveParser.Read<float>().TryUnpack(out waveData.Phase)) {
                }
                
                parsed.Add(waveData);
            }

            cachedRef = parsed;

            return parsed;
        }
    }

    [Flags]
    internal enum RainbowModes {
        None = 0,
        Surface = 1,
        Edge = 2,
        Bubble = 4,
        
        All = Surface | Edge | Bubble,
    }

    internal List<WaveData> Waves;
    
    public Vector2 Position;
    public float Fade = 16f;
    public float Spikey;
    public OnlyModes OnlyMode;
    
    public float CurveAmplitude = 12f;
    public float UpdateMultiplier = 1f;
    
    public Color SurfaceColor = Color.White;
    public Color EdgeColor = Color.LightGray;
    public Color CenterColor = Color.DarkGray;

    internal RainbowModes IsRainbow;
    
    private float _timer = Calc.Random.NextFloat(100f);
    private VertexPositionColor[] _verts;
    private bool _dirty;
    private int _vertCount;
    private Bubble[]? _bubbles;
    private SurfaceBubble[]? _surfaceBubbles;
    private int _surfaceBubbleIndex;
    private List<List<MTexture>>? _surfaceBubbleAnimations;

    private float _bubbleAmountMultiplier;
    public bool HasBubbles => _bubbleAmountMultiplier > 0f;

    public int SurfaceStep { get; set; }

    public float Width { get; set; }

    public float Height { get; set; }
    
    private List<MTexture> _bubbleTextures { get; set; }

    private void SetSurfaceBubbleAnimations(ReadOnlySpan<char> key) {
        var parser = new SpanParser(key);

        _surfaceBubbleAnimations = [];
        while (parser.SliceUntil(';').TryUnpack(out var next)) {
            _surfaceBubbleAnimations.Add(GFX.Game.GetAtlasSubtexturesWithNotif(next.Remaining.ToString()));
        }
    }
    
    private void SetBubbleTextures(ReadOnlySpan<char> key) {
        var parser = new SpanParser(key);

        _bubbleTextures = [];
        while (parser.SliceUntil(';').TryUnpack(out var next)) {
            _bubbleTextures.Add(GFX.Game.GetWithNotif(next.Remaining.ToString()));
        }
    }

    private void SetupBubbles(string config) {
        // multiplier|bubbleTexture1;bubbleTexture2...|surfaceBubbles1;surfaceBubbles2...
        var parser = new SpanParser(config);
        ReadOnlySpan<char> bubbleTextures = "particles/bubble";
        ReadOnlySpan<char> surfaceBubbleAnims = "danger/lava/bubble_a";

        parser.ReadUntil<float>('|').TryUnpack(out _bubbleAmountMultiplier);
        if (!parser.IsEmpty && parser.SliceUntil('|').TryUnpack(out var bubblePath)) {
            if (!bubblePath.Remaining.IsEmpty) {
                bubbleTextures = bubblePath.Remaining;
            }
        }
        
        if (!parser.IsEmpty) {
            surfaceBubbleAnims = parser.Remaining;
        }

        SetBubbleTextures(bubbleTextures);
        SetSurfaceBubbleAnimations(surfaceBubbleAnims);
    }

    public CustomLavaRect(float width, float height, int step, string bubbleConfig)
        : base(true, true) {
        SetupBubbles(bubbleConfig);
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
                ref var bubble = ref _bubbles[index];
                bubble.Position =
                    new Vector2(1f + Calc.Random.NextFloat(Width - 2f), Calc.Random.NextFloat(Height));
                bubble.Speed = Calc.Random.Range(4, 12);
                bubble.Alpha = Calc.Random.Range(0.4f, 0.8f);
                bubble.Type = _bubbleTextures.Count == 1 ? (byte)0 : (byte)Calc.Random.Next(_bubbleTextures.Count);
            }

            for (int index = 0; index < _surfaceBubbles.Length; ++index)
                _surfaceBubbles[index].X = -1f;
        }
    }

    private Rectangle GetCullRect() {
        Vector2 basePos = Entity.Position + Position;
        
        var rect = new Rectangle((int)basePos.X, (int)basePos.Y, (int)Width, (int)Height);
        rect.Inflate(16, 16);

        return rect;
    }
    
    private bool IsVisible(out Rectangle cullRect) {
        Camera camera = (Scene as Level)!.Camera;
        cullRect = GetCullRect();
        
        return CameraCullHelper.IsRectangleVisible(cullRect, lenience: 4f, camera);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override void Update() {
        if (!IsVisible(out _))
            return;
        
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Sin(float value) => (1.0f + float.Sin(value)) / 2.0f;

    public float Wave(int step, float length) {
        int val = step * SurfaceStep;
        float num1 = OnlyMode != OnlyModes.All
            ? 1f
            : Calc.ClampedMap(val, 0.0f, length * 0.1f) * Calc.ClampedMap(val, length * 0.9f, length, 1f, 0.0f);

        float num2 = 0;
        foreach (var wave in CollectionsMarshal.AsSpan(Waves)) {
            num2 += wave.Get(val, _timer);
        }
        
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
            
            *v = v[-3];
            v++;
            
            *v = v[-2];
            v++;

            v->Position = vd;
            v->Color = cd;
        }

        vert += 6;
    }

    private interface IWaveProvider {
        float Wave(CustomLavaRect rect, int step, float length);
    }

    private struct DefaultWaveProvider : IWaveProvider {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Wave(CustomLavaRect rect, int step, float length) {
            return rect.Wave(step, length);
        }
    }
    
    private unsafe /*ref*/ struct PrecalculatedWaveProvider(float* waves) : IWaveProvider {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Wave(CustomLavaRect rect, int step, float length) {
            return waves[step];
        }
    }

    private void Edge<TWave>(ref int vert, NumVector2 a, NumVector2 b, float fade, float insetFade, 
                             TWave waveProvider, Rectangle cullRect) 
    where TWave : struct, IWaveProvider {
        
        float length = (a - b).Length();
        float steps = length / SurfaceStep;

        var aVisible = new NumVector2(
            float.Clamp(a.X, cullRect.Left, cullRect.Right),
            float.Clamp(a.Y, cullRect.Top, cullRect.Bottom)
        );
        var bVisible = new NumVector2(
            float.Clamp(b.X, cullRect.Left, cullRect.Right),
            float.Clamp(b.Y, cullRect.Top, cullRect.Bottom)
        );
        var stepsToRenderAnywayDueToFade = float.Max(fade, insetFade) / SurfaceStep / 2;
        float visibleLength = (aVisible - bVisible).Length();
        float visibleSteps = visibleLength / SurfaceStep;
        var startingStep = (int)float.Ceiling(((a - aVisible).Length() / SurfaceStep) - stepsToRenderAnywayDueToFade);
        startingStep = int.Max(startingStep, 0);
        visibleSteps += startingStep;
        visibleSteps = float.Min(steps, visibleSteps + stepsToRenderAnywayDueToFade*2);

        if (startingStep > visibleSteps)
            return;

        /*
        Console.WriteLine($"T: {steps}, skipping: {startingStep}, rendering: {visibleSteps}");
        */
        
        float newMin = OnlyMode == OnlyModes.All ? insetFade / length : 0.0f;
        var inset = (b - a).SafeNormalize().Perpendicular();

        float prevWave = waveProvider.Wave(this, startingStep, length);
        var va = a - (inset * prevWave);
        var vaColor = GetSurfaceColor(va);
        var vaInsetColor = GetSurfaceColor(va + inset);
        var vaInsetEdgeColor = GetEdgeColor(va + inset);
        var prevOtherPos = NumVector2.Lerp(a, b, newMin);
        var prevOtherPosCenterColor = GetCenterColor(prevOtherPos + inset * (fade - prevWave));
        
        for (int step = startingStep + 1; step <= visibleSteps; ++step) {
            var percent = step / steps;
            if (percent > 1f)
                percent = 1f;
            
            var pos = NumVector2.Lerp(a, b, percent);
            float wave = waveProvider.Wave(this, step, length);
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

    public override unsafe void Render() {
        if (!IsVisible(out var rect))
            return;
        
        GameplayRenderer.End();
        
        Camera camera = (Scene as Level)!.Camera;
        Vector2 basePos = Entity.Position + Position;
        var visibleRect = CameraCullHelper.GetVisibleSection(rect, lenience: 4, camera);
        
        if (_dirty) {
            NumVector2 topLeft = default;
            NumVector2 topRight = new(Width, 0f);
            NumVector2 botLeft = new(0f, Height);
            NumVector2 botRight = new(Width, Height);
            NumVector2 fadeByAxis = new(Math.Min(Fade, Width / 2f), Math.Min(Fade, Height / 2f));
            var visibleRectForEdges = visibleRect.MovedBy(-basePos);
            
            _vertCount = 0;
            if (OnlyMode == OnlyModes.All) {
                int steps = (int)float.Ceiling(Width / SurfaceStep) + 1;
                Span<float> waves = stackalloc float[steps];
                for (int i = 0; i < steps; i++) {
                    waves[i] = Wave(i, Width);
                }

                int steps2 = (int)float.Ceiling(Height / SurfaceStep) + 1;
                Span<float> waves2 = Width == Height ? waves : stackalloc float[steps2];
                if (Width != Height) {
                    for (int i = 0; i < steps2; i++) {
                        waves2[i] = Wave(i, Height);
                    }
                }

                // We can't have : allows ref struct yet, so we need to use pointers... oh no
                // This is safe because we know the data is stack-allocated, and has no GC-refs, so it will never be moved under us.
                // (and we need stackalloc to Span<float> instead of float*, because otherwise the ternary for waves2 doesn't work.
                var precalced = new PrecalculatedWaveProvider((float*)Unsafe.AsPointer(ref waves[0]));
                var precalced2 = new PrecalculatedWaveProvider((float*)Unsafe.AsPointer(ref waves2[0]));
                
                Edge(ref _vertCount, topLeft, topRight, fadeByAxis.Y, fadeByAxis.X, precalced, visibleRectForEdges);
                Edge(ref _vertCount, topRight, botRight, fadeByAxis.X, fadeByAxis.Y, precalced2, visibleRectForEdges);
                Edge(ref _vertCount, botRight, botLeft, fadeByAxis.Y, fadeByAxis.X, precalced, visibleRectForEdges);
                Edge(ref _vertCount, botLeft, topLeft, fadeByAxis.X, fadeByAxis.Y, precalced2, visibleRectForEdges);
                
                Quad(ref _vertCount,
                    topLeft + fadeByAxis, GetCenterColor(topLeft + fadeByAxis), 
                    topRight + new NumVector2(-fadeByAxis.X, fadeByAxis.Y), GetCenterColor(topRight + new NumVector2(-fadeByAxis.X, fadeByAxis.Y)),
                    botRight - fadeByAxis, GetCenterColor(botRight - fadeByAxis), 
                    botLeft + new NumVector2(fadeByAxis.X, -fadeByAxis.Y), GetCenterColor(botLeft + new NumVector2(fadeByAxis.X, -fadeByAxis.Y)));
            } else if (OnlyMode == OnlyModes.OnlyTop) {
                Edge(ref _vertCount, topLeft, topRight, fadeByAxis.Y, 0.0f, new DefaultWaveProvider(), visibleRectForEdges);
                Quad(ref _vertCount, topLeft + new NumVector2(0.0f, fadeByAxis.Y),
                    topRight + new NumVector2(0.0f, fadeByAxis.Y), botRight, botLeft, CenterColor);
            } else if (OnlyMode == OnlyModes.OnlyBottom) {
                Edge(ref _vertCount, botRight, botLeft, fadeByAxis.Y, 0.0f, new DefaultWaveProvider(), visibleRectForEdges);
                Quad(ref _vertCount, topLeft, topRight, botRight + new NumVector2(0.0f, -fadeByAxis.Y),
                    botLeft + new NumVector2(0.0f, -fadeByAxis.Y), CenterColor);
            }

            _dirty = false;
        }


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
            var bubbles = _bubbles;
            var bubbleTextures = CollectionsMarshal.AsSpan(_bubbleTextures);
            
            for (int index = 0; index < bubbles!.Length; ++index) {
                ref var bubble = ref bubbles[index];
                var pos = basePos + bubble.Position;
                if (visibleRect.Contains(pos.ToPoint()))
                    bubbleTextures[bubble.Type].DrawCentered(pos, GetBubbleColor(pos.ToNumerics()) * bubbles[index].Alpha);
            }

            var surfaceBubbles = _surfaceBubbles;
            for (int index = 0; index < surfaceBubbles!.Length; ++index) {
                ref var surfaceBubble = ref surfaceBubbles[index];
                
                if (surfaceBubble.X >= 0.0) {
                    MTexture surfaceTexture = _surfaceBubbleAnimations![surfaceBubble.Animation][(int) surfaceBubble.Frame];
                    int step = (int) (surfaceBubble.X / SurfaceStep);
                    float y = 1f - Wave(step, Width);
                    var position = basePos + new Vector2(step * SurfaceStep, y);
                    if (visibleRect.Contains(position.ToPoint()))
                        surfaceTexture.DrawJustified(position, new Vector2(0.5f, 1f), GetBubbleColor(position.ToNumerics()));
                }
            }
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Color GetBubbleColor(NumVector2 pos) {
        return (IsRainbow & RainbowModes.Bubble) != 0 ? ColorHelper.GetHue(Scene, Entity.Position.Add(pos)) : SurfaceColor;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Color GetSurfaceColor(NumVector2 pos) {
        return (IsRainbow & RainbowModes.Surface) != 0 ? ColorHelper.GetHue(Scene, Entity.Position.Add(pos)) : SurfaceColor;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Color GetEdgeColor(NumVector2 pos) {
        return (IsRainbow & RainbowModes.Edge) != 0 ? MultiplyNoAlpha(ColorHelper.GetHue(Scene, Entity.Position.Add(pos)), 0.8f) : EdgeColor;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Color GetCenterColor(NumVector2 pos) {
        //return MultiplyNoAlpha(ColorHelper.GetHue(Scene, Entity.Position), 0.6f);
        return CenterColor;
        //return (IsRainbow & RainbowModes.Edge) != 0 ? MultiplyNoAlpha(ColorHelper.GetHue(Scene, Entity.Position.Add(pos)), 0.6f) : CenterColor;
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
        public byte Type;
    }

    public struct SurfaceBubble {
        public float X;
        public float Frame;
        public byte Animation;
    }
}