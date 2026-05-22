using FrostHelper.Components;
using FrostHelper.Helpers;

namespace FrostHelper.Entities;

[CustomEntity("FrostHelper/BloomBlocker")]
internal sealed class ArbitraryBloomBlocker : Entity {
    public ArbitraryBloomBlocker(EntityData data, Vector2 offset) : base(data.Position + offset) {
        var verts = ArbitraryShapeEntityHelper.GetFillVertsFromNodes(data, offset, Color.White * data.Float("alpha", 1f));
        var condition = data.GetCondition("flag");

        Add(new CustomBloomBlocker {
            OnRender = () => {
                if (condition.Check(Scene.ToLevel().Session)) {
                    CustomBloomBlocker.DrawVertices(verts, SceneAs<Level>(), parallaxOffset: default);
                }
            }
        });
    }
}
