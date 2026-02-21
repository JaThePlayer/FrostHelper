using FrostHelper.Helpers;
using System.Runtime.InteropServices;

namespace FrostHelper.Components;

/*
 * IDEAS:
 * - RainRegion Entity - optimisation
 *   * Mapper-defined regions for rain to be in
 *   * Rain generators placed within a region will use that region
 *   * Rain generators treat these regions as their bounding box for broad-phase entity checks
 *   * Each region has a name, placing multiple regions with the same name combines their region, creating an arbitrary sized region.
 *     * ALT: Arbitrary Shape Region, but it'd need a FILLED polygon collider + perf issues?
 * - StationaryTypes(List<Type>) - optimisation
 *   * Mapper-defined list of types of entities that the mapper declares will NOT move, ever.
 *   * All stationary entities get combined into a Grid<1x1 pixels>, and collision is only checked against the grid + non-stationary entities.
 * - RainCollider Component - feature. Make water use it instead of hardcoding.
 *   * AttachToSolid(bool) 
 *   * CollideChance(float): impl - Dict<Generator, bool[]/BitArray collidedIds>, array idx set to false by generator when a rain droplet regenerates
 *   * bool OnRainHit(Vector2 pos) - returns: whether the collision actually occured. Can be set via API.
 *   * Stationary(bool)
 *     * if (!AttachToSolid && && CollideChance >= 1f && OnRainHit is null && Stationary),
 *     * this collider is treated as stationary and can be merged into the stationary Grid.  
 */

internal sealed class DynamicRainGroup() : Component(true, false) {
    public float PlayerInsideTimer;

    public string FlagIfPlayerInside { get; set; } = "";
    
    public ParticleSystem Particles = new(Depths.Particles, 100);

    public Action<Player, Color>? OnPlayer { get; init; }

    private static Type[]? _defaultEntityFilter;
    public Type[] EntityFilter { get; set; } = 
        _defaultEntityFilter ??= FrostModule.GetTypes("Celeste.Player,Celeste.Solid,Celeste.Water");

    public override void Update() {
        base.Update();

        if (FlagIfPlayerInside != "")
            (Scene as Level)!.Session.SetFlag(FlagIfPlayerInside, PlayerInsideTimer > 0f);
        else
            Active = false; // Remember to remove if this method starts doing more.
        if (PlayerInsideTimer > 0f)
            PlayerInsideTimer -= Engine.DeltaTime;
    }

    public override void EntityAwake() {
        base.EntityAwake();

        Particles.Depth = Entity.Depth;
        Scene.Add(Particles);
    }
}

internal sealed class DynamicRainGenerator : Component {
    #region Hooks
    private static bool _hooksLoaded;
    
    [OnLoad]
    internal static void LoadHooksIfNeeded() {
        if (_hooksLoaded)
            return;
        _hooksLoaded = true;
        
        On.Celeste.Water.ctor_Vector2_bool_bool_float_float += WaterCtor;
    }

    private static void WaterCtor(On.Celeste.Water.orig_ctor_Vector2_bool_bool_float_float orig, Water self, Vector2 position, bool topsurface, bool bottomsurface, float width, float height) {
        orig(self, position, topsurface, bottomsurface, width, height);

        float rippleTimerStart = 0f;
        self.Add(new RainCollider(self.Collider, false) {
            MakeSplashes = true,
            OnMakeSplashes = (ParticleSystem system, ref Rain rain) => {
                if ((self.Scene.TimeActive - rippleTimerStart) > 0f) {
                    Vector2 p = new(rain.Position.X, self.Y);
                    self.TopSurface?.DoRipple(p, 0.21f);
                    rippleTimerStart = self.Scene.TimeActive;
                }

                return false;
            }
        });
    }

    [OnUnload]
    internal static void UnloadHooks() {
        if (!_hooksLoaded)
            return;
        _hooksLoaded = false;
        
        On.Celeste.Water.ctor_Vector2_bool_bool_float_float -= WaterCtor;
    }
    #endregion
    
    public Vector2 Offset;

    internal static readonly Color[] DefaultColors = [Calc.HexToColor("161933")];
    internal Color[] Colors { get; set; } = DefaultColors;
    
    internal float Alpha { get; set; } = 1f;
    
    internal Vector2 SpeedRange { get; set; } = new(200f, 600f);

    internal Vector2 RotationRange { get; set; } = new(-0.05f, 0.05f);

    internal Vector2 ScaleRange { get; set; } = new(4f, 16f);
    
