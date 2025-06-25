using FrostHelper.Components;

namespace FrostHelper;

[CustomEntity("FrostHelper/RainCollider")]
internal sealed class RainColliderEntity : Entity {
    public RainColliderEntity(EntityData data, Vector2 offset) : base(data.Position + offset) {
        Collider = new Hitbox(data.Width, data.Height);
        Add(new RainCollider(Collider, stationary: false) {
            MakeSplashes = data.Bool("makeSplashes", true),
            PassThroughChance = data.Float("passThroughChance", 0f),
        });
        Active = Collidable = Visible = false;
    }
}