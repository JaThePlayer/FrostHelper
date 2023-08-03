using FrostHelper.ModIntegration;

namespace FrostHelper;

/// <summary>
/// A custom version of the LightningRenderer that's used for arbitrary shape lightning entities
/// </summary>
[Tracked]
public class CustomLightningRenderer : Entity {
    public List<Edge> edges;
    public List<Bolt> Bolts;
    public List<Fill> fills;
#if TRIFILL
    private List<TriFill> triFills = new();
#endif
    public List<Vector3[]> arbitraryFills;
    public VertexPositionColor[] edgeVerts;
    public VertexPositionColor[] bloomVerts;
    public Color[] ElectricityColors;
    public Color[] electricityColorsLerped;
    public float Fade;
    public uint edgeSeed;
    public uint leapSeed;
    public bool UpdateSeeds;
    public bool DrawEdges;

    private static CustomLightningRenderer? justAddedRenderer = null;

    public CustomLightningRenderer() {
        edges = new();
        Bolts = new();
        fills = new();
        arbitraryFills = new();
        ElectricityColors = new[]
        {
                Calc.HexToColor("fcf579"),
                Calc.HexToColor("8cf7e2")
        };
        UpdateSeeds = true;
        DrawEdges = true;
        Tag = (Tags.Global | Tags.TransitionUpdate);
        Depth = -1000100;
        electricityColorsLerped = new Color[ElectricityColors.Length];
        Add(new CustomBloom(OnRenderBloom));
        //base.Add(this.AmbientSfx = new SoundSource());
        //this.AmbientSfx.DisposeOnTransition = false;
        edgeVerts = new VertexPositionColor[1024];
        bloomVerts = new VertexPositionColor[1024];
    }

    /// <summary>
    /// To be called in entity.Added(Scene scene)
    /// </summary>
    public static CustomLightningRenderer AddToSceneIfNeeded(Scene scene) {
        var tracked = scene.Tracker.SafeGetEntity<CustomLightningRenderer>();
        if (tracked is null && justAddedRenderer is null) {
            scene.Add(justAddedRenderer = new CustomLightningRenderer());
            return justAddedRenderer;
        }

        return tracked ?? justAddedRenderer!;
    }

    public static CustomLightningRenderer Get(Scene scene) {
        return scene.Tracker.SafeGetEntity<CustomLightningRenderer>() ?? justAddedRenderer!;
    }

    public void SetColors(Color[] colors) {
        ElectricityColors = colors;
        var bolts = Bolts;
        for (int i = 0; i < bolts.Count; i++) {
            bolts[i].Color = colors[i % 2];
        }
    }

    public override void Awake(Scene scene) {
        base.Awake(scene);
        // no longer needed
        justAddedRenderer = null;

        API.API.GetLightningColors(out var a, out var b, out var _, out float _);
        SetColors(new[] { a, b });
    }

    public override void SceneEnd(Scene scene) {
        base.SceneEnd(scene);
        edges = new();
        Bolts = new();
    }

    public void Add(Edge edge) {
        edges.Add(edge);
    }

    public void Add(Bolt bolt) {
        Bolts.Add(bolt);
    }

    public void Add(Fill fill) {
        fills.Add(fill);
    }

#if TRIFILL
    public void Add(TriFill fill) {
        triFills.Add(fill);
    }
#endif

    public void Add(Vector3[] fill) {
        arbitraryFills.Add(fill);
    }

    public void Remove(Edge edge) {
        edges.Remove(edge);
    }

    public void Remove(Bolt bolt) {
        Bolts.Remove(bolt);
    }

    public void Remove(Fill fill) {
        fills.Remove(fill);
    }

#if TRIFILL
    public void Remove(TriFill fill) {
        triFills.Remove(fill);
    }
#endif
    public void Remove(Vector3[] fill) {
        arbitraryFills.Remove(fill);
    }


