namespace FrostHelper.Backdrops;

internal sealed class ShaderWrapperBackdrop : ShaderFolder {
    private readonly string WrappedTag;
    private bool _consumed;
    
    public ShaderWrapperBackdrop(BinaryPacker.Element child) : base(child) {
        WrappedTag = child.Attr("wrappedTag", "");
    }

    public IEnumerable<Backdrop> GetAffectedBackdrops() => Renderer.Backdrops.Where(b => b != this && b.Tags.Contains(WrappedTag));

    public override void Update(Scene scene) {
        base.Update(scene);

        if (_consumed)
            return;
        
        var affected = GetAffectedBackdrops().ToList();
        if (affected.Count <= 0)
            return;
        
        Wrap(scene, affected);
        _consumed = true;
    }

    private void Wrap(Scene scene, List<Backdrop> affected) {
        Inner.AddRange(affected);
        scene.OnEndOfFrame += () => {
            foreach (var b in affected) {
                Renderer.Backdrops.Remove(b);
            }
        };
    }
}
