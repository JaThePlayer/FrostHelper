using System;
using System.Collections;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste;

namespace FrostHelper
{
    [Celeste.Mod.Entities.CustomEntity("FrostHelper/TemporaryKeyDoor")]
    public class LockBlock : Solid
    {
        public LockBlock(Vector2 position, EntityID id, bool stepMusicProgress, string spriteName, string unlock_sfx) : base(position, 32f, 32f, false)
        {
            this.ID = id;
            this.DisableLightsInside = false;
            this.stepMusicProgress = stepMusicProgress;
            base.Add(new PlayerCollider(new Action<Player>(this.OnPlayer), new Circle(60f, 16f, 16f), null));
            base.Add(this.sprite = GFX.SpriteBank.Create("lockdoor_" + spriteName));
            this.sprite.Play("idle", false, false);
            this.sprite.Position = new Vector2(base.Width / 2f, base.Height / 2f);
            bool flag = string.IsNullOrWhiteSpace(unlock_sfx);
            if (flag)
            {
                this.unlockSfxName = "event:/game/03_resort/key_unlock";
                bool flag2 = spriteName == "temple_a";
                if (flag2)
                {
                    this.unlockSfxName = "event:/game/05_mirror_temple/key_unlock_light";
                }
                else
                {
                    bool flag3 = spriteName == "temple_b";
                    if (flag3)
                    {
                        this.unlockSfxName = "event:/game/05_mirror_temple/key_unlock_dark";
                    }
                }
            }
            else
            {
                this.unlockSfxName = SFX.EventnameByHandle(unlock_sfx);
            }
        }

        public LockBlock(EntityData data, Vector2 offset, EntityID id) : this(data.Position + offset, id, data.Bool("stepMusicProgress", false), data.Attr("sprite", "wood"), data.Attr("unlock_sfx", null))
        {
        }

        public void Appear()
        {
            this.Visible = true;
            this.sprite.Play("appear", false, false);
            base.Add(Alarm.Create(Alarm.AlarmMode.Oneshot, delegate
            {
                Level level = base.Scene as Level;
                bool flag = !base.CollideCheck<Solid>(this.Position - Vector2.UnitX);
                if (flag)
                {
                    level.Particles.Emit(LockBlock.P_Appear, 16, this.Position + new Vector2(3f, 16f), new Vector2(2f, 10f), 3.14159274f);
                    level.Particles.Emit(LockBlock.P_Appear, 16, this.Position + new Vector2(29f, 16f), new Vector2(2f, 10f), 0f);
                }
                level.Shake(0.3f);
            }, 0.25f, true));
        }

        private void OnPlayer(Player player)
        {
            bool flag = !this.opening;
            if (flag)
            {
                foreach (Follower follower in player.Leader.Followers)
                {
                    bool flag2 = follower.Entity is Key && !(follower.Entity as Key).StartedUsing;
                    if (flag2)
                    {
                        this.TryOpen(player, follower);
                        break;
                    }
                }
            }
        }

        private void TryOpen(Player player, Follower fol)
        {
            this.Collidable = false;
            bool flag = !base.Scene.CollideCheck<Solid>(player.Center, base.Center);
            if (flag)
            {
                this.opening = true;
                (fol.Entity as Key).StartedUsing = true;
                base.Add(new Coroutine(this.UnlockRoutine(fol), true));
            }
            this.Collidable = true;
        }

        private IEnumerator UnlockRoutine(Follower fol)
        {
            SoundEmitter emitter = SoundEmitter.Play(this.unlockSfxName, this, null);
            emitter.Source.DisposeOnTransition = true;
            Level level = this.SceneAs<Level>();
            Key key = fol.Entity as Key;
            this.Add(new Coroutine(key.UseRoutine(this.Center + new Vector2(0f, 2f)), true));
            yield return 1.2f;
            this.UnlockingRegistered = true;
            bool flag = this.stepMusicProgress;
            if (flag)
            {
                AudioTrackState music = level.Session.Audio.Music;
                int progress = music.Progress;
                music.Progress = progress + 1;
                level.Session.Audio.Apply(false);
            }
            //level.Session.DoNotLoad.Add(this.ID);
            key.RegisterUsed();
            while (key.Turning)
            {
                yield return null;
            }
            this.Tag |= Tags.TransitionUpdate;
            this.Collidable = false;
            emitter.Source.DisposeOnTransition = false;
            yield return this.sprite.PlayRoutine("open", false);
            level.Shake(0.3f);
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
            yield return this.sprite.PlayRoutine("burst", false);
            this.RemoveSelf();
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