    public void ToggleEdges(bool immediate = false) {
        Camera camera = (Scene as Level)!.Camera;
        Rectangle rectangle = new Rectangle((int) camera.Left - 4, (int) camera.Top - 4, (int) (camera.Right - camera.Left) + 8, (int) (camera.Bottom - camera.Top) + 8);
        for (int i = 0; i < edges.Count; i++) {
            if (immediate) {
                edges[i].Visible = edges[i].InView(ref rectangle);
            } else if (!edges[i].Visible && Scene.OnInterval(0.05f, i * 0.01f) && edges[i].InView(ref rectangle)) {
                edges[i].Visible = true;
            } else if (edges[i].Visible && Scene.OnInterval(0.25f, i * 0.01f) && !edges[i].InView(ref rectangle)) {
                edges[i].Visible = false;
            }
        }
    }
    public override void Update() {
        ToggleEdges(false);
        foreach (Bolt bolt in Bolts) {
            bolt.Update(Scene);
        }
        if (UpdateSeeds) {
            if (Scene.OnInterval(0.1f)) {
                edgeSeed = (uint) Calc.Random.Next();
            }
            if (Scene.OnInterval(0.7f)) {
                leapSeed = (uint) Calc.Random.Next();
            }
        }
    }
    private void OnRenderBloom() {
        Camera camera = (Scene as Level)!.Camera;
        Color color = Color.White * (0.25f + Fade * 0.75f);
        int num = 0;
        float scale = HDlesteCompat.Scale;

        foreach (Edge edge in edges) {
            if (edge.Visible) {
                Draw.Line((edge.Parent.Position + edge.A), (edge.Parent.Position + edge.B), color, 4f * scale);
            }
        }

        RenderFills(ref num, ref bloomVerts, color);

        if (num > 0) {
            GameplayRenderer.End();
            GFX.DrawVertices(camera.Matrix * Matrix.CreateScale(scale), bloomVerts, num, null, null);
            GameplayRenderer.Begin();
        }

        if (Fade > 0f) {
            var level = (Scene as Level)!;
            Draw.Rect(level.Camera.X, level.Camera.Y, 320f * scale, 180f * scale, Color.White * Fade);
        }
    }

    public void RenderFills(ref int index, ref VertexPositionColor[] verts, Color color) {
        foreach (Fill fill in fills) {
            bloomVerts[index].Color = color;
            bloomVerts[index].Position = new Vector3(fill.R.X, fill.R.Y, 0f);
            index++;
            bloomVerts[index].Color = color;
            bloomVerts[index].Position = new Vector3(fill.R.X + fill.R.Width, fill.R.Y, 0f);
            index++;
            bloomVerts[index].Color = color;
            bloomVerts[index].Position = new Vector3(fill.R.X + fill.R.Width, fill.R.Y + fill.R.Height, 0f);
            index++;

            bloomVerts[index].Color = color;
            bloomVerts[index].Position = new Vector3(fill.R.X, fill.R.Y, 0f);
            index++;
            bloomVerts[index].Color = color;
            bloomVerts[index].Position = new Vector3(fill.R.X, fill.R.Y + fill.R.Height, 0f);
            index++;
            bloomVerts[index].Color = color;
            bloomVerts[index].Position = new Vector3(fill.R.X + fill.R.Width, fill.R.Y + fill.R.Height, 0f);
            index++;
        }

#if TRIFILL
        foreach (TriFill edge in triFills) {
            bloomVerts[index].Color = color;
            bloomVerts[index].Position = new Vector3(edge.Tri.A, 0f);
            index++;
            bloomVerts[index].Color = color;
            bloomVerts[index].Position = new Vector3(edge.Tri.B, 0f);
            index++;
            bloomVerts[index].Color = color;
            bloomVerts[index].Position = new Vector3(edge.Tri.C, 0f);
            index++;
        }
#endif

        for (int i = 0; i < arbitraryFills.Count; i++) {
            var fill = arbitraryFills[i];

            for (int j = 0; j < fill.Length; j++) {
                verts[index].Color = color;
                verts[index].Position = fill[j];
                index++;
            }
        }
    }

