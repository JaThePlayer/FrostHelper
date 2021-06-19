using System;
using System.Collections;
using Celeste;
using Microsoft.Xna.Framework;
using Monocle;


namespace FrostHelper
{
    public class KeyIce : Key
    {
        private bool IsFirstIceKey
        {
            get
            {
                for (int i = follower.FollowIndex - 1; i > -1; i--)
                {
                    bool flag = follower.Leader.Followers[i].Entity is KeyIce;
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
                    StartedUsing = false;
                    if (!IsUsed)
                    {
                        if (tween != null)
                        {
                            tween.RemoveSelf();
                            tween = null;
                        }
                        if (alarm != null)
                        {
                            alarm.RemoveSelf();
                            alarm = null;
                        }
                        Turning = false;
                        Visible = true;
                        sprite.Visible = true;
                        sprite.Rate = 1f;
                        sprite.Scale = Vector2.One;
                        sprite.Play("idle", false, false);
                        sprite.Rotation = 0f;
                        this.follower.MoveTowardsLeader = true;
                    }
                }
            });
        }

        private void OnDash(Vector2 dir)
        {
            bool flag1 = follower.Leader != null;
            if (flag1)
            {
                Dissolve();
            }

        }

        public override void Added(Monocle.Scene scene)
        {
            base.Added(scene);
            Level level = scene as Level;
            bool flag = level == null;
            if (!flag)
            {
                start = Position;
                startLevel = level.Session.Level;
            }
        }

        public override void Update()
        {
            Level level = Scene as Level;
            Session session = (level != null) ? level.Session : null;
            bool flag = IsUsed && !wasUsed;
            if (flag)
            {
                session.DoNotLoad.Add(ID);
                wasUsed = true;
            }
            bool flag2 = !dissolved && !IsUsed && !base.Turning;
            if (flag2)
            {
                bool flag3 = session != null && session.Keys.Contains(ID);
                if (flag3)
                {
                    session.DoNotLoad.Remove(ID);
                    session.Keys.Remove(ID);
                    session.UpdateLevelStartDashes();
                }
                int followIndex = follower.FollowIndex;
                bool flag4 = follower.Leader != null && follower.DelayTimer <= 0f && IsFirstIceKey;
            }
            base.Update();
        }

        public void Dissolve()
        {
            bool flag = dissolved || IsUsed || base.Turning;
            if (!flag)
            {
                dissolved = true;
                bool flag2 = follower.Leader != null;
                if (flag2)
                {
                    Player player = follower.Leader.Entity as Player;
                    player.StrawberryCollectResetTimer = 2.5f;
                    follower.Leader.LoseFollower(follower);
                }
                Add(new Monocle.Coroutine(DissolveRoutine(), true));
            }
        }

        private IEnumerator DissolveRoutine()
        {
            Level level = Scene as Level;
            Session session = level.Session;
            session.DoNotLoad.Remove(ID);
            session.Keys.Remove(ID);
            session.UpdateLevelStartDashes();
            Audio.Play("event:/game/general/seed_poof", Position);
            Collidable = false;
            sprite.Scale = Vector2.One * 0.5f;
            yield return 0.05f;
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
            int num;
            for (int i = 0; i < 6; i = num + 1)
            {
                float dir = Calc.Random.NextFloat(6.28318548f);
                level.ParticlesFG.Emit(StrawberrySeed.P_Burst, 1, Position + Calc.AngleToVector(dir, 4f), Vector2.Zero, dir);
                num = i;
            }
            sprite.Scale = Vector2.Zero;
            Visible = false;
            bool flag = level.Session.Level != startLevel;
            if (flag)
            {
                RemoveSelf();
                yield break;
            }
            yield return 0.3f;
            dissolved = false;
            Audio.Play("event:/game/general/seed_reappear", Position);
            Position = start;
            sprite.Scale = Vector2.One;
            Visible = true;
            Collidable = true;
            level.Displacement.AddBurst(Position, 0.2f, 8f, 28f, 0.2f, null, null);
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


