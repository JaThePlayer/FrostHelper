using System;
using System.Collections;
using System.Collections.Generic;
using Celeste;
using Celeste.Mod;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;

namespace FrostHelper
{
    [Celeste.Mod.Entities.CustomEntity("FrostHelper/TemporaryKey")]
    public class TemporaryKey : Key
    {
        private bool IsFirstTemporaryKey
        {
            get
            {
                for (int i = this.follower.FollowIndex - 1; i > -1; i--)
                {
                    bool flag = this.follower.Leader.Followers[i].Entity is TemporaryKey;
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

        public TemporaryKey(EntityData data, Vector2 offset, EntityID id) : base(data.Position + offset, id, null)
        {
            this.follower = Get<Follower>();
            // Create sprite
            DynData<Key> dyndata = new DynData<Key>(this as Key);
            sprite = dyndata.Get<Sprite>("sprite");
            Remove(sprite);
            sprite = new Sprite(GFX.Game, data.Attr("directory", "collectables/FrostHelper/keytemp") + "/");
            sprite.Justify = new Vector2(0.5f, 0.5f);
            sprite.AddLoop("idle", "idle", 0.1f);
            sprite.AddLoop("enter", "enter", 0.1f);
            sprite.Play("idle");
            Add(sprite);
            dyndata.Set("sprite", sprite);
            Follower follower = this.follower;
            follower.OnLoseLeader = (Action)Delegate.Combine(follower.OnLoseLeader, new Action(Dissolve));
            this.follower.PersistentFollow = false; // was false
            Add(new TransitionListener
            {
                OnOut = delegate (float f)
                {
                    this.StartedUsing = false;
                    if (!this.IsUsed)
                    {
                        Dissolve();
                    }
                }
            });
        }

        public override void Added(Scene scene)
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
            Level level = base.Scene as Level;
            Session session = (level != null) ? level.Session : null;
            bool flag = this.IsUsed && !this.wasUsed;
            if (flag)
            {
//                session.DoNotLoad.Add(this.ID);
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
                bool flag4 = this.follower.Leader != null && this.follower.DelayTimer <= 0f && this.IsFirstTemporaryKey;
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
                base.Add(new Coroutine(DissolveRoutine(), true));
            }
        }

        private IEnumerator DissolveRoutine()
        {
            Level level = base.Scene as Level;
            Session session = level.Session;
            if (session.DoNotLoad.Contains(ID))
            session.DoNotLoad.Remove(this.ID);
            if (session.Keys.Contains(ID))
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
                float dir = Calc.Random.NextFloat(6.28318548f);
                level.ParticlesFG.Emit(StrawberrySeed.P_Burst, 1, this.Position + Calc.AngleToVector(dir, 4f), Vector2.Zero, dir);
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

        public override void Removed(Scene scene)
        {
            base.Removed(scene);
            Session session = (scene as Level).Session;
            if (session.DoNotLoad.Contains(ID))
                session.DoNotLoad.Remove(this.ID);
            if (session.Keys.Contains(ID))
                session.Keys.Remove(this.ID);
        }

        private Sprite sprite;

        private Follower follower;

        private Vector2 start;

        private string startLevel;

        private bool dissolved;

        private bool wasUsed;
    }
}

