namespace FrostHelper {
    [Celeste.Mod.Entities.CustomEntity("FrostHelper/TemporaryKeyDoor")]
    public class LockBlock : Solid {
        public LockBlock(Vector2 position, EntityID id, bool stepMusicProgress, string spriteName, string unlock_sfx) : base(position, 32f, 32f, false) {
            ID = id;
            DisableLightsInside = false;
            this.stepMusicProgress = stepMusicProgress;
            Add(new PlayerCollider(new Action<Player>(OnPlayer), new Circle(60f, 16f, 16f), null));
            Add(sprite = GFX.SpriteBank.Create("lockdoor_" + spriteName));
            sprite.Play("idle", false, false);
            sprite.Position = new Vector2(Width / 2f, Height / 2f);

            if (string.IsNullOrWhiteSpace(unlock_sfx)) {
                unlockSfxName = "event:/game/03_resort/key_unlock";
                if (spriteName == "temple_a") {
                    unlockSfxName = "event:/game/05_mirror_temple/key_unlock_light";
                } else if (spriteName == "temple_b") {
                    unlockSfxName = "event:/game/05_mirror_temple/key_unlock_dark";
                }
            } else {
                unlockSfxName = SFX.EventnameByHandle(unlock_sfx);
            }
        }

        public LockBlock(EntityData data, Vector2 offset, EntityID id) : this(data.Position + offset, id, data.Bool("stepMusicProgress", false), data.Attr("sprite", "wood"), data.Attr("unlock_sfx", null)) {
        }

        public void Appear() {
            Visible = true;
            sprite.Play("appear", false, false);
            Add(Alarm.Create(Alarm.AlarmMode.Oneshot, delegate {
                Level level = (Scene as Level)!;
                if (!CollideCheck<Solid>(Position - Vector2.UnitX)) {
                    level.Particles.Emit(P_Appear, 16, Position + new Vector2(3f, 16f), new Vector2(2f, 10f), 3.14159274f);
                    level.Particles.Emit(P_Appear, 16, Position + new Vector2(29f, 16f), new Vector2(2f, 10f), 0f);
                }
                level.Shake(0.3f);
            }, 0.25f, true));
        }

        private void OnPlayer(Player player) {
            if (!opening) {
                foreach (Follower follower in player.Leader.Followers) {
                    if (follower.Entity is Key && !(follower.Entity as Key)!.StartedUsing) {
                        TryOpen(player, follower);
                        break;
                    }
                }
            }
        }

        private void TryOpen(Player player, Follower fol) {
            Collidable = false;
            if (!Scene.CollideCheck<Solid>(player.Center, Center)) {
                opening = true;
                (fol.Entity as Key)!.StartedUsing = true;
                Add(new Coroutine(UnlockRoutine(fol), true));
            }
            Collidable = true;
        }

        private IEnumerator UnlockRoutine(Follower fol) {
            SoundEmitter emitter = SoundEmitter.Play(unlockSfxName, this, null);
            emitter.Source.DisposeOnTransition = true;
            Level level = SceneAs<Level>();
            Key key = (fol.Entity as Key)!;
            Add(new Coroutine(key.UseRoutine(Center + new Vector2(0f, 2f)), true));
            yield return 1.2f;
            UnlockingRegistered = true;

            if (stepMusicProgress) {
                AudioTrackState music = level.Session.Audio.Music;
                int progress = music.Progress;
                music.Progress = progress + 1;
                level.Session.Audio.Apply(false);
            }

            key.RegisterUsed();
            while (key.Turning) {
                yield return null;
            }
            Tag |= Tags.TransitionUpdate;
            Collidable = false;
            emitter.Source.DisposeOnTransition = false;
            yield return sprite.PlayRoutine("open", false);
            level.Shake(0.3f);
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
            yield return sprite.PlayRoutine("burst", false);
            RemoveSelf();
            yield break;
        }

        public static ParticleType P_Appear;

        public EntityID ID;

        public bool UnlockingRegistered;

        private Sprite sprite;

        private bool opening;

        private bool stepMusicProgress;

        private string unlockSfxName;
    }
}
