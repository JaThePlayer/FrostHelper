using FrostHelper.Components;
using FrostHelper.Helpers;

namespace FrostHelper.Entities.FrozenWaterfall;

/*
TODO:
- LoopingWaterfall
- session expressions in behaviors should have something like $swimTime(float) - so you can implement stuff like drowning yourself.
- teleport waterfalls (no default colors, just add behavior)
*/

[CustomEntity("FrostHelper/DynamicWaterfall")]
[Tracked]
internal sealed class DynamicWaterfall : WaterFall {
    private bool _draining;
    private bool _checkForResize;

    private readonly float _fallSpeed;
    private readonly float _drainSpeed;
    
    private Color _surfaceColor, _fillColor, _rayTopColor;

    internal Color Color;

    private readonly Hitbox _hitbox;
    private readonly ConditionHelper.Condition? _drainCondition;
    
    public DynamicWaterfall(EntityData data, Vector2 offset) : base(data, offset) {
        _fallSpeed = data.Float("fallSpeed", 2f * 60f);
        _drainSpeed = data.Float("drainSpeed", 8f * 60f);

        SetColor(data.GetColor("color", "LightSkyBlue"));

        _checkForResize = true;

        _hitbox = new Hitbox(8f, 8f);
        Collidable = true;
        Collider = _hitbox;
        Add(new PlayerCollider(OnPlayer, _hitbox));

        var drainCond = data.Attr("drainCondition", "");
        if (!string.IsNullOrWhiteSpace(drainCond)) {
            _drainCondition = data.GetCondition("drainCondition", "");
            Add(new ExpressionListener(_drainCondition, OnDrainCondition, true));
        }
    }

    private void OnDrainCondition(Entity self, object? lastValue, object newValue) {
        //var last = ConditionHelper.Condition.CoerceToBool(lastValue ?? false);
        var curr = ConditionHelper.Condition.CoerceToBool(newValue ?? false);

        _draining = curr;
    }

    internal void SetColor(Color color) {
        Color = color;
        _surfaceColor = Color * 0.8f;
        _fillColor = Color * 0.3f;
        _rayTopColor = Color * 0.6f;
    }

    private void OnPlayer(Player player) {
        if (Scene.Tracker.SafeGetEntity<DynamicWaterBehaviorController>() is {} controller)
            controller.HandleBehaviorFor(player, Color);
    }

    private readonly List<Water> _tempWaters = [];
    private readonly List<Solid> _tempSolids = [];

    public override void Awake(Scene scene) {
        base.Awake(scene);

        if (_drainCondition?.Check(scene.ToLevel().Session) ?? false) {
            _draining = true;
            height = 0f;
        }
        UpdateCollider();
    }

    public override void Update() {
        Components.Update();
        
        Level level = Scene.ToLevel();
        
        if (_checkForResize)
        {
            Collidable = true;
            Visible = true;
            float prevHeight = height;
            float heightIncrement = 2f;
            height = heightIncrement;
            water = null;
            solid = null;
            var maxHeight = prevHeight 
                            + (_draining ? (-_drainSpeed * Engine.DeltaTime) : (_fallSpeed * Engine.DeltaTime));
            maxHeight = float.Max(maxHeight, 0f);

            var maxBounds = new Rectangle((int) X, (int) (Y + height), 8, (int)float.Ceiling(maxHeight));
            
            _tempWaters.Clear();
            _tempSolids.Clear();
            Scene.CollideInto(maxBounds, _tempWaters);
            Scene.CollideInto(maxBounds, _tempSolids);
            
            while (Y + height < level.Bounds.Bottom 
                   && (water = CollideExt.CollideFirst(new Rectangle((int)X, (int)(Y + height), 8, (int)float.Ceiling(heightIncrement)), _tempWaters)) == null 
                   && ((solid = CollideExt.CollideFirst(new Rectangle((int)X, (int)(Y + height), 8, (int)float.Ceiling(heightIncrement)), _tempSolids)) == null 
                       || !solid.BlockWaterfalls))
            {
                height += heightIncrement;
                if (height >= maxHeight) {
                    height = maxHeight;
                    break;
                }
                solid = null;
            }
            if (prevHeight != height)
            {
                UpdateCollider();
            }
        }
        
        loopingSfx.Position.Y = Calc.Clamp(level.Camera.Position.Y + 90f, Y, height);
        if (water != null && Scene.OnInterval(0.3f))
            water.TopSurface.DoRipple(new Vector2(X + 4f, water.Y), 0.75f);
        if (water != null || solid != null)
        {
            level.ParticlesFG.Emit(Water.P_Splash, 1,
                new Vector2(X + 4f, Y + height + 2.0f), 
                new Vector2(8f, 2f),
                Color,
                new Vector2(0.0f, -1f).Angle());
        }
    }

    public override void Render() {
        if (water?.TopSurface == null)
        {
            Draw.Rect(X + 1f, Y, 6f, height, _fillColor);
            Draw.Rect(X - 1f, Y, 2f, height, _surfaceColor);
            Draw.Rect(X + 7f, Y, 2f, height, _surfaceColor);
            return;
        }

        var topSurface = water.TopSurface;
        float h = height + water.TopSurface.Position.Y - water.Y;
        for (int index = 0; index < 6; ++index)
            Draw.Rect(X + index + 1.0f, Y, 1f, h - topSurface.GetSurfaceHeight(new Vector2(X + 1f + index, water.Y)), _fillColor);
        Draw.Rect(X - 1f, Y, 2f, h - topSurface.GetSurfaceHeight(new Vector2(X, water.Y)), _surfaceColor);
        Draw.Rect(X + 7f, Y, 2f, h - topSurface.GetSurfaceHeight(new Vector2(X + 8f, water.Y)), _surfaceColor);
    }

    private void UpdateCollider() {
        _hitbox.height = height;
    }
}