using Celeste.Mod.CommunalHelper.Utils;
using MonoMod.ModInterop;

namespace FrostHelper.ModIntegration;

// ReSharper disable InconsistentNaming 
// ReSharper disable UnassignedField.Global
#pragma warning disable CS0649 // Field is never assigned to
#pragma warning disable CA2211

[ModImportName("CommunalHelper.DashStates")]
internal static class CommunalHelperIntegration {
    public static bool LoadIfNeeded()
    {
        if (Loaded)
            return true;

        typeof(CommunalHelperIntegration).ModInterop();

        Loaded = true;

        return GetDreamTunnelDashState is {};
    }

    private static bool Loaded { get; set; }

    public static bool Available => LoadIfNeeded() && GetDreamTunnelDashState is { };

    // int GetDreamTunnelDashState()
    public static Func<int>? GetDreamTunnelDashState;
}

internal static class CommunalHelperShapes {
    public static Mesh<VertexPCTN> HalfRing(float height, float thickness, Color color)
    {
        const int circSub = 16;
        const int ringSub = 4;
        const int len = circSub * ringSub;

        thickness /= 2.0f;

        Vector3[] vertices = new Vector3[(circSub + 1) * ringSub];
        int[] indices = new int[6 * len];

        static Vector3 Circle(Vector3 x, Vector3 y, float angle)
            => (float) Math.Cos(angle) * x + (float) Math.Sin(angle) * y;

        int index = 0;
        for (int i = 0; i <= circSub; i++)
        {
            Vector3 axis = Circle(Vector3.UnitZ * 16, Vector3.UnitY * height / 2f, MathHelper.Pi * i / circSub - MathHelper.PiOver2);
            for (int j = 0; j < ringSub; j++)
            {
                vertices[i * ringSub + j] = axis + Circle(axis.SafeNormalize(), Vector3.UnitX, MathHelper.TwoPi * j / ringSub) * thickness;

                if (i == circSub)
                    continue;

                int a = i * ringSub + ((j + 0) % ringSub) + 0 * ringSub;
                int b = i * ringSub + ((j + 1) % ringSub) + 0 * ringSub;
                int c = i * ringSub + ((j + 0) % ringSub) + 1 * ringSub;
                int d = i * ringSub + ((j + 1) % ringSub) + 1 * ringSub;

                indices[index++] = a;
                indices[index++] = b;
                indices[index++] = c;
                indices[index++] = b;
                indices[index++] = d;
                indices[index++] = c;
            }
        }

        return BuildMesh(vertices, indices, color, 0.1f);
    }
    
    private static Mesh<VertexPCTN> BuildMesh(Vector3[] vertices, int[] indices, Color color, float rainbow = 0f, float scale = 1f)
    {
        Mesh<VertexPCTN> mesh = new();
        for (int i = 0; i < indices.Length; i += 3)
        {
            Vector3 v1 = vertices[indices[i]];
            Vector3 v2 = vertices[indices[i + 1]];
            Vector3 v3 = vertices[indices[i + 2]];

            Vector3 normal = Vector3.Normalize(Vector3.Cross(v3 - v1, v2 - v1));
            Vector3 n = (normal + Vector3.One) / 2f;
            Color c = Color.Lerp(color, new(n.X, n.Y, n.Z, 1.0f), rainbow);

            mesh.AddTriangle(i, i + 2, i + 1);
            mesh.AddVertices(
                new VertexPCTN(v1 * scale, c, Vector2.Zero, normal),
                new VertexPCTN(v2 * scale, c, Vector2.Zero, normal),
                new VertexPCTN(v3 * scale, c, Vector2.Zero, normal)
            );
        }

        mesh.Bake();
        return mesh;
    }
}