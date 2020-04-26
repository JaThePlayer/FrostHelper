using System;
using System.Collections;
using System.Collections.Generic;
using Celeste;
using Celeste.Mod;
using Microsoft.Xna.Framework;
using Monocle;


namespace FrostHelper {
    public class KeyIce : Key
    {
        private bool IsFirstIceKey
        {
            get
            {
                for (int i = this.follower.FollowIndex - 1; i > -1; i--)
                {
                    bool flag = this.follower.Leader.Followers[i].Entity is KeyIce;
                    if (flag)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        private Alarm alarm;
        private Tween tween;
        public new bool Turning { get; private set; }

        public KeyIce(EntityData data, Vector2 offset, EntityID id, Vector2[] nodes) : base(data.Position + offset, id, nodes)
        {
            sprite = Get<Monocle.Sprite>();
            this.follower = Get<Follower>();
            FrostModule.SpriteBank.CreateOn(sprite, "keyice");
            Follower follower = this.follower;
            follower.OnLoseLeader = (Action)Delegate.Combine(follower.OnLoseLeader, new Action(Dissolve));
            this.follower.PersistentFollow = true; // was false
            Add(new DashListener
            {
                OnDash = new Action<Vector2>(OnDash)
            });
            Add(new TransitionListener
            {
                OnOut = delegate (float f)
                {
                    this.StartedUsing = false;
                    if (!this.IsUsed)
                    {
                        if (this.tween != null)
                        {
                            this.tween.RemoveSelf();
                            this.tween = null;
                        }
                        if (this.alarm != null)
                        {
                            this.alarm.RemoveSelf();
                            this.alarm = null;
                        }
                        this.Turning = false;
                        this.Visible = true;
                        this.sprite.Visible = true;
                        this.sprite.Rate = 1f;
                        this.sprite.Scale = Vector2.One;
                        this.sprite.Play("idle", false, false);
                        this.sprite.Rotation = 0f;
                        this.follower.MoveTowardsLeader = true;
                    }
                }
            });
        }

        private void OnDash(Vector2 dir)
        {
            bool flag1 = this.follower.Leader != null;
            if (flag1)
            {
                this.Dissolve();
            }

        }

        public override void Added(Monocle.Scene scene)
        {
            base.Added(scene);
            Level level = scene as Level;
            bool flag = level == null;
            if (!flag)
            {
                this.start = this.Position;
                this.startLevel = level.Session.Level;
            }
        }

        public override void Update()
        {
            Level level = base.Scene as Level;
            Session session = (level != null) ? level.Session : null;
            bool flag = this.IsUsed && !this.wasUsed;
            if (flag)
            {
                session.DoNotLoad.Add(this.ID);
                this.wasUsed = true;
            }
            bool flag2 = !this.dissolved && !this.IsUsed && !base.Turning;
            if (flag2)
            {
                bool flag3 = session != null && session.Keys.Contains(this.ID);
                if (flag3)
                {
                    session.DoNotLoad.Remove(this.ID);
                    session.Keys.Remove(this.ID);
                    session.UpdateLevelStartDashes();
                }
                int followIndex = this.follower.FollowIndex;
                bool flag4 = this.follower.Leader != null && this.follower.DelayTimer <= 0f && this.IsFirstIceKey;
            }
            base.Update();
        }

        public void Dissolve()
        {
            bool flag = this.dissolved || this.IsUsed || base.Turning;
            if (!flag)
            {
                this.dissolved = true;
                bool flag2 = this.follower.Leader != null;
                if (flag2)
                {
                    Player player = this.follower.Leader.Entity as Player;
                    player.StrawberryCollectResetTimer = 2.5f;
                    this.follower.Leader.LoseFollower(this.follower);
                }
                base.Add(new Monocle.Coroutine(this.DissolveRoutine(), true));
            }
        }

        private IEnumerator DissolveRoutine()
        {
            Level level = base.Scene as Level;
            Session session = level.Session;
            session.DoNotLoad.Remove(this.ID);
            session.Keys.Remove(this.ID);
            session.UpdateLevelStartDashes();
            Audio.Play("event:/game/general/seed_poof", this.Position);
            this.Collidable = false;
            this.sprite.Scale = Vector2.One * 0.5f;
            yield return 0.05f;
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
            int num;
            for (int i = 0; i < 6; i = num + 1)
            {
                float dir = Monocle.Calc.Random.NextFloat(6.28318548f);
                level.ParticlesFG.Emit(StrawberrySeed.P_Burst, 1, this.Position + Monocle.Calc.AngleToVector(dir, 4f), Vector2.Zero, dir);
                num = i;
            }
            this.sprite.Scale = Vector2.Zero;
            this.Visible = false;
            bool flag = level.Session.Level != this.startLevel;
            if (flag)
            {
                base.RemoveSelf();
                yield break;
            }
            yield return 0.3f;
            this.dissolved = false;
            Audio.Play("event:/game/general/seed_reappear", this.Position);
            this.Position = this.start;
            this.sprite.Scale = Vector2.One;
            this.Visible = true;
            this.Collidable = true;
            level.Displacement.AddBurst(this.Position, 0.2f, 8f, 28f, 0.2f, null, null);
            yield break;
        }

        private Sprite sprite;

        private Follower follower;

        private Vector2 start;

        private string startLevel;

        private bool dissolved;

        private bool wasUsed;
    }
}