    public override void Render() {
        base.Render();
        Camera camera = (Scene as Level)!.Camera;
        int num = 0;
        API.API.GetLightningFillColor(out Color fillColor, out float fillColorMultiplier);
        RenderFills(ref num, ref edgeVerts, fillColor * fillColorMultiplier);

        if (edges.Count > 0) {
            for (int i = 0; i < electricityColorsLerped.Length; i++) {
                electricityColorsLerped[i] = Color.Lerp(ElectricityColors[i], Color.White, Fade);
            }
            uint num2 = leapSeed;
            foreach (Edge edge in edges) {
                if (edge.Visible) {
                    DrawSimpleLightning(ref num, ref edgeVerts, edgeSeed, edge.Parent.Position, edge.A, edge.B, electricityColorsLerped[0], 1f + Fade * 3f);
                    DrawSimpleLightning(ref num, ref edgeVerts, edgeSeed + 1u, edge.Parent.Position, edge.A, edge.B, electricityColorsLerped[1], 1f + Fade * 3f);
                    if (PseudoRand(ref num2) % 30u == 0u) {
                        DrawBezierLightning(ref num, ref edgeVerts, edgeSeed, edge.Parent.Position, edge.A, edge.B, 24f, 10, electricityColorsLerped[1]);
                    }
                }
            }
            if (num > 0) {
                GameplayRenderer.End();
                GFX.DrawVertices(camera.Matrix * Matrix.CreateScale(HDlesteCompat.Scale), edgeVerts, num, null, null);
                GameplayRenderer.Begin();
            }
        }
    }

    public static void DrawSimpleLightning(ref int index, ref VertexPositionColor[] verts, uint seed, Vector2 pos, Vector2 a, Vector2 b, Color color, float thickness = 1f) {
        seed += (uint) (a.GetHashCode() + b.GetHashCode());
        a += pos;
        b += pos;
        float len = (b - a).Length();
        Vector2 segment = (b - a) / len;
        Vector2 perpendicularSegment = segment.TurnRight();
        a += perpendicularSegment;
        b += perpendicularSegment;
        var perpendicularSegmentTimesThickness = perpendicularSegment * thickness;

        Vector2 start = a;
        int num2 = (PseudoRand(ref seed) % 2u == 0u) ? -1 : 1;
        float num3 = PseudoRandRange(ref seed, 0f, 6.28318548f);
        float curLen = 0f;
        float maxIndex = index + ((b - a).Length() / 4f + 1f) * 6f;
        while (maxIndex >= verts.Length) {
            Array.Resize(ref verts, verts.Length * 2);
        }
        int i = index;
        while (i < maxIndex) {
            verts[i].Color = color;
            i++;
        }
        do {
            float extraLen = PseudoRandRange(ref seed, 0f, 4f);
            num3 += 0.1f;
            curLen += 4f + extraLen;
            Vector2 end = a + segment * curLen;
            if (curLen < len) {
                end += num2 * perpendicularSegment * extraLen - perpendicularSegment;
            } else {
                end = b;
            }

            verts[index++].Position = new Vector3(start - perpendicularSegmentTimesThickness, 0f);
            verts[index++].Position = new Vector3(end - perpendicularSegmentTimesThickness, 0f);
            verts[index++].Position = new Vector3(end + perpendicularSegmentTimesThickness, 0f);
            verts[index++].Position = new Vector3(start - perpendicularSegmentTimesThickness, 0f);
            verts[index++].Position = new Vector3(end + perpendicularSegmentTimesThickness, 0f);
            verts[index++].Position = new Vector3(start, 0f);

            start = end;
            num2 = -num2;
        }
        while (curLen < len);
    }

