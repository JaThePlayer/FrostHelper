namespace FrostHelper.Entities.Noperture;

[CustomEntity("noperture/deadlyLazer")]
class DeadlyLazer : Entity {
    public DeadlyLazer(EntityData data, Vector2 offset) : base(data.Position + offset) {
        Collider = new Hitbox(4f, data.Height + 1f, 2f);
        Add(new PlayerCollider((player) => { player.Die(Vector2.Zero); }));
    }

    public override void Render() {
        base.Render();
        Draw.Rect(new Rectangle((int) X + 3, (int) Y, 2, (int) Height + 1), Color.Red);
        Draw.HollowRect(Collider, Color.Red * 0.33f);

        SceneAs<Level>().Particles.Emit(BadelineOldsite.P_Vanish, 1, new Vector2(X + 3f, Y + Height), Vector2.UnitY * 3, Color.Red);
    }
}
