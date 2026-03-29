namespace FrostHelper.Materials;

[Tracked(inherited: true)]
internal abstract class MaterialSource(EntityData data, Vector2 offset) : Entity(data.Position + offset) {
    public string Name { get; } = data.Attr("name");
    
    public abstract IMaterial CreateMaterial(MaterialManager manager);

    public override void Added(Scene scene) {
        base.Added(scene);

        if (!string.IsNullOrWhiteSpace(Name)) {
            var manager = MaterialManager.GetFor(scene);
            manager.Register(Name, () => CreateMaterial(manager));
        }
    }
}