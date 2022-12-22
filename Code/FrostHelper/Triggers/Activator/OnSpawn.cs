﻿namespace FrostHelper.Triggers.Activator;

[CustomEntity("FrostHelper/OnSpawnActivator")]
internal class OnSpawnActivator : BaseActivator {
    public OnSpawnActivator(EntityData data, Vector2 offset) : base(data, offset) {
        Collidable = false;
    }

    public override void Awake(Scene scene) {
        base.Awake(scene);

        ActivateAll(scene.Tracker.GetEntity<Player>());
    }

    public override void OnEnter(Player player) {
        base.OnEnter(player);

        ActivateAll(player);
    }
}