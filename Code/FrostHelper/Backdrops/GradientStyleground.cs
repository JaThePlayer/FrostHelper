using FrostHelper.Helpers;

namespace FrostHelper.Backdrops;

internal sealed class GradientStyleground : Backdrop {
    private Gradient Gradient;
    private readonly Gradient.Directions Direction;
    
    // Render cache
    private VirtualRenderTarget? renderTarget;
    private Vector2 cachedPosition;
    private VertexPositionColor[]? vertexPositionColors;
    
    public GradientStyleground(BinaryPacker.Element data) {
        var gradientString = data.Attr("gradient");
        if (!Gradient.TryParse(gradientString, null, out Gradient)) {
            NotificationHelper.Notify($"Invalid gradient: {gradientString}");
            Gradient = new() { Entries = [ new() {
                ColorFrom = Color.Black,
                ColorTo = Color.Black,
                Percent = 100,
            }]};
        }

        Direction = Enum.Parse<Gradient.Directions>(data.Attr("direction", nameof(Gradient.Directions.Vertical)));
        LoopX = data.AttrBool("loopX");
        LoopY = data.AttrBool("loopY");

        CustomBackdropBlendModeHelper.SetBlendMode(this, CustomBackdropBlendModeHelper.ParseBlendMode(data.Attr("blendMode", "alphablend")));
    }

    public override void BeforeRender(Scene scene) {
        var shouldRerender = renderTarget is null;
        
        if (Position != cachedPosition) {
            shouldRerender = true;
            cachedPosition = Position;
        }

        if (renderTarget is { } && GameplayBuffers.Gameplay.Width != renderTarget.Width) {
            renderTarget.Dispose();
            renderTarget = null;
            shouldRerender = true;
        }
        
        if (shouldRerender) {
            renderTarget ??= RenderTargetHelper.RentFullScreenBuffer();
            Gradient.GetVertexes(ref vertexPositionColors, Direction, Position, LoopX, LoopY, out var vertexCount);
            var gd = Engine.Graphics.GraphicsDevice;
            gd.SetRenderTarget(renderTarget);
            gd.Clear(Color.Transparent);
            
            GFX.DrawVertices(Matrix.Identity, vertexPositionColors, vertexCount);
        }
        
        base.BeforeRender(scene);
    }

    public override void Render(Scene scene) {
        if (renderTarget is null)
            return;
        if (scene is not Level level)
            return;

        var target = GameplayBuffers.Gameplay.Width / 320;
        var scale = 1f / float.Min(level.Zoom, target);
        Draw.SpriteBatch.Draw(renderTarget, default, null, Color.White, 0f, default, scale, SpriteEffects.None, 0f);
    }

    public override void Ended(Scene scene) {
        base.Ended(scene);

        if (renderTarget is { } target) {
            renderTarget = null;
            RenderTargetHelper.ReturnFullScreenBuffer(target);
        }
    }
}

internal class Gradient : ISpanParsable<Gradient>
{
    public List<Entry> Entries { get; init; } = [];
    private float? entryPercentageSum = null;
    
    const float yUnit = 180f / 100f;
    const float xUnit = 320f / 100f;

    public void GetVertexes(ref VertexPositionColor[]? into, Directions dir, Vector2 basePos, bool loopX, bool loopY, out int vertexCount) {
        entryPercentageSum ??= Entries.Sum(e => e.Percent);

        // Perf: Modulo the position of looping directions by the size of the gradient
        if (loopY && dir == Directions.Vertical) {
            basePos.Y %= entryPercentageSum.Value * yUnit * 2;
        } else if (loopX && dir == Directions.Horizontal) {
            basePos.X %= entryPercentageSum.Value * xUnit * 2;
        }
        
        vertexCount = 0;
        into ??= new VertexPositionColor[Entries.Count * 6];
        
        March(ref into, dir, basePos, loopX, loopY, ref vertexCount, moveInverted: false);

        if (dir == Directions.Vertical && loopY && basePos.Y > 0f) {
            // we've started moved down a bit, we need to march upwards
            March(ref into, dir, basePos, loopX, loopY, ref vertexCount, moveInverted: true);
        }
        else if (dir == Directions.Horizontal && loopY && basePos.X > 0f) {
            // we've started moved right a bit, we need to march left
            March(ref into, dir, basePos, loopX, loopY, ref vertexCount, moveInverted: true);
        }
    }

    private void March(ref VertexPositionColor[] ret, Directions dir, Vector2 basePos, bool loopX, bool loopY,
        ref int vertexCount, bool moveInverted) {
        var span = ret.AsSpan(vertexCount);
        var i = 0;
        var entries = Entries;
        var inc = 1;
        var start = 0f;
        while (true) {
            var entry = entries[i];

            var end = start + entry.Percent * (moveInverted ? -1 : 1);
            
            var (x1, x2, y1, y2) = dir switch {
                Directions.Vertical => (0f, 320f, start * yUnit, end * yUnit),
                Directions.Horizontal => (start * xUnit, end * xUnit, 0f, 180f),
                _ => (0f, 0f, 0f, 0f)
            };

            // No point in moving on a looping axis if the gradient is not in that direction
            if (!(loopX && dir == Directions.Vertical)) {
                x1 += basePos.X;
                x2 += basePos.X;
            }
            if (!(loopY && dir == Directions.Horizontal)) {
                y1 += basePos.Y;
                y2 += basePos.Y;
            }

            // Cull entries above or to the left of the screen
            if ((x1 >= 0 || x2 >= 0) && (y1 >= 0 || y2 >= 0)) {
                if (span.Length < 6) {
                    Array.Resize(ref ret, ret.Length + 6);
                    span = ret.AsSpan()[^6..];
                }

                var c1 = inc < 0 ? entry.ColorTo : entry.ColorFrom;
                var c2 = inc < 0 ? entry.ColorFrom : entry.ColorTo;
                
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

                    vertexCount += 6;
                    span = span[6..];
                }
            }
            
            // Return early if we have already covered the entire screen
            if (dir == Directions.Vertical && (moveInverted ? y2 < 0f : y2 >= 180f)) {
                break;
            }

            if (dir == Directions.Horizontal && (moveInverted ? x2 < 0f : x2 >= 320f)) {
                break;
            }

            // We ran out of entries
            if (i + inc >= entries.Count || i + inc < 0) {
                // change direction if we're looping in the same direction as the gradient
                if (loopY && dir == Directions.Vertical && (moveInverted ? y2 >= 0f : y2 < 180f)) {
                    inc *= -1;
                } else if (loopX && dir == Directions.Horizontal && (moveInverted ? x2 >= 0f : x2 < 320f)) {
                    inc *= -1;
                } else
                    break;
            } else {
                i += inc;
            }

            start = end;
        }
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