    internal ConditionHelper.Condition EnableCondition { get; set; } = ConditionHelper.EmptyCondition;
    
    internal bool IsRainbow { get; set; }
    
    internal required DynamicRainGroup Group { get; init; }

    internal float PreSimulationTime { get; set; } = 1f;
    
    private bool _wasEnabled;
    private readonly Random _random;
    private readonly int _length;
    private Rectangle _levelBounds;
    
    private readonly List<Entity> _potentialCollisionTargets = [];
    private readonly List<RainCollider> _potentialRainColliders = [];
    
    private readonly Rain[] _rains;
    private readonly HashSet<RainCollider>?[] _collidersWithin;

    public DynamicRainGenerator(int length, float density) : base(true, true) {
        _random = new Random(Calc.Random.Next());
        _length = length;
        _rains = new Rain[(int)(length * density)];
        _collidersWithin = new HashSet<RainCollider>[_rains.Length];
    }

    private Vector2 RenderPos => Offset + Entity.Position;

    public override void EntityAwake() {
        base.EntityAwake();
        
        if (Scene is not Level level)
            return;
        
        _levelBounds = level.Bounds;
        _lastPosition = RenderPos;
        // calculate how far away rain could possibly get

        Rectangle bounds = new Rectangle((int)RenderPos.X - _length / 2, (int)RenderPos.Y, _length, 8);
        for (float p = 0; p <= 1f; p += 1f / 60f) {
            var angle = 1.57079637f + Calc.ClampedMap(p, 0f, 1f, RotationRange.X, RotationRange.Y);
            var angleVec = Calc.AngleToVector(angle, 1);
            var dist = float.MaxValue;
            if (angleVec.X < 0f) {
                var distToLeft = (RenderPos.X - _levelBounds.Left) / float.Abs(angleVec.X);
                if (distToLeft > 0f)
                    dist = float.Min(dist, distToLeft);
            } else if (angleVec.X > 0f) {
                var distToRight = (_levelBounds.Right - RenderPos.X) / angleVec.X;
                if (distToRight > 0f)
                    dist = float.Min(dist, distToRight);
            }
            
            if (angleVec.Y < 0f) {
                var distToTop = (RenderPos.Y - _levelBounds.Top) / -angleVec.Y;
                if (distToTop > 0f)
                    dist = float.Min(dist, distToTop);
            } else if (angleVec.Y > 0f) {
                var distToBottom = (_levelBounds.Bottom - RenderPos.Y) / angleVec.Y;
                if (distToBottom > 0f)
                    dist = float.Min(dist, distToBottom);
            }
            
            
            var vector = angleVec * dist + RenderPos;
            
            var v1 = vector + angleVec.Perpendicular() * (-_length / 2f);
            var v2 = vector + angleVec.Perpendicular() * (_length / 2f);
            
            bounds = RectangleExt.Merge(bounds, new((int)v1.X, (int)v1.Y, 1, 1));
            bounds = RectangleExt.Merge(bounds, new((int)v2.X, (int)v2.Y, 1, 1));
        }
        bounds.Inflate(4, 4);
        _bounds = bounds;
        
        for (int index = 0; index < _rains.Length; ++index)
            _rains[index].Init(GetNewRainPos(), this);

        // Update at a fixed 60 fps instead of Engine.DeltaTime, as certain entities (like Teleport Doors)
        // might set Engine.TimeRate to 0 on room load, causing a permanent loop.
        // Since none of this is visible, there is no point in simulating precision differences from TimeRate changes anyway -
        // - this only needs to be a rough estimate.
        for (float f = 0; f < PreSimulationTime; f += 1f / 60f)
            UpdateSimulation(1f / 60f);
    }

    public override void Update() {
        Visible = CameraCullHelper.IsRectangleVisible(GetBounds());
        if (!Visible)
            return;

        UpdateSimulation(Engine.DeltaTime);
    }

    private Rectangle _bounds;
    private Vector2 _lastPosition;
    private Rectangle GetBounds() {
        var renderPos = RenderPos;
        if (_lastPosition != renderPos) {
            // If we've moved, merge the old bounds with new bounds - there might still be rain in the old location we shouldn't cull.
            _bounds = RectangleExt.Merge(_bounds, _bounds.MovedBy(renderPos - _lastPosition));
            _lastPosition = renderPos;
        }
        
        return _bounds;
    }

    private NumVector2 GetNewRainPos() {
        return RenderPos.ToNumerics();
    }

    /*
    public override void DebugRender(Camera camera) {
        base.DebugRender(camera);
        Draw.Rect(GetBounds(), Color.Red * 0.16f);
    }
    */