    public static void DrawBezierLightning(ref int index, ref VertexPositionColor[] verts, uint seed, Vector2 pos, Vector2 a, Vector2 b, float anchor, int steps, Color color) {
        seed += (uint) (a.GetHashCode() + b.GetHashCode());
        a += pos;
        b += pos;
        Vector2 vector = (b - a).SafeNormalize().TurnRight();
        SimpleCurve simpleCurve = new SimpleCurve(a, b, (b + a) / 2f + vector * anchor);
        int i = index + (steps + 2) * 6;
        while (i >= verts.Length) {
            Array.Resize(ref verts, verts.Length * 2);
        }
        Vector2 vector2 = simpleCurve.GetPoint(0f);
        for (int j = 0; j <= steps; j++) {
            Vector2 vector3 = simpleCurve.GetPoint(j / (float) steps);
            if (j != steps) {
                vector3 += new Vector2(PseudoRandRange(ref seed, -2f, 2f), LightningRenderer.PseudoRandRange(ref seed, -2f, 2f));
            }
            verts[index].Position = new Vector3(vector2 - vector, 0f);
            VertexPositionColor[] array = verts;
            int num = index;
            index = num + 1;
            array[num].Color = color;
            verts[index].Position = new Vector3(vector3 - vector, 0f);
            VertexPositionColor[] array2 = verts;
            num = index;
            index = num + 1;
            array2[num].Color = color;
            verts[index].Position = new Vector3(vector3, 0f);
            VertexPositionColor[] array3 = verts;
            num = index;
            index = num + 1;
            array3[num].Color = color;
            verts[index].Position = new Vector3(vector2 - vector, 0f);
            VertexPositionColor[] array4 = verts;
            num = index;
            index = num + 1;
            array4[num].Color = color;
            verts[index].Position = new Vector3(vector3, 0f);
            VertexPositionColor[] array5 = verts;
            num = index;
            index = num + 1;
            array5[num].Color = color;
            verts[index].Position = new Vector3(vector2, 0f);
            VertexPositionColor[] array6 = verts;
            num = index;
            index = num + 1;
            array6[num].Color = color;
            vector2 = vector3;
        }
    }

    public static void DrawFatLightning(uint seed, Vector2 a, Vector2 b, float size, float gap, Color color) {
        seed += (uint) (a.GetHashCode() + b.GetHashCode());
        float num = (b - a).Length();
        Vector2 vector = (b - a) / num;
        Vector2 value = vector.TurnRight();
        Vector2 vector2 = a;
        int num2 = 1;
        PseudoRandRange(ref seed, 0f, 6.28318548f);
        float num3 = 0f;
        do {
            num3 += PseudoRandRange(ref seed, 10f, 14f);
            Vector2 vector3 = a + vector * num3;
            if (num3 < num) {
                vector3 += num2 * value * PseudoRandRange(ref seed, 0f, 6f);
            } else {
                vector3 = b;
            }
            Vector2 value2 = vector3;
            if (gap > 0f) {
                value2 = vector2 + (vector3 - vector2) * (1f - gap);
                Draw.Line(vector2, vector3 + vector, color, size * 0.5f);
            }
            Draw.Line(vector2, value2 + vector, color, size);
            vector2 = vector3;
            num2 = -num2;
        }
        while (num3 < num);
    }

    public static uint PseudoRand(ref uint seed) {
        seed ^= seed << 13;
        seed ^= seed >> 17;
        return seed;
    }

    public static float PseudoRandRange(ref uint seed, float min, float max) {
        return min + (PseudoRand(ref seed) & 1023u) / 1024f * (max - min);
    }

