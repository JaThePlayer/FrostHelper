namespace FrostHelper.Entities;

[Tracked]
internal sealed class CustomDustEdge : Component {
    public CustomDustEdge(CustomDustGraphic graphic, Func<bool> onRenderDust) : base(false, true) {
        RenderDust = onRenderDust;
        Graphic = graphic;
    }

    public readonly Func<bool> RenderDust;
    public readonly CustomDustGraphic Graphic;

    public override void EntityRemoved(Scene scene) {
        Untrack(scene);

        base.EntityRemoved(scene);
    }

    public override void SceneEnd(Scene scene) {
        Untrack(scene);

        base.SceneEnd(scene);
    }

    void Untrack(Scene scene) {
        CustomDustGraphic.Untrack(scene.Tracker.GetEntity<DustEdges>(), this);
    }
}