    public override void Render() {
        Rectangle bounds = GetBounds();
        var cam = (Scene as Level)?.Camera;
        if (cam is null)
            return;
        
        if (!CameraCullHelper.IsRectangleVisible(bounds, camera: cam))
            return;

        var rains = _rains;
        var batch = Draw.SpriteBatch;
        var pixel = DrawExt.Pixel;
        for (int i = 0; i < rains.Length; i++) {
            ref Rain rain = ref rains[i];
            if (rain.Color == default || !CameraCullHelper.IsPointVisible(rain.Position, 8f, cam))
                continue;
            
            batch.Draw(pixel, rain.Position.ToXna(), null, rain.Color, rain.Rotation, new(0.5f, 0.5f), rain.Scale, SpriteEffects.None, 0.0f);
        }
    }

    private void ReinitializeRain() {
        var rains = _rains;
        for (var i = 0; i < rains.Length; i++) {
            ref Rain rain = ref rains[i];
            if (rain.Color == default)
                rain.Init(GetNewRainPos(), this);
        }
    }
    
    private void UpdateSimulation(float deltaTime) {
        if (Scene is not Level level)
            return;
        var cam = level.Camera;

        var enabled = EnableCondition.Check();
        if (enabled && !_wasEnabled) {
            ReinitializeRain();
        }

        Rectangle colliderBounds = default;

        var selfBounds = GetBounds();
        var bottom = level.Bounds.Bottom;

        // Find all candidate entities that any of our rain droplets *could* collide with.
        foreach (var type in Group.EntityFilter) {
            CollideInto(level, selfBounds, _potentialCollisionTargets, type, ref colliderBounds);
        }
        // Same for rain colliders
        // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
        foreach (RainCollider rainCollider in level.Tracker.SafeGetComponents<RainCollider>()) {
            if (FastBroadCollision(ref selfBounds, rainCollider.Collider, ref colliderBounds)) {
                _potentialRainColliders.Add(rainCollider);
            }
        }

        Player? playerInside = null;
        Color firstPlayerCollidedColor = default;
        
        var rains = _rains;
        var targetEntities = CollectionsMarshal.AsSpan(_potentialCollisionTargets);
        var targetRainColliders = CollectionsMarshal.AsSpan(_potentialRainColliders);
        var madeRipple = false; // multiple ripples don't really work well, AND cause lag.

        for (var i = 0; i < rains.Length; i++) {
            ref Rain rain = ref rains[i];
            if (rain.Color == default)
                continue;

            rain.Position += rain.Speed * deltaTime;
            var shouldInit = false;
            if (rain.Position.Y > bottom) {
                shouldInit = true;
            } else if (colliderBounds.Contains(rain.Position)) {
                if (CheckCollision(i, ref rain, targetRainColliders) is { } rainCollider) {
                    shouldInit = true;
                    if (rainCollider.MakeSplashes && CameraCullHelper.IsPointVisible(rain.Position, 8f, cam)) {
                        rainCollider.MakeSplashesImpl(Group.Particles, ref rain);
                    }
                } else if (CheckCollision(rain.Position, targetEntities) is { } collidedEntity) {
                    if (playerInside is null) {
                        playerInside = collidedEntity.GetType() == typeof(Player) ? (Player)collidedEntity : null;
                        firstPlayerCollidedColor = rain.Color;
                    }

                    if (CameraCullHelper.IsPointVisible(rain.Position, 8f, cam)) {
                        Group.Particles.Emit(
                            Water.P_Splash, 
                            // WaterInteraction.P_Drip,
                            rain.Position.ToXna(), rain.Color, float.Pi + rain.Rotation);
                    }
                    shouldInit = true;
                } 
            }

            if (shouldInit) {
                _collidersWithin[i]?.Clear();
                if (enabled)
                    rain.Init(GetNewRainPos(), this);
                else
                    rain.Color = default;
            }
        }

        _potentialCollisionTargets.Clear();
        _potentialRainColliders.Clear();
        _wasEnabled = enabled;

        if (playerInside is {} player) {
            Group.PlayerInsideTimer = deltaTime * 45f;
            Group.OnPlayer?.Invoke(player, firstPlayerCollidedColor);
        }
    }

    private Entity? CheckCollision(NumVector2 point, Span<Entity> collidedEntities) {
        foreach (Entity e in collidedEntities)
            if (FastPointCollision(point, e.Collider))
                return e;

        return null;
    }
    
