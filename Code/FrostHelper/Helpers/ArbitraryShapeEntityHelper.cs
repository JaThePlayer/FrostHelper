using Triangulator;

namespace FrostHelper;

public static class ArbitraryShapeEntityHelper {
    public static Vector3[] GetFillFromNodes(EntityData data, Vector2 offset) {
        var nodes = data.NodesOffset(offset);
        Vector2[] input = new Vector2[nodes.Length + 1];
        input[0] = data.Position + offset;
        for (int i = 1; i < input.Length; i++) {
            input[i] = nodes[i - 1];
        }

        Triangulator.Triangulator.Triangulate(input, WindingOrder.CounterClockwise, out var verts, out var indices);

        var fill = new Vector3[indices.Length];
        for (int i = 0; i < indices.Length; i++) {
            fill[i] = new Vector3(verts[indices[i]], 0f);
        }

        return fill;
    }
}
