namespace FrostHelper.Triggers.Activator;

[CustomEntity("FrostHelper/ElseActivator")]
internal sealed class ElseActivator : BaseActivator, IIfActivator {
    public ElseActivator(EntityData data, Vector2 offset) : base(data, offset) {
        Active = false;
        Visible = false;
        Collidable = false;
    }

    public override void OnEnter(Player player) {
        ActivateAll(player);
    }

    public bool IsElse => true;
}