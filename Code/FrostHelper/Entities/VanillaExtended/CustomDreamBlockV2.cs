namespace FrostHelper;

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
        Color prevBackColor, prevLineColor;
        Color backColor, lineColor;

        if (playerHasDreamDash) {
            lineColor = ActiveLineColor;
            backColor = ActiveBackColor;
            prevBackColor = _getActiveBack();
            prevLineColor = _getActiveLine();
        } else {
            lineColor = DisabledLineColor;
            backColor = DisabledBackColor;
            prevBackColor = _getDisabledBack();
            prevLineColor = _getDisabledLine();
        }
        
        SetDreamBlockColors(backColor, lineColor, playerHasDreamDash);

        base.Render();

        SetDreamBlockColors(prevBackColor, prevLineColor, playerHasDreamDash);

        /*
        if (playerHasDreamDash) {
            // change the colors
            DreamBlock_activeBackColor.SetValue(null, ActiveBackColor);
            DreamBlock_activeLineColor.SetValue(null, ActiveLineColor);
            base.Render();
            // revert changes
            DreamBlock_activeBackColor.SetValue(null, baseActiveBackColor);
            DreamBlock_activeLineColor.SetValue(null, baseActiveLineColor);
        } else {
            var back = (Color)DreamBlock_disabledBackColor.GetValue(null);
            var line = (Color) DreamBlock_disabledLineColor.GetValue(null);

            // change the colors
            DreamBlock_disabledBackColor.SetValue(null, DisabledBackColor);
            DreamBlock_disabledLineColor.SetValue(null, DisabledLineColor);
            base.Render();
            // revert changes
            DreamBlock_disabledBackColor.SetValue(null, back);
            DreamBlock_disabledLineColor.SetValue(null, line);
        }

        */
    }

    private static readonly FieldInfo DreamBlock_activeBackColor = typeof(DreamBlock).GetField("activeBackColor", BindingFlags.NonPublic | BindingFlags.Static);
    private static readonly FieldInfo DreamBlock_disabledBackColor = typeof(DreamBlock).GetField("disabledBackColor", BindingFlags.NonPublic | BindingFlags.Static);
    private static readonly FieldInfo DreamBlock_activeLineColor = typeof(DreamBlock).GetField("activeLineColor", BindingFlags.NonPublic | BindingFlags.Static);
    private static readonly FieldInfo DreamBlock_disabledLineColor = typeof(DreamBlock).GetField("disabledLineColor", BindingFlags.NonPublic | BindingFlags.Static);

    public static void SetDreamBlockColors(Color back, Color line, bool active) {
        var method = active ? _setActiveDreamBlockColors : _setDisabledDreamBlockColors;

        method(back, line);
    }

    private static readonly Action<Color, Color> _setActiveDreamBlockColors = _getIL_setDreamBlockColors(DreamBlock_activeBackColor, DreamBlock_activeLineColor, true);
    private static readonly Action<Color, Color> _setDisabledDreamBlockColors = _getIL_setDreamBlockColors(DreamBlock_disabledBackColor, DreamBlock_disabledLineColor, false);
    private static readonly Func<Color> _getActiveLine = _getIL_getDreamBlockColor(DreamBlock_activeLineColor);
    private static readonly Func<Color> _getDisabledLine = _getIL_getDreamBlockColor(DreamBlock_disabledLineColor);
    private static readonly Func<Color> _getActiveBack = _getIL_getDreamBlockColor(DreamBlock_activeBackColor);
    private static readonly Func<Color> _getDisabledBack = _getIL_getDreamBlockColor(DreamBlock_disabledBackColor);

    private static Func<Color> _getIL_getDreamBlockColor(FieldInfo field) {
        return EasierILHook.CreateDynamicMethod<Func<Color>>($"CustomDreamBlockV2._getDreamBlockColor_{field.Name}", (ILProcessor gen) => {
            gen.Emit(OpCodes.Ldsfld, field);
            gen.Emit(OpCodes.Ret);
        });
    }

    private static Action<Color, Color> _getIL_setDreamBlockColors(FieldInfo back, FieldInfo line, bool active) {
        string methodName = $"CustomDreamBlockV2._setDreamBlockColors_{(active ? "active" : "_disabled")}";

        DynamicMethodDefinition method = new DynamicMethodDefinition(methodName, null, new[] { typeof(Color), typeof(Color) });
        var gen = method.GetILProcessor();

        gen.Emit(OpCodes.Ldarg_0);
        gen.Emit(OpCodes.Stsfld, back);

        gen.Emit(OpCodes.Ldarg_1);
        gen.Emit(OpCodes.Stsfld, line);

        gen.Emit(OpCodes.Ret);

        return (Action<Color, Color>) method.Generate().CreateDelegate(typeof(Action<Color, Color>));
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
