using FrostHelper.Helpers;

namespace FrostHelper.Backdrops;

internal sealed class GradientStyleground : Backdrop {
    public readonly Gradient Gradient;
    public readonly Gradient.Directions Direction;
    private VirtualRenderTarget? renderTarget;
    
    public GradientStyleground(BinaryPacker.Element data) {
        var gradientString = data.Attr("gradient");
        if (!Gradient.TryParse(gradientString, null, out Gradient)) {
            NotificationHelper.Notify($"Invalid gradient: {gradientString}");
            Gradient = new() { Entries = [ new() {
                ColorFrom = Color.White,
                ColorTo = Color.White,
                Percent = 100,
            }]};
        }

        Direction = Enum.Parse<Gradient.Directions>(data.Attr("direction", nameof(Gradient.Directions.Vertical)));

        CustomBackdropBlendModeHelper.SetBlendMode(this, CustomBackdropBlendModeHelper.ParseBlendMode(data.Attr("blendMode", "alphablend")));
    }

    public override void BeforeRender(Scene scene) {
        if (renderTarget is null) {
            var vertexes = Gradient.GetVertexes(Direction);
            renderTarget = RenderTargetHelper.RentFullScreenBuffer();
        
            Engine.Graphics.GraphicsDevice.SetRenderTarget(renderTarget);
            Engine.Graphics.GraphicsDevice.Clear(Color.Transparent);
            
            GFX.DrawVertices(Matrix.Identity, vertexes, vertexes.Length);
        }
        
        base.BeforeRender(scene);
    }

    public override void Render(Scene scene) {
        if (renderTarget is null)
            return;
        
        Draw.SpriteBatch.Draw(renderTarget, Vector2.Zero, Color.White);
    }

    public override void Ended(Scene scene) {
        base.Ended(scene);

        if (renderTarget is { } target) {
            RenderTargetHelper.ReturnFullScreenBuffer(target);
        }
    }
}

internal readonly struct Gradient : ISpanParsable<Gradient>
{
    public List<Entry> Entries { get; init; } = [];

    public VertexPositionColor[] GetVertexes(Directions dir) {
        var ret = new VertexPositionColor[Entries.Count * 6];
        var span = ret.AsSpan();
        
        var start = 0f;
        foreach (var entry in Entries) {
            var c1 = entry.ColorFrom;
            var c2 = entry.ColorTo;

            var end = start + entry.Percent;

            const float yUnit = 180f / 100f;
            const float xUnit = 320f / 100f;
            
            var (x1, x2, y1, y2) = dir switch {
                Directions.Vertical => (0f, 320f, start * yUnit, end * yUnit),
                Directions.Horizontal => (start * xUnit, end * xUnit, 0f, 180f),
                _ => (0f, 0f, 0f, 0f)
            };
            
            // explicit bounds check to help the JIT
            if (span.Length >= 6) {
                switch (dir) {
                    case Directions.Vertical:
                        span[0] = new VertexPositionColor(new Vector3(x1, y1, 0f), c1);
                        span[1] = new VertexPositionColor(new Vector3(x2, y1, 0f), c1);
                        span[2] = new VertexPositionColor(new Vector3(x2, y2, 0f), c2);
                        span[3] = new VertexPositionColor(new Vector3(x1, y1, 0f), c1);
                        span[4] = new VertexPositionColor(new Vector3(x2, y2, 0f), c2);
                        span[5] = new VertexPositionColor(new Vector3(x1, y2, 0f), c2);
                        break;
                    case Directions.Horizontal:
                        span[0] = new VertexPositionColor(new Vector3(x1, y1, 0f), c1);
                        span[1] = new VertexPositionColor(new Vector3(x1, y2, 0f), c1);
                        span[2] = new VertexPositionColor(new Vector3(x2, y2, 0f), c2);
                        span[3] = new VertexPositionColor(new Vector3(x1, y1, 0f), c1);
                        span[4] = new VertexPositionColor(new Vector3(x2, y2, 0f), c2);
                        span[5] = new VertexPositionColor(new Vector3(x2, y1, 0f), c2);
                        break;
                }
            }

            start = end;
            span = span[6..];
        }

        return ret;
    }

    public Gradient()
    {
        
    }
    
    
    public static Gradient Parse(string s, IFormatProvider? provider) 
        => Parse(s.AsSpan(), provider);

    public static bool TryParse(string? s, IFormatProvider? provider, out Gradient result) =>
        TryParse(s.AsSpan(), provider, out result);

    public static Gradient Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
    {
        if (!TryParse(s, provider, out var parsed))
            throw new Exception("Invalid gradient");

        return parsed;
    }

    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Gradient result)
    {
        result = new();

        var p = new SpanParser(s);
        while (p.SliceUntil(';').TryUnpack(out var entryParser))
        {
            
            
            entryParser.TrimStart();
            if (!entryParser.ReadUntil<RGBAOrXnaColor>(',').TryUnpack(out var colorFrom))
                return false;
            entryParser.TrimStart();
            if (!entryParser.ReadUntil<RGBAOrXnaColor>(',').TryUnpack(out var colorTo))
                return false;
            entryParser.TrimStart();
            if (!entryParser.TryRead<float>(out var percent))
                return false;

            result.Entries.Add(new Entry {
                ColorFrom = colorFrom.Color,
                ColorTo = colorTo.Color,
                Percent = percent
            });
        }

        return true;
    }

    public struct Entry
    {
        public Color ColorFrom;
        public Color ColorTo;
        public float Percent;
    }

    public enum Directions {
        Vertical,
        Horizontal
    }
}