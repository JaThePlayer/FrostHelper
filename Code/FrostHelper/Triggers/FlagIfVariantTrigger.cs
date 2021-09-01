using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;

namespace FrostHelper.Triggers
{
    [CustomEntity("FrostHelper/FlagIfVariantTrigger")]
    public class FlagIfVariantTrigger : Trigger
    {
        public enum Variants
        {
            DashAssist,
            GameSpeed,
            Hiccups,
            InfiniteStamina,
            Invincible,
            InvisibleMotion,
            LowFriction,
            MirrorMode,
            NoGrabbing,
            PlayAsBadeline,
            SuperDashing,
            ThreeSixtyDashing,
        }

        private static Assists GetAssists() => SaveData.Instance.Assists;

        private static bool ValueToBool(string value)
        {
            return value.ToLower() == "true";
        }

        public static Dictionary<Variants, Func<string, bool>> VariantCheckers = new Dictionary<Variants, Func<string, bool>>()
        {
            { Variants.DashAssist, (string s) => { return GetAssists().DashAssist == ValueToBool(s); } },
            { Variants.Invincible, (string s) => { return GetAssists().Invincible == ValueToBool(s); } },
            { Variants.Hiccups, (string s) => { return GetAssists().Hiccups == ValueToBool(s); } },
            { Variants.InfiniteStamina, (string s) => { return GetAssists().InfiniteStamina == ValueToBool(s); } },
            { Variants.InvisibleMotion, (string s) => { return GetAssists().InvisibleMotion == ValueToBool(s); } },
            { Variants.LowFriction, (string s) => { return GetAssists().LowFriction == ValueToBool(s); } },
            { Variants.MirrorMode, (string s) => { return GetAssists().MirrorMode == ValueToBool(s); } },
            { Variants.NoGrabbing, (string s) => { return GetAssists().NoGrabbing == ValueToBool(s); } },
            { Variants.PlayAsBadeline, (string s) => { return GetAssists().PlayAsBadeline == ValueToBool(s); } },
            { Variants.SuperDashing, (string s) => { return GetAssists().SuperDashing == ValueToBool(s); } },
            { Variants.ThreeSixtyDashing, (string s) => { return GetAssists().ThreeSixtyDashing == ValueToBool(s); } },
            { Variants.GameSpeed, (string s) => { return GetAssists().GameSpeed == int.Parse(s, System.Globalization.NumberStyles.Integer); } },
        };

        public Variants Variant;
        public string Value;
        public string Flag;
        public bool Inverted;

        public FlagIfVariantTrigger(EntityData data, Vector2 offset) : base(data, offset)
        {
            Variant = data.Enum("variant", Variants.Invincible);
            Value = data.Attr("variantValue", "true");
            Flag = data.Attr("flag");
            Inverted = data.Bool("inverted", false);
        }

        public override void OnStay(Player player)
        {
            base.OnStay(player);
            (Scene as Level).Session.SetFlag(Flag, Inverted != VariantCheckers[Variant](Value));
        }
    }
}