    public class Edge {
        public Edge(Entity parent, Vector2 a, Vector2 b) {
            Parent = parent;
            Visible = true;
            A = a;
            B = b;
            Min = new Vector2(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y));
            Max = new Vector2(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));
        }

        public void Move(Vector2 move) {
            A += move;
            B += move;
            Min = new Vector2(Math.Min(A.X, B.X), Math.Min(A.Y, B.Y));
            Max = new Vector2(Math.Max(A.X, B.X), Math.Max(A.Y, B.Y));
        }

        public bool InView(ref Rectangle view) {
            return view.Left < Parent.X + Max.X && view.Right > Parent.X + Min.X && view.Top < Parent.Y + Max.Y && view.Bottom > Parent.Y + Min.Y;
        }

        public Entity Parent;

        public bool Visible;

        public Vector2 A;

        public Vector2 B;

        public Vector2 Min;

        public Vector2 Max;
    }

    public class Bolt {
        public Bolt(Color color, float scale, int width, int height) {
            Nodes = new();
            Color = color;
            Width = width;
            Height = height;
            Scale = scale;
            Routine = new Coroutine(Run(), true);
        }

        public void Update(Scene scene) {
            Routine.Update();
            Flash = Calc.Approach(Flash, 0f, Engine.DeltaTime * 2f);
        }

        private IEnumerator Run() {
            yield return Calc.Random.Range(0f, 4f);
            while (true) {
                List<Vector2> list = new List<Vector2>();
                for (int j = 0; j < 3; j++) {
                    Vector2 vector = Calc.Random.Choose(new Vector2(0f, Calc.Random.Range(8, Height - 16)), new Vector2(Calc.Random.Range(8, Width - 16), 0f), new Vector2(Width, Calc.Random.Range(8, Height - 16)), new Vector2(Calc.Random.Range(8, Width - 16), Height));
                    Vector2 item = (vector.X <= 0f || vector.X >= Width) ? new Vector2(Width - vector.X, vector.Y) : new Vector2(vector.X, Height - vector.Y);
                    list.Add(vector);
                    list.Add(item);
                }
                List<Vector2> list2 = new List<Vector2>();
                for (int k = 0; k < 3; k++) {
                    list2.Add(new Vector2(Calc.Random.Range(0.25f, 0.75f) * Width, Calc.Random.Range(0.25f, 0.75f) * Height));
                }
                Nodes.Clear();
                foreach (Vector2 vector2 in list) {
                    Nodes.Add(vector2);
                    Nodes.Add(list2.ClosestTo(vector2));
                }
                Vector2 item2 = list2[list2.Count - 1];
                foreach (Vector2 vector3 in list2) {
                    Nodes.Add(item2);
                    Nodes.Add(vector3);
                    item2 = vector3;
                }
                Flash = 1f;
                Visible = true;
                Size = 5f;
                Gap = 0f;
                Alpha = 1f;
                int num;
                for (int i = 0; i < 4; i = num + 1) {
                    Seed = (uint) Calc.Random.Next();
                    yield return 0.1f;
                    num = i;
                }
                for (int i = 0; i < 5; i = num + 1) {
                    if (!Settings.Instance.DisableFlashes) {
                        Visible = false;
                    }
                    yield return 0.05f + i * 0.02f;
                    float num2 = i / 5f;
                    Visible = true;
                    Size = (1f - num2) * 5f;
                    Gap = num2;
                    Alpha = 1f - num2;
                    Visible = true;
                    Seed = (uint) Calc.Random.Next();
                    yield return 0.025f;
                    num = i;
                }
                Visible = false;
                yield return Calc.Random.Range(4f, 8f);
            }
        }

        public void Render() {
            if (Flash > 0f && !Settings.Instance.DisableFlashes) {
                Draw.Rect(0f, 0f, Width, Height, Color.White * Flash * 0.15f * Scale);
            }
            if (Visible) {
                for (int i = 0; i < Nodes.Count; i += 2) {
                    DrawFatLightning(Seed, Nodes[i], Nodes[i + 1], Size * Scale, Gap, Color * Alpha);
                }
            }
        }

        public List<Vector2> Nodes;
        public Coroutine Routine;
        public bool Visible;
        public float Size;
        public float Gap;
        public float Alpha;
        public uint Seed;
        public float Flash;
        public Color Color;
        public float Scale;
        public int Width;
        public int Height;
    }

    public class Fill {
        public Rectangle R;

        public Fill(Rectangle rect) {
            R = rect;
        }
    }

#if TRIFILL
    public class TriFill {
        public TriangleHitbox Tri;

        public TriFill(TriangleHitbox triangle) {
            Tri = triangle;
        }
    }
#endif
}
