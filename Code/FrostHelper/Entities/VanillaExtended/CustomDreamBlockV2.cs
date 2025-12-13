namespace FrostHelper;

/// <summary>
/// Custom dream blocks except they extend DreamBlock
/// </summary>
[CustomEntity($"FrostHelper/CustomDreamBlock = {nameof(LoadCustomDreamBlock)}")]
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
    float moveSpeedMult;
    Ease.Easer easer;

    public static Entity LoadCustomDreamBlock(Level level, LevelData levelData, Vector2 offset, EntityData entityData) {
        if (entityData.Bool("old", false)) {
#pragma warning disable CS0618 // Type or member is obsolete
            return new CustomDreamBlock(entityData, offset);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        return new CustomDreamBlockV2(entityData, offset);
    }

    public CustomDreamBlockV2(EntityData data, Vector2 offset) : base(data, offset) {
        LoadIfNeeded();

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
        fastMoving = data.Bool("fastMoving", false);

        var particleTexture = GFX.Game[data.Attr("particlePath", "objects/dreamblock/particles")];
        particleTextures = [
            particleTexture.GetSubtexture(14, 0, 7, 7),
            particleTexture.GetSubtexture(7, 0, 7, 7),
            particleTexture.GetSubtexture(0, 0, 7, 7),
            particleTexture.GetSubtexture(7, 0, 7, 7)
        ];
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
        Color prevBackColor, prevLineColor;

        if (playerHasDreamDash) {
            prevBackColor = DreamBlock.activeBackColor;
            prevLineColor = DreamBlock.activeLineColor;
            DreamBlock.activeLineColor = ActiveLineColor;
            DreamBlock.activeBackColor = ActiveBackColor;
        } else {
            prevBackColor = DreamBlock.disabledBackColor;
            prevLineColor = DreamBlock.disabledLineColor;
            DreamBlock.disabledLineColor = DisabledLineColor;
            DreamBlock.disabledBackColor = DisabledBackColor;
        }

        base.Render();

        if (playerHasDreamDash) {
            DreamBlock.activeLineColor = prevLineColor;
            DreamBlock.activeBackColor = prevBackColor;
        } else {
            DreamBlock.disabledLineColor = prevLineColor;
            DreamBlock.disabledBackColor = prevBackColor;
        }
    }

    #region Hooks
    private static bool _hooksLoaded;

    [HookPreload]
    public static void LoadIfNeeded() {
        if (_hooksLoaded)
            return;
        _hooksLoaded = true;

        On.Celeste.Player.DreamDashUpdate += Player_DreamDashUpdate;
        On.Celeste.Player.DreamDashEnd += Player_DreamDashEnd;
        IL.Celeste.Player.DreamDashBegin += Player_DreamDashBegin;
    }

    [OnUnload]
    public static void Unload() {
        if (!_hooksLoaded)
            return;
        _hooksLoaded = false;

        On.Celeste.Player.DreamDashUpdate -= Player_DreamDashUpdate;
        On.Celeste.Player.DreamDashEnd -= Player_DreamDashEnd;
        IL.Celeste.Player.DreamDashBegin -= Player_DreamDashBegin;
    }

    private static void Player_DreamDashBegin(ILContext il) {
        ILCursor cursor = new ILCursor(il);

        while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchStfld<Player>("Speed"))) {
            cursor.Index--;
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<Vector2, Player, Vector2>>(GetDreamDashSpeed);

            break;
        }
    }

    private static Vector2 GetDreamDashSpeed(Vector2 origSpeed, Player player) {
        CustomDreamBlockV2 currentDreamBlock = player.CollideFirst<CustomDreamBlockV2>(player.Position + Vector2.UnitX * Math.Sign(player.Speed.X)) ?? player.CollideFirst<CustomDreamBlockV2>(player.Position + Vector2.UnitY * Math.Sign(player.Speed.Y));
        if (currentDreamBlock is { ConserveSpeed: true }) {
            var newSpeed = player.Speed * Math.Sign(currentDreamBlock.DashSpeed);
            //player.DashDir = newSpeed.SafeNormalize();
            return newSpeed;
        }

        return origSpeed;
    }

    private static void Player_DreamDashEnd(On.Celeste.Player.orig_DreamDashEnd orig, Player self) {
        orig(self);
        DynamicData.For(self).Set("lastDreamSpeed", 0f);
    }

    private static int Player_DreamDashUpdate(On.Celeste.Player.orig_DreamDashUpdate orig, Player self) {
        CustomDreamBlockV2 currentDreamBlock = self.CollideFirst<CustomDreamBlockV2>();
        if (currentDreamBlock != null) {
            var dyn = DynamicData.For(self);

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
