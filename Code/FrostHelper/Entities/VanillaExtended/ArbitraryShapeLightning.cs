using FrostHelper.Colliders;
using Triangulator;

namespace FrostHelper;

[CustomEntity("FrostHelper/ArbitraryShapeLightning")]
public class ArbitraryShapeLightning : Entity {
    public CustomLightningRenderer.ArbitraryFill? Vertices;
    public CustomLightningRenderer.Edge[] Edges;
    public bool Fill;

    public ArbitraryShapeLightning(EntityData data, Vector2 offset) : base(data.Position + offset) {
        var nodes = data.NodesOffset(offset);

        Fill = data.Bool("fill", true);

        if (Fill) {
            Vertices = new(ArbitraryShapeEntityHelper.GetFillFromNodes(data, offset));
        }

        Edges = new CustomLightningRenderer.Edge[nodes.Length + (Fill ? 1 : 0)];
        for (int i = 1; i < nodes.Length; i++) {
            Edges[i] = new CustomLightningRenderer.Edge(this, nodes[i - 1] - Position, nodes[i] - Position);
        }
        Edges[0] = new CustomLightningRenderer.Edge(this, Vector2.Zero, nodes[0] - Position);
        if (Fill)
            Edges[^1] = new CustomLightningRenderer.Edge(this, nodes[^1] - Position, Vector2.Zero);

        Collider = new ShapeHitbox(data.GetNodesWithOffsetWithPositionPrepended(offset)) { Fill = Fill };
        Add(new PlayerCollider(OnPlayer, Collider, null));
    }

    public void OnPlayer(Player player) {
        if (!player.Dead)
            player.Die(Vector2.UnitX);
    }

    public override void Added(Scene scene) {
        base.Added(scene);
        var renderer = CustomLightningRenderer.AddToSceneIfNeeded(scene);

        foreach (var item in Edges)
            renderer.Add(item);
        
        if (Fill)
            renderer.Add(Vertices!);
    }

    public override void Removed(Scene scene) {
        base.Removed(scene);
        var renderer = CustomLightningRenderer.AddToSceneIfNeeded(scene);
        foreach (var item in Edges) {
            renderer.Remove(item);
        }
        if (Vertices is not null)
            renderer.Remove(Vertices);
    }
    /*
    public override void DebugRender(Camera camera) {
        base.DebugRender(camera);
        if (Vertices is null)
            return;
        
        Vector3 vert1, vert2;
        for (int i = 0; i < Vertices.Length - 1; i++) {
            vert1 = Vertices[i];
            vert2 = Vertices[i + 1];
            Draw.Line(new Vector2(vert1.X, vert1.Y), new Vector2(vert2.X, vert2.Y), Color.Pink);
        }
        vert1 = Vertices[0];
        vert2 = Vertices[Vertices.Length - 1];
        Draw.Line(new Vector2(vert1.X, vert1.Y), new Vector2(vert2.X, vert2.Y), Color.Pink);
    }
    */
}