    private RainCollider? CheckCollision(int rainIdx, ref Rain rain, Span<RainCollider> collidedEntities) {
        foreach (RainCollider e in collidedEntities)
            if (FastPointCollision(rain.Position, e.Collider)) {
                if (e.PassThroughChance > 0f) {
                    var store = _collidersWithin[rainIdx] ??= [];
                    if (!store.Add(e) || _random.NextFloat(1f) < e.PassThroughChance) {
                        continue;
                    }
                }

                if (e.TryHit(ref rain)) {
                    return e;
                }
            }

        return null;
    }

    internal static void CollideInto(Scene scene, Rectangle rect, List<Entity> hits, Type type, ref Rectangle colliderBounds) {
        if (!scene.Tracker.Entities.TryGetValue(type, out var entities))
            return;

        foreach (Entity e in CollectionsMarshal.AsSpan(entities)) {
            if (!e.Collidable || e.Collider is not {} collider)
                continue;

            if (FastBroadCollision(ref rect, collider, ref colliderBounds)) {
                hits.Add(e);
            }
        }
    }

    #region FastCollisions
    private static bool FastPointCollision(NumVector2 point, Collider collider) {
        // Fast path, because Hitbox collisions are stupidly slow (TODO: Everest PR)
        if (collider.GetType() == typeof(Hitbox)) {
            var hitbox = (Hitbox) collider;
            var pos = hitbox.Position + hitbox.Entity.Position;
            var bounds = new Rectangle((int)pos.X, (int)pos.Y, (int)hitbox.width, (int)hitbox.height);
            return bounds.Contains(point);
        }
        
        // Fast path, because Grid collisions are stupidly slow (TODO: Everest PR)
        if (collider.GetType() == typeof(Grid))
        {
            var grid = (Grid) collider;
            var p = point - grid.Position.ToNumerics() - grid.Entity.Position.ToNumerics();
            return grid.Data[(int) (p.X / grid.CellWidth), (int) (p.Y / grid.CellHeight)];
        }
        
        return collider.Collide(point.ToXna());
    }

    /// <summary>
    /// Performs a fast collision against this collider for broad-phase checking, expanding colliderBounds as needed.
    /// Might not do a full collision check if its deemed worth it for perf, for example grids always return true.
    /// </summary>
    private static bool FastBroadCollision(ref Rectangle rect, Collider collider, ref Rectangle colliderBounds) {
        if (collider.GetType() == typeof(Hitbox)) {
            // Fast path, because Hitbox collisions are stupidly slow
            var hitbox = (Hitbox) collider;
            var pos = hitbox.Position + (hitbox.Entity?.Position ?? default);
            var bounds = new Rectangle((int)pos.X, (int)pos.Y, (int)hitbox.width, (int)hitbox.height);
            if (bounds.Intersects(rect)) {
                colliderBounds = colliderBounds.Width == 0 ? bounds : RectangleExt.Merge(colliderBounds, bounds);
                return true;
            }
        }
        // Grid collisions become really expensive with large rectangles, it's better to just accept them here and do point collision checks against them later.
        else if (collider.GetType() == typeof(Grid) || collider.Collide(rect)) {
            var bounds = collider.Bounds;
            colliderBounds = colliderBounds.Width == 0 ? bounds : RectangleExt.Merge(colliderBounds, bounds);
            return true;
        }

        return false;
    }
    #endregion

    internal struct Rain {
        public NumVector2 Position;
        public NumVector2 Speed;
        public Vector2 Scale;
        public float Rotation;
        public Color Color;

        public void Init(NumVector2 position, DynamicRainGenerator generator) {
            Position = position;

            var random = generator._random;
            Rotation = 1.57079637f + random.Range(generator.RotationRange.X, generator.RotationRange.Y);
            var speedRange = generator.SpeedRange;
            var speedLen = random.Range(speedRange.X, speedRange.Y);
            var angle = Calc.AngleToVector(Rotation, 1f).ToNumerics();

            var halfWidth = generator._length / 2f;
            Position += angle.Perpendicular() * (random.Range(-halfWidth, halfWidth));
            Speed = angle * speedLen;
            Scale = new Vector2(generator.ScaleRange.X + (speedLen - speedRange.X) / (speedRange.Y - speedRange.X) * (generator.ScaleRange.Y - generator.ScaleRange.X), 1f);
            Color = (generator.IsRainbow ? ColorHelper.GetHue(generator.Scene, position.ToXna()) : random.Choose(generator.Colors)) * generator.Alpha;
        }
    }
}