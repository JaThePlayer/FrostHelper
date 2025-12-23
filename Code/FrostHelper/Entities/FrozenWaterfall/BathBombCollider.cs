namespace FrostHelper.Entities.FrozenWaterfall;

[Tracked]
internal sealed class BathBombCollider(Collider? overrideCollider = null) : Component(true, false) {

    public Collider Collider => overrideCollider ?? Entity?.Collider ?? throw new Exception("BathBombCollider does not have a Collider!");

    public required Func<BathBomb, bool> CanCollideWith { get; init; }
    
    public required Action<BathBomb> OnCollide { get; init; }
}