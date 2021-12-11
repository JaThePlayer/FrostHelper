using Celeste.Mod.Entities;
using FrostHelper.Colliders;
using Triangulator;

namespace FrostHelper.Entities {
    [CustomEntity("FrostHelper/ArbitraryShapeFireBarrier")]
    public class ArbitraryShapeFireBarrier : Entity {
        public Vector3[] Vertices;
        public bool IsIce;
        //public ThunderRenderer.Edge[] Edges;

        public ArbitraryShapeFireBarrier(EntityData data, Vector2 offset) : base(data.Position + offset) {
            Depth = -8500;
            var nodes = data.NodesOffset(offset);
            Vector2[] input = new Vector2[nodes.Length + 1];
            input[0] = Position;
            for (int i = 1; i < input.Length; i++) {
                input[i] = nodes[i - 1];
            }

            Triangulator.Triangulator.Triangulate(input, WindingOrder.CounterClockwise, out Vector2[] verts, out int[] indices);

            Vertices = new Vector3[indices.Length];
            for (int i = 0; i < indices.Length; i++) {
                Vertices[i] = new Vector3(verts[indices[i]], 0f);
            }
            LavaShape Lava = new LavaShape(input, Vertices, IsIce ? 2 : 4);
            Lava.SurfaceColor = ColorHelper.GetColor(data.Attr("surfaceColor", "ff8933"));
            Lava.EdgeColor = ColorHelper.GetColor(data.Attr("edgeColor", "f25e29"));
            Lava.CenterColor = ColorHelper.GetColor(data.Attr("centerColor", "d01c01"));
            Lava.SmallWaveAmplitude = 2f;
            Lava.BigWaveAmplitude = 1f;
            Lava.CurveAmplitude = 1f;
            if (IsIce) {
                Lava.UpdateMultiplier = 0f;
                Lava.Spikey = 3f;
                Lava.SmallWaveAmplitude = 1f;
            }
            Add(Lava);
            /*
            Edges = new ThunderRenderer.Edge[nodes.Length + 1];
            for (int i = 1; i < nodes.Length; i++)
            {
                Edges[i] = new ThunderRenderer.Edge(this, nodes[i - 1] - Position, nodes[i] - Position);
            }
            Edges[0] = new ThunderRenderer.Edge(this, Vector2.Zero, nodes[0] - Position);
            Edges[Edges.Length - 1] = new ThunderRenderer.Edge(this, nodes[nodes.Length - 1] - Position, Vector2.Zero);*/

            Collider = new ShapeHitbox(input);
            Add(new PlayerCollider(new Action<Player>(OnPlayer), Collider, null));
        }

        public void OnPlayer(Player player) {
            if (!player.Dead)
                player.Die(Vector2.Normalize(-player.Speed));
        }
    }
}
