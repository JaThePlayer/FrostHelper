using System;
using System.Collections;
using Celeste;
using Celeste.Mod;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;

namespace FrostTempleHelper
{
    [CustomEntity("FrostHelper/CoreBerry")]
    [RegisterStrawberry(true, false)]
    public class CoreBerry : Strawberry, IStrawberry
    {
        public bool IsIce;
        Vector2 start;
        string startLevel;
        Sprite indicator;
        public CoreBerry(EntityData data, Vector2 position, EntityID gid) : base(data, position, gid)
        {
            ID = gid;
            IsIce = data.Bool("isIce", false);
            Add(new CoreModeListener(new Action<Session.CoreModes>(OnChangeMode)));
        }
        
        private void OnChangeMode(Session.CoreModes mode)
        {
            if (!IsIce)
            {
                // berry hot
                if (mode == Session.CoreModes.Cold)
                {
                    Dissolve();
                    sprite.Visible = false;
                    indicator.Visible = true;
                }
            } else
            {
                // berry cold
                if (mode == Session.CoreModes.Hot || mode == Session.CoreModes.None)
                {
                    Dissolve();
                    sprite.Visible = false;
                    indicator.Visible = true;
                }
            }
        }

        Sprite sprite;
        public override void Added(Scene scene)
        {
            base.Added(scene);
            Remove(Get<BloomPoint>());
            string spr;
            string ind;
            if (IsIce)
            {
                ind = "collectables/FrostHelper/CoreBerry/Cold/CoreBerry_Cold_Indicator";
                if (SaveData.Instance.CheckStrawberry(ID))
                {
                    spr = "coldberryghost";
                    
                } else
                {
                    spr = "coldberry";
                }
            }
            else
            {
                ind = "collectables/FrostHelper/CoreBerry/Hot/CoreBerry_Hot_Indicator";
                if (SaveData.Instance.CheckStrawberry(ID))
                {
                    spr = "hotberryghost";
                }
                else
                {
                    spr = "hotberry";
                }
            }
            DynData<Strawberry> data = new DynData<Strawberry>(this as Strawberry);
            sprite = data.Get<Sprite>("sprite");
            Remove(sprite);
            sprite = FrostHelper.FrostModule.SpriteBank.Create(spr);
            Add(sprite);
            data.Set("sprite", sprite);
            indicator = new Sprite(GFX.Game, ind);
            indicator.AddLoop("idle", "", 0.1f);
            indicator.Play("idle", false, false);
            indicator.CenterOrigin();
            indicator.Visible = false;
            Add(indicator);
            /*
            if (SaveData.Instance.CheckStrawberry(ID))
            {
                sprite.Visible = false;
                indicator.Visible = true;
            } else
            {
                sprite.Visible = true;
                indicator.Visible = false;
            } */
            Level level = scene as Level;
            if (level != null)
            {
                start = Position;
                startLevel = level.Session.Level;
            }
            //OnChangeMode((scene as Level).Session.CoreMode);
            Session.CoreModes mode = (scene as Level).Session.CoreMode;
            if (!IsIce)
            {
                // berry hot
                if (mode == Session.CoreModes.Cold)
                {
                    Dissolve(false);
                    sprite.Visible = false;
                    indicator.Visible = true;
                }
            }
            else
            {
                // berry cold
                if (mode == Session.CoreModes.Hot || mode == Session.CoreModes.None)
                {
                    Dissolve(false);
                    sprite.Visible = false;
                    indicator.Visible = true;
                }
            }
        }

        public void Dissolve(bool visible = true)
        {
            if (Follower.Leader != null)
            {
                Player player = Follower.Leader.Entity as Player;
                player.StrawberryCollectResetTimer = 2.5f;
                Follower.Leader.LoseFollower(Follower);
            }
            Add(new Coroutine(DissolveRoutine(visible), true));
        }

        private IEnumerator DissolveRoutine(bool visible = true)
        {
            Level level = Scene as Level;
            Session session = level.Session;
            session.DoNotLoad.Remove(ID);
            Audio.Play("event:/game/general/seed_poof", Position);
            Collidable = false;
            sprite.Scale = Vector2.One * 0.5f;
            yield return 0.05f;
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
            if (visible)
            {
                for (int i = 0; i < 6; i++)
                {
                    float dir = Calc.Random.NextFloat(6.28318548f);
                    level.ParticlesFG.Emit(StrawberrySeed.P_Burst, 1, Position + Calc.AngleToVector(dir, 4f), Vector2.Zero, dir);
                }
            }
            
            sprite.Scale = Vector2.Zero;
            //Visible = false;
            //RemoveSelf();
            hidden = true;
            sprite.Visible = false;
            indicator.Visible = true;
            yield break;
        }

        bool hidden;
    }
}