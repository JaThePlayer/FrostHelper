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
    private bool _shatterBathBombs;

    private readonly float _fallSpeed;
    private readonly float _drainSpeed;
    
    private Color _surfaceColor, _fillColor, _rayTopColor;

    internal Color Color { get; private set; }

    private readonly Hitbox _hitbox;
    private readonly ConditionHelper.Condition? _drainCondition;
    private readonly bool _collideWithPlatforms;
    private readonly bool _collideWithHoldables;
    
    public DynamicWaterfall(EntityData data, Vector2 offset) : base(data, offset) {
        _fallSpeed = data.Float("fallSpeed", 2f * 60f);
        _drainSpeed = data.Float("drainSpeed", 8f * 60f);
        _collideWithPlatforms = data.Bool("collideWithPlatforms", false);
        _collideWithHoldables = data.Bool("collideWithHoldables", false);
        Depth = data.Int("depth", Depth);

        SetColor(data.GetColor("color", "LightSkyBlue"));

        _checkForResize = true;

        _hitbox = new Hitbox(8f, 8f);
        Collidable = true;
        Collider = _hitbox;
        Add(new PlayerCollider(OnPlayer, _hitbox));

        var drainCond = data.Attr("drainCondition", "");
        if (!string.IsNullOrWhiteSpace(drainCond)) {
            _drainCondition = data.GetCondition("drainCondition", "");
            Add(new ExpressionListener<bool>(_drainCondition, OnDrainCondition, true));
        }

        _shatterBathBombs = data.Bool("shatterBathBombs", false);
        Add(new BathBombCollider {
            CanCollideWith = b => b.Color != Color,
            OnCollide = OnBathBomb,
        });
    }

    private void OnBathBomb(BathBomb b) {
        SetColor(b.Color);
        if (_shatterBathBombs)
            b.ShatterIfPossible();
    }

    private void OnDrainCondition(Entity self, Maybe<bool> lastValue, bool newValue) {
        _draining = newValue;
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
    private readonly List<Platform> _tempPlatforms = [];
    private readonly List<Holdable> _tempHoldables = [];

    public override void Awake(Scene scene) {
        base.Awake(scene);

        if (_drainCondition?.Check(scene.ToLevel().Session) ?? false) {
            _draining = true;
            height = 0f;
        }
        UpdateCollider();
    }

    private struct PlatformBlockWaterfallsFilter : IFunc<Platform, bool> {
        public bool Invoke(Platform arg) {
            return arg.BlockWaterfalls;
        }
    }
    
    private struct HoldableColliderGetter : IFunc<Holdable, Collider?> {
        public Collider? Invoke(Holdable arg) {
            return arg.PickupCollider;
        }
    }

    private float GetTopOf(object? obj) =>
        obj switch {
            Entity e => e.Top,
            Holdable h => h.PickupCollider?.AbsoluteTop ?? h.Entity?.Top ?? Y + height,
            _ => Y + height
        };

    public override void Update() {
        Components.Update();
        
        Level level = Scene.ToLevel();

        object? collisionTarget = null;
        
        if (_checkForResize)
        {
            Collidable = true;
            Visible = true;
            float prevHeight = height;
            float heightIncrement = 2f;
            height = heightIncrement;
            water = null;
            collisionTarget = null;
            var maxHeight = prevHeight 
                            + (_draining ? (-_drainSpeed * Engine.DeltaTime) : (_fallSpeed * Engine.DeltaTime));
            maxHeight = float.Max(maxHeight, 0f);

            var maxBounds = new Rectangle((int) X, (int) (Y + height), 8, (int)float.Ceiling(maxHeight));
            
            _tempWaters.Clear();
            _tempPlatforms.Clear();
            _tempHoldables.Clear();
            Scene.CollideInto(maxBounds, _tempWaters);
            if (_collideWithPlatforms)
                CollideExt.CollideInto<Platform, Platform, PlatformBlockWaterfallsFilter>(Scene, maxBounds, _tempPlatforms);
            else
                CollideExt.CollideInto<Platform, Solid, PlatformBlockWaterfallsFilter>(Scene, maxBounds, _tempPlatforms);

            List<Component>? allHoldables = null;
            if (_collideWithHoldables) {
                allHoldables = Scene.Tracker.SafeGetComponents<Holdable>();
                foreach (Holdable holdable in allHoldables) {
                    holdable.PickupCollider.Entity = holdable.Entity;
                }
                CollideExt.CollideIntoComponents(allHoldables, maxBounds, _tempHoldables, new HoldableColliderGetter());
            }

            var bounds = new Rectangle((int) X, (int) (Y + height), 8, (int) float.Ceiling(heightIncrement));
            
            while (Y + height < level.Bounds.Bottom
                   && (collisionTarget = CollideExt.CollideFirstAssumeCollideable(bounds, _tempWaters)) == null
                   && (collisionTarget = CollideExt.CollideFirstAssumeCollideable(bounds, _tempPlatforms)) == null
                   && (collisionTarget = CollideExt.CollideFirstComponent(bounds, _tempHoldables, new HoldableColliderGetter())) == null)
            {
                height += heightIncrement;
                bounds.Y = (int) (Y + height);
                if (height >= maxHeight) {
                    height = maxHeight;
                    break;
                }
            }

            water = collisionTarget as Water;
            solid = collisionTarget as Solid;

            if (collisionTarget != null) {
                if (collisionTarget is BathBomb bomb)
                    OnBathBomb(bomb);
                if (collisionTarget is Holdable { Entity: BathBomb holdableBomb })
                    OnBathBomb(holdableBomb);

                height = float.Max(Y + height, GetTopOf(collisionTarget)) - Y;
            }
            
            if (allHoldables is {})
                foreach (Holdable holdable in allHoldables)
                    holdable.PickupCollider.Entity = null;
            
            if (prevHeight != height)
                UpdateCollider();
        }
        
        loopingSfx.Position.Y = Calc.Clamp(level.Camera.Position.Y + 90f, Y, height);
        enteringSfx.Position.Y = height;
        if (water != null && Scene.OnInterval(0.3f))
            water.TopSurface.DoRipple(new Vector2(X + 4f, water.Y), 0.75f);
        if (water != null || collisionTarget != null)
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