using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace FrostHelper {

    /// <summary>
    /// What the heck is this, you ask?
    /// It's a Camera Target trigger that doesn't crash with dual players when one dies.
    /// :shrug:
    /// </summary>
    [Tracked(false)]
    [CustomEntity("FrostHelper/CameraTargetV2")]
    class CameraTargetTriggerv2 : Trigger {
        public CameraTargetTriggerv2(EntityData data, Vector2 offset) : base(data, offset) {
            Target = data.Nodes[0] + offset - new Vector2(320f, 180f) * 0.5f;
            LerpStrength = data.Float("lerpStrength", 0f);
            PositionMode = data.Enum<Trigger.PositionModes>("positionMode", PositionModes.NoEffect);
            XOnly = data.Bool("xOnly", false);
            YOnly = data.Bool("yOnly", false);
            DeleteFlag = data.Attr("deleteFlag", "");
        }

        public override void OnEnter(Player player) {
            base.OnEnter(player);
        }

        public override void OnStay(Player play) {
            bool flag = string.IsNullOrEmpty(DeleteFlag) || !SceneAs<Level>().Session.GetFlag(DeleteFlag);
            if (flag) {
                foreach (Player player in Scene.Tracker.GetEntities<Player>()) {
                    player.CameraAnchor = Target;
                    player.CameraAnchorLerp = Vector2.One * MathHelper.Clamp(LerpStrength * GetPositionLerp(player, PositionMode), 0f, 1f);
                    player.CameraAnchorIgnoreX = YOnly;
                    player.CameraAnchorIgnoreY = XOnly;
                }
            }
        }

        public override void OnLeave(Player play) {
            base.OnLeave(play);
            bool flag = false;
            foreach (Entity entity in Engine.Scene.Tracker.GetEntities<CameraTargetTriggerv2>()) {
                CameraTargetTriggerv2 cameraTargetTrigger = (CameraTargetTriggerv2) entity;
                bool playerIsInside = cameraTargetTrigger.PlayerIsInside;
                if (playerIsInside) {
                    flag = true;
                    break;
                }
            }
            if (!flag) {
                foreach (Entity entity2 in Engine.Scene.Tracker.GetEntities<CameraAdvanceTargetTrigger>()) {
                    CameraAdvanceTargetTrigger cameraAdvanceTargetTrigger = (CameraAdvanceTargetTrigger) entity2;
                    bool playerIsInside2 = cameraAdvanceTargetTrigger.PlayerIsInside;
                    if (playerIsInside2) {
                        flag = true;
                        break;
                    }
                }
            }
            if (!flag) {
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
