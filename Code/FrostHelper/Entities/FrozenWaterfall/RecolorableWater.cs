namespace FrostHelper.Entities.FrozenWaterfall;

[CustomEntity("FrostHelper/RecolorableWater")]
[Tracked]
[TrackedAs(typeof(Water))]
internal sealed class RecolorableWater : Water {
    #region Hooks

    private static bool _hooksLoaded;
    
    [HookPreload]
    internal static void LoadHooksIfNeeded() {
        if (_hooksLoaded)
            return;
        _hooksLoaded = true;
        
        IL.Celeste.Player.NormalUpdate += PlayerOnNormalUpdate;
    }

    private static void PlayerOnNormalUpdate(ILContext il) {
        var cursor = new ILCursor(il);

        cursor.GotoNext(MoveType.Before, i => i.MatchLdfld<Water>(nameof(Water.TopSurface)));

        cursor.GotoPrev(MoveType.After, i => i.MatchCallOrCallvirt<Player>(nameof(Player.Jump)));
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldloc, 13); // water
        cursor.EmitDelegate(OnPlayerJumpedOffOfWater);
    }

    private static void OnPlayerJumpedOffOfWater(Player player, Water water) {
        if (water is RecolorableWater { _triggerOnJump: true } recolorableWater) {
            recolorableWater.OnPlayer(player);
        }
    }

    [OnUnload]
    internal static void Unload() {
        if (!_hooksLoaded)
            return;
        _hooksLoaded = false;
        
        IL.Celeste.Player.NormalUpdate -= PlayerOnNormalUpdate;
    }
    #endregion
    
    private Color _surfaceColor, _fillColor, _rayTopColor;
    private readonly bool _triggerOnJump;

    internal Color Color;
    
    public RecolorableWater(EntityData data, Vector2 offset) : base(data, offset) {
        LoadHooksIfNeeded();

        _triggerOnJump = data.Bool("triggerOnJump");
        SetColor(data.GetColor("color", "LightSkyBlue"));
        Add(new PlayerCollider(OnPlayer));
        
        Add(new BathBombCollider {
            CanCollideWith = b => b.Color != Color,
            OnCollide = b => {
                SetColor(b.Color);
                b.ShatterIfPossible();
            }
        });
    }

    private void OnPlayer(Player player) {
        if (Scene.Tracker.SafeGetEntity<DynamicWaterBehaviorController>() is {} controller)
            controller.HandleBehaviorFor(player, Color);
    }
    
    internal void SetColor(Color color) {
        Color = color;
        _surfaceColor = Color * 0.8f;
        _fillColor = Color * 0.3f;
        _rayTopColor = Color * 0.6f;

        if (TopSurface is { } surface) {
            UpdateSurfaceColor(surface);
        }
    }

    private void UpdateSurfaceColor(Surface surface) {
        var width = Width;
        int num1 = (int) (width / 4.0);
        
        for (int fillStartIndex = surface.fillStartIndex; fillStartIndex < surface.fillStartIndex + num1 * 6; ++fillStartIndex)
            surface.mesh[fillStartIndex].Color = _fillColor;
        for (int surfaceStartIndex = surface.surfaceStartIndex; surfaceStartIndex < surface.surfaceStartIndex + num1 * 6; ++surfaceStartIndex)
            surface.mesh[surfaceStartIndex].Color = _surfaceColor;
    }

    public override void Update() {
        using (new WaterColorOverride(this)) {
            base.Update();
        }
    }

    public override void Render() {
        using (new WaterColorOverride(this)) {
            base.Render();
        }
    }


    private readonly ref struct WaterColorOverride : IDisposable {
        private readonly Color _fillColor;
        private readonly Color _surfaceColor;
        private readonly Color _rayTopColor;
        
        public WaterColorOverride(RecolorableWater water) {
            _fillColor = FillColor;
            _surfaceColor = SurfaceColor;
            _rayTopColor = RayTopColor;
            
            FillColor = water._fillColor;
            SurfaceColor = water._surfaceColor;
            RayTopColor = water._rayTopColor;
        }

        public void Dispose() {
            FillColor = _fillColor;
            SurfaceColor = _surfaceColor;
            RayTopColor = _rayTopColor;
        }
    }
}