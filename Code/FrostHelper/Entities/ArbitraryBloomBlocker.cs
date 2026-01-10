using FrostHelper.Components;

namespace FrostHelper.Entities;

[CustomEntity("FrostHelper/BloomBlocker")]
internal sealed class ArbitraryBloomBlocker : Entity {
    public ArbitraryBloomBlocker(EntityData data, Vector2 offset) : base(data.Position + offset) {
        var verts = ArbitraryShapeEntityHelper.GetFillVertsFromNodes(data, offset, Color.White * data.Float("alpha", 1f));

        Add(new CustomBloomBlocker() {
            OnRender = () => CustomBloomBlocker.DrawVertices(verts, SceneAs<Level>(), parallaxOffset: default),
        });
    }
}
