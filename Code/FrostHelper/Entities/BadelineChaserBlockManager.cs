namespace FrostHelper;

public class BadelineChaserBlockManager : Entity {
    public BadelineChaserBlockManager() {
        Depth = Depths.Top;
    }

    bool lastState;

    public override void Update() {
        bool state = false;
        Player player = Scene.Tracker.GetEntity<Player>();

        foreach (BadelineChaserBlockActivator activator in Scene.Tracker.SafeGetEntities<BadelineChaserBlockActivator>()) {
            activator.DoneCollisionChecks = true;
            if (!state) {
                if (activator.Solid && activator.HasBaddyRider()) {
                    state = true;
                    break;
                }

                foreach (BadelineOldsite baddy in Scene.Tracker.SafeGetEntities<BadelineOldsite>()) {
                    if (activator.Solid) {
                        // on the side
                        if (baddy.CollideCheck(activator, baddy.Position + (Vector2.UnitX * 2)) || baddy.CollideCheck(activator, baddy.Position + (Vector2.UnitX * -2))) {
                            // now let's see if badeline is grabbing

                            //var data = DynamicData.For(baddy as BadelineOldsite);
                            if (player != null && !player.Dead && baddy.following && player.GetChasePosition(Scene.TimeActive, baddy.followBehindTime + baddy.followBehindIndexDelay, out var chaserState)) {
                                string anim = chaserState.Animation.ToLower();
                                if (anim.Contains("climb") || anim == "dangling" || anim == "wallslide") {
                                    state = true;
                                    break;
                                }
                            }
                        }
                    } else {
                        activator.Collidable = true;
                        if (baddy.CollideCheck(activator)) {
                            state = true;
                            activator.Collidable = false;
                            break;
                        }
                        activator.Collidable = false;
                    }
                }
            }
        }

        foreach (BadelineChaserBlock block in Scene.Tracker.SafeGetEntities<BadelineChaserBlock>()) {
            block.SetState(state);
        }

        if (lastState != state) {
            var audio = Audio.Play(state ? BadelineChaserBlockActivator.ActivateSfx : BadelineChaserBlockActivator.DeactivateSfx);
            audio.getVolume(out float volume, out _);
            audio.setVolume(volume * 60f);
        }
        lastState = state;
    }
}
