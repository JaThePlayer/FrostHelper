using Celeste;
using Celeste.Mod;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace FrostTempleHelper
{

    /// <summary>
    /// What the heck is this, you ask?
    /// It's a Camera Target trigger that doesn't crash with dual players when one dies.
    /// :shrug:
    /// </summary>
    [Tracked(false)]
    [CustomEntity("FrostHelper/CameraTargetV2")]
    class CameraTargetTriggerv2 : Trigger
    {
        public CameraTargetTriggerv2(EntityData data, Vector2 offset) : base(data, offset)
        {
            this.Target = data.Nodes[0] + offset - new Vector2(320f, 180f) * 0.5f;
            this.LerpStrength = data.Float("lerpStrength", 0f);
            this.PositionMode = data.Enum<Trigger.PositionModes>("positionMode", Trigger.PositionModes.NoEffect);
            this.XOnly = data.Bool("xOnly", false);
            this.YOnly = data.Bool("yOnly", false);
            this.DeleteFlag = data.Attr("deleteFlag", "");
        }

        public override void OnEnter(Player player)
        {
            base.OnEnter(player);
        }

        public override void OnStay(Player play)
        {
            bool flag = string.IsNullOrEmpty(this.DeleteFlag) || !base.SceneAs<Level>().Session.GetFlag(this.DeleteFlag);
            if (flag)
            {
                foreach (Player player in Scene.Tracker.GetEntities<Player>())
                {
                    player.CameraAnchor = this.Target;
                    player.CameraAnchorLerp = Vector2.One * MathHelper.Clamp(this.LerpStrength * base.GetPositionLerp(player, this.PositionMode), 0f, 1f);
                    player.CameraAnchorIgnoreX = this.YOnly;
                    player.CameraAnchorIgnoreY = this.XOnly;
                }
            }
        }

        public override void OnLeave(Player play)
        {
            base.OnLeave(play);
            bool flag = false;
            foreach (Entity entity in Engine.Scene.Tracker.GetEntities<CameraTargetTriggerv2>())
            {
                CameraTargetTriggerv2 cameraTargetTrigger = (CameraTargetTriggerv2)entity;
                bool playerIsInside = cameraTargetTrigger.PlayerIsInside;
                if (playerIsInside)
                {
                    flag = true;
                    break;
                }
            }
            if (!flag)
            {
                foreach (Entity entity2 in Engine.Scene.Tracker.GetEntities<CameraAdvanceTargetTrigger>())
                {
                    CameraAdvanceTargetTrigger cameraAdvanceTargetTrigger = (CameraAdvanceTargetTrigger)entity2;
                    bool playerIsInside2 = cameraAdvanceTargetTrigger.PlayerIsInside;
                    if (playerIsInside2)
                    {
                        flag = true;
                        break;
                    }
                }
            }
            if (!flag)
            {
                foreach (Player player in Engine.Scene.Tracker.GetEntities<Player>())
                    player.CameraAnchorLerp = Vector2.Zero;
            }
        }

        public Vector2 Target;

        public float LerpStrength;

        public Trigger.PositionModes PositionMode;

        public bool XOnly;

        public bool YOnly;

        public string DeleteFlag;
    }
}
