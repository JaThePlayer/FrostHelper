using Celeste;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.Utils;
using System;
using System.Reflection;


namespace FrostHelper {
    /// <summary>
    /// Custom dream blocks except they extend DreamBlock
    /// </summary>
    //[CustomEntity("FrostHelper/CustomDreamBlock")]
    [TrackedAs(typeof(DreamBlock))]
    [Tracked]
    public class CustomDreamBlockV2 : DreamBlock {
        // new attributes
        public float DashSpeed;
        public bool AllowRedirects;
        public bool AllowRedirectsInSameDir;
        public float SameDirectionSpeedMultiplier;
        public bool ConserveSpeed;


        public Color ActiveBackColor;

        public Color DisabledBackColor;

        public Color ActiveLineColor;

        public Color DisabledLineColor;

        private bool playerHasDreamDash;

        private Vector2? node;
        float moveSpeedMult;
        Ease.Easer easer;
        // legacy
        bool fastMoving;

        public CustomDreamBlockV2(EntityData data, Vector2 offset) : base(data, offset) {
            ActiveBackColor = ColorHelper.GetColor(data.Attr("activeBackColor", "Black"));
            DisabledBackColor = ColorHelper.GetColor(data.Attr("disabledBackColor", "1f2e2d"));
            ActiveLineColor = ColorHelper.GetColor(data.Attr("activeLineColor", "White"));
            DisabledLineColor = ColorHelper.GetColor(data.Attr("disabledLineColor", "6a8480"));
            DashSpeed = data.Float("speed", 240f);
            AllowRedirects = data.Bool("allowRedirects");
            AllowRedirectsInSameDir = data.Bool("allowSameDirectionDash");
            SameDirectionSpeedMultiplier = data.Float("sameDirectionSpeedMultiplier", 1f);
            node = data.FirstNodeNullable(new Vector2?(offset));
            moveSpeedMult = data.Float("moveSpeedMult", 1f);
            easer = EaseHelper.GetEase(data.Attr("moveEase", "SineInOut"));
            ConserveSpeed = data.Bool("conserveSpeed", false);
            // legacy
            fastMoving = data.Bool("fastMoving", false);
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            playerHasDreamDash = SceneAs<Level>().Session.Inventory.DreamDash;
            if (playerHasDreamDash && node != null) {
                Remove(Get<Tween>());
                Vector2 start = Position;
                Vector2 end = node.Value;
                float num = Vector2.Distance(start, end) / (12f * moveSpeedMult);
                if (fastMoving) {
                    num /= 3f;
                }
                Tween tween = Tween.Create(Tween.TweenMode.YoyoLooping, easer, num, true);
                tween.OnUpdate = delegate (Tween t) {
                    if (Collidable) {
                        MoveTo(Vector2.Lerp(start, end, t.Eased));
                        return;
                    }
                    MoveToNaive(Vector2.Lerp(start, end, t.Eased));
                };
                Add(tween);
                node = null;
            }
        }

        public override void Render() {
            if (playerHasDreamDash) {
                // change the colors
                DreamBlock_activeBackColor.SetValue(null, ActiveBackColor);
                DreamBlock_activeLineColor.SetValue(null, ActiveLineColor);
                base.Render();
                // revert changes
                DreamBlock_activeBackColor.SetValue(null, baseActiveBackColor);
                DreamBlock_activeLineColor.SetValue(null, baseActiveLineColor);
            } else {
                // change the colors
                DreamBlock_disabledBackColor.SetValue(null, DisabledBackColor);
                DreamBlock_disabledLineColor.SetValue(null, DisabledLineColor);
                base.Render();
                // revert changes
                DreamBlock_disabledBackColor.SetValue(null, baseDisabledBackColor);
                DreamBlock_disabledLineColor.SetValue(null, baseDisabledLineColor);
            }
        }

