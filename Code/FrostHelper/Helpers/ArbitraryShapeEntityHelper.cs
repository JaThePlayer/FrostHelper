using Triangulator;

namespace FrostHelper;

public static class ArbitraryShapeEntityHelper {
    public static WindingOrder? GetWindingOrder(this EntityData data, string key) {
        if (!data.Values.TryGetValue(key, out var windingOrder)) {
            return null;
        }

        return windingOrder switch {
            nameof(WindingOrder.Clockwise) => WindingOrder.Clockwise,
            nameof(WindingOrder.CounterClockwise) => WindingOrder.CounterClockwise,
            _ => null
        };
    }

    public static Vector3[] GetFillFromNodes(EntityData data, Vector2 offset, string cacheKey = "__cachedFillV3") {
        if (data.Values.TryGetValue(cacheKey, out var cached)) {
            return (Vector3[]) cached;
        }

        var nodes = data.NodesOffset(offset);
        Vector2[] input = new Vector2[nodes.Length + 1];
        input[0] = data.Position + offset;
        for (int i = 1; i < input.Length; i++) {
            input[i] = nodes[i - 1];
        }

        Triangulator.Triangulator.Triangulate(input, WindingOrder.Clockwise, GetWindingOrder(data, "windingOrder"), out var verts, out var indices);

        var fill = new Vector3[indices.Length];
        for (int i = 0; i < indices.Length; i++) {
            fill[i] = new Vector3(verts[indices[i]], 0f);
        }

        data.Values[cacheKey] = fill;

        return fill;
    }

    public static VertexPositionColor[] GetFillVertsFromNodes(EntityData data, Vector2 offset, Color color, string cacheKey = "__cachedFillVPC") {
        if (data.Values.TryGetValue(cacheKey, out var cached)) {
            return (VertexPositionColor[]) cached;
        }

        var nodes = data.NodesOffset(offset);
        Vector2[] input = new Vector2[nodes.Length + 1];
        input[0] = data.Position + offset;
        for (int i = 1; i < input.Length; i++) {
            input[i] = nodes[i - 1];
        }

        Triangulator.Triangulator.Triangulate(input, WindingOrder.Clockwise, GetWindingOrder(data, "windingOrder"), out var verts, out var indices);

        var fill = new VertexPositionColor[indices.Length];
        for (int i = 0; i < indices.Length; i++) {
            ref var f = ref fill[i];

            f.Position = new(verts[indices[i]].Floor(), 0f);
            f.Color = color;
        }

        data.Values[cacheKey] = fill;

        return fill;
    }

    public static void DrawVertices<T>(Matrix matrix, T[] vertices, int vertexCount, Effect? effect = null, BlendState? blendState = null, RasterizerState? rasterizerState = null) where T : struct, IVertexType {
        effect ??= GFX.FxPrimitive;
        blendState ??= BlendState.AlphaBlend;
        rasterizerState ??= RasterizerState.CullNone;

        Vector2 vector = new Vector2(Engine.Graphics.GraphicsDevice.Viewport.Width, Engine.Graphics.GraphicsDevice.Viewport.Height);
        matrix *= Matrix.CreateScale(1f / vector.X * 2f, -(1f / vector.Y) * 2f, 1f);
        matrix *= Matrix.CreateTranslation(-1f, 1f, 0f);

        Engine.Instance.GraphicsDevice.RasterizerState = rasterizerState;
        Engine.Instance.GraphicsDevice.BlendState = blendState;

        effect.Parameters["World"].SetValue(matrix);
        foreach (EffectPass effectPass in effect.CurrentTechnique.Passes) {
            effectPass.Apply();
            Engine.Instance.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, vertexCount / 3);
        }
    }

    public static readonly RasterizerState Wireframe = new RasterizerState {
        FillMode = FillMode.WireFrame,
    };

    public static void DrawDebugWireframe(VertexPositionColor[] vertices, Color color) {
        VertexPositionColor vert1, vert2;
        for (int i = 0; i < vertices.Length - 1; i++) {
            vert1 = vertices[i];
            vert2 = vertices[i + 1];
            Draw.Line(new Vector2(vert1.Position.X, vert1.Position.Y), new Vector2(vert2.Position.X, vert2.Position.Y), color);
        }
        vert1 = vertices[0];
        vert2 = vertices[^1];
        Draw.Line(new Vector2(vert1.Position.X, vert1.Position.Y), new Vector2(vert2.Position.X, vert2.Position.Y), color);
    }

    public static void DrawDebugWireframe<T>(T[] vertices, Color color, Func<T, Vector2> tToVec2) {
        Vector2 vert1, vert2;
        for (int i = 0; i < vertices.Length - 1; i++) {
            vert1 = tToVec2(vertices[i]);
            vert2 = tToVec2(vertices[i + 1]);
            Draw.Line(vert1, vert2, color);
        }
        vert1 = tToVec2(vertices[0]);
        vert2 = tToVec2(vertices[^1]);
        Draw.Line(vert1, vert2, color);
    }
}