        private static readonly FieldInfo DreamBlock_activeBackColor = typeof(DreamBlock).GetField("activeBackColor", BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly FieldInfo DreamBlock_disabledBackColor = typeof(DreamBlock).GetField("disabledBackColor", BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly FieldInfo DreamBlock_activeLineColor = typeof(DreamBlock).GetField("activeLineColor", BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly FieldInfo DreamBlock_disabledLineColor = typeof(DreamBlock).GetField("disabledLineColor", BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly Color baseActiveBackColor = Color.Black;
        private static readonly Color baseDisabledBackColor = Calc.HexToColor("1f2e2d");
        private static readonly Color baseActiveLineColor = Color.White;
        private static readonly Color baseDisabledLineColor = Calc.HexToColor("6a8480");

        #region Hooks
        // Hook initialization
        [OnLoad]
        public static void Load() {
            On.Celeste.Player.DreamDashUpdate += Player_DreamDashUpdate;
            On.Celeste.Player.DreamDashEnd += Player_DreamDashEnd;

            IL.Celeste.Player.DreamDashBegin += Player_DreamDashBegin;
        }

        [OnUnload]
        public static void Unload() {
            On.Celeste.Player.DreamDashUpdate -= Player_DreamDashUpdate;
            On.Celeste.Player.DreamDashEnd -= Player_DreamDashEnd;

            IL.Celeste.Player.DreamDashBegin -= Player_DreamDashBegin;
        }

        private static void Player_DreamDashBegin(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchStfld<Player>("Speed"))) {
                cursor.Index--;
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<Vector2, Player, Vector2>>((origSpeed, player) => {
                    CustomDreamBlockV2 currentDreamBlock = player.CollideFirst<CustomDreamBlockV2>(player.Position + Vector2.UnitX * Math.Sign(player.Speed.X)) ?? player.CollideFirst<CustomDreamBlockV2>(player.Position + Vector2.UnitY * Math.Sign(player.Speed.Y));
                    if (currentDreamBlock != null && currentDreamBlock.ConserveSpeed) {
                        var newSpeed = player.Speed * Math.Sign(currentDreamBlock.DashSpeed);
                        //player.DashDir = newSpeed.SafeNormalize();
                        return newSpeed;
                    }
                    return origSpeed;
                });

                break;
            }
        }

        private static void Player_DreamDashEnd(On.Celeste.Player.orig_DreamDashEnd orig, Player self) {
            orig(self);
            new DynData<Player>(self).Set("lastDreamSpeed", 0f);
        }

        private static int Player_DreamDashUpdate(On.Celeste.Player.orig_DreamDashUpdate orig, Player self) {
            CustomDreamBlockV2 currentDreamBlock = self.CollideFirst<CustomDreamBlockV2>();
            if (currentDreamBlock != null) {
                var dyn = new DynData<Player>(self);

                if (!currentDreamBlock.ConserveSpeed) {
                    float lastDreamSpeed = dyn.Get<float>("lastDreamSpeed");
                    if (lastDreamSpeed != currentDreamBlock.DashSpeed) {
                        self.Speed = self.DashDir * currentDreamBlock.DashSpeed;
                        dyn.Set("lastDreamSpeed", currentDreamBlock.DashSpeed * 1f);
                    }
                }

                // Redirects
                if (currentDreamBlock.AllowRedirects && self.CanDash) {
                    Vector2 aimVector = Input.GetAimVector(self.Facing);
                    bool sameDir = aimVector == self.DashDir;
                    if (!sameDir || currentDreamBlock.AllowRedirectsInSameDir) {
                        self.DashDir = aimVector;
                        self.Speed = self.DashDir * self.Speed.Length();
                        self.Dashes = Math.Max(0, self.Dashes - 1);
                        Audio.Play("event:/char/madeline/dreamblock_enter");
                        if (sameDir) {
                            self.Speed *= currentDreamBlock.SameDirectionSpeedMultiplier;
                            self.DashDir *= Math.Sign(currentDreamBlock.SameDirectionSpeedMultiplier);
                        }

                        if (self.Speed.X != 0.0f)
                            self.Facing = (Facings) Math.Sign(self.Speed.X);

                        Input.Dash.ConsumeBuffer();
                        Input.Dash.ConsumePress();
                        Input.CrouchDash.ConsumeBuffer();
                        Input.CrouchDash.ConsumePress();
                    }
                }
            }
            return orig(self);
        }
        #endregion
    }
}
