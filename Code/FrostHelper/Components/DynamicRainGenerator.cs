using FrostHelper.Helpers;
using System.Runtime.InteropServices;

namespace FrostHelper.Components;

internal sealed class DynamicRainGroup() : Component(true, false) {
    public float PlayerInsideTimer;

    public string FlagIfPlayerInside { get; set; } = "";
    
    public ParticleSystem Particles = new(Depths.Particles, 100);

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
    public Vector2 Offset;

    internal static readonly Color[] DefaultColors = [Calc.HexToColor("161933")];
    internal Color[] Colors { get; set; } = DefaultColors;
    
    internal Vector2 SpeedRange { get; set; } = new(200f, 600f);

    internal Vector2 RotationRange { get; set; } = new(-0.05f, 0.05f);

    internal Vector2 ScaleRange { get; set; } = new(4f, 16f);
    
    internal ConditionHelper.Condition EnableCondition { get; set; } = ConditionHelper.EmptyCondition;
    
    internal bool IsRainbow { get; set; }
    
    internal required DynamicRainGroup Group { get; init; }

    internal float PreSimulationTime { get; set; } = 1f;
    
    private bool _wasEnabled;
    private Hitbox? _playerReplacementHitbox = null;
    private readonly Random _random;
    private readonly int _length;
    private Rectangle _levelBounds;
    private readonly List<Entity> _potentialCollisionTargets = new();
    private readonly Rain[] _rains;

    public DynamicRainGenerator(int length, float density) : base(true, true) {
        _random = new Random(Calc.Random.Next());
        _length = length;
        _rains = new Rain[(int)(length * density)];
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

        for (float f = 0; f < PreSimulationTime; f += Engine.DeltaTime) {
            UpdateSimulation();
        }
    }

    public override void Update() {
        Visible = CameraCullHelper.IsRectangleVisible(GetBounds());
        if (!Visible)
            return;

        UpdateSimulation();
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

    
    public override void DebugRender(Camera camera) {
        base.DebugRender(camera);
        Draw.Rect(GetBounds(), Color.Red * 0.16f);
    }
    

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
    
    private void UpdateSimulation() {
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

        var playerInside = false;
        
        var rains = _rains;
        var targets = CollectionsMarshal.AsSpan(_potentialCollisionTargets);
        var madeRipple = false; // multiple ripples don't really work well, AND cause lag.

        for (var i = 0; i < rains.Length; i++) {
            ref Rain rain = ref rains[i];
            if (rain.Color == default)
                continue;

            rain.Position += rain.Speed * Engine.DeltaTime;
            var shouldInit = false;
            if (rain.Position.Y > bottom) {
                shouldInit = true;
            } else if (colliderBounds.Contains(rain.Position) && CheckCollision(rain.Position, targets) is {} collidedEntity) {
                playerInside |= collidedEntity.GetType() == typeof(Player);

                if (CameraCullHelper.IsPointVisible(rain.Position, 8f, cam)) {
                    if (!madeRipple && collidedEntity is Water water) {
                        Vector2 p = new(rain.Position.X, water.Y);
                        water.TopSurface?.DoRipple(p, 0.21f);
                        madeRipple = true;
                        //Particles.Emit(Water.P_Splash, 1, p, new Vector2(8f, 2f), Color, new Vector2(0.0f, -1f).Angle());
                    }
                    Group.Particles.Emit(
                        Water.P_Splash, 
                        // WaterInteraction.P_Drip,
                        rain.Position.ToXna(), rain.Color, float.Pi + rain.Rotation);
                }
                
                shouldInit = true;
            }

            if (shouldInit) {
                if (enabled)
                    rain.Init(GetNewRainPos(), this);
                else
                    rain.Color = default;
            }
        }

        _potentialCollisionTargets.Clear();
        _wasEnabled = enabled;
        
        if (playerInside)
            Group.PlayerInsideTimer = Engine.DeltaTime * 45f;
    }

    private Entity? CheckCollision(NumVector2 point, Span<Entity> collidedEntities) {
        foreach (Entity e in collidedEntities) {
            var collider = e.Collider;
            
            // Fast path, because Hitbox collisions are stupidly slow (TODO: Everest PR)
            if (collider.GetType() == typeof(Hitbox)) {
                var hitbox = (Hitbox) collider;
                var pos = hitbox.Position + e.Position;
                var bounds = new Rectangle((int)pos.X, (int)pos.Y, (int)hitbox.width, (int)hitbox.height);
                if (bounds.Contains(point)) {
                    return e;
                }
            }
            // Fast path, because Grid collisions are stupidly slow (TODO: Everest PR)
            else if (collider.GetType() == typeof(Grid))
            {
                var grid = (Grid) collider;
                var p = point - grid.Position.ToNumerics() - e.Position.ToNumerics();
                if (grid.Data[(int) (p.X / grid.CellWidth), (int) (p.Y / grid.CellHeight)])
                    return e;
            }
            else if (collider.Collide(point.ToXna()))
                return e;
        }

        return null;
    }

    private void CollideInto(Scene scene, Rectangle rect, List<Entity> hits, Type type, ref Rectangle colliderBounds) {
        if (!scene.Tracker.Entities.TryGetValue(type, out var entities))
            return;

        foreach (Entity e in CollectionsMarshal.AsSpan(entities)) {
            if (!e.Collidable || e.Collider is not {} collider)
                continue;

            if (collider.GetType() == typeof(Hitbox)) {
                // Fast path, because Hitbox collisions are stupidly slow
                var hitbox = (Hitbox) collider;
                var pos = hitbox.Position + e.Position;
                var bounds = new Rectangle((int)pos.X, (int)pos.Y, (int)hitbox.width, (int)hitbox.height);
                if (bounds.Intersects(rect)) {
                    colliderBounds = colliderBounds.Width == 0 ? bounds : RectangleExt.Merge(colliderBounds, bounds);
                    hits.Add(e);
                }
            }
            // Grid collisions become really expensive with large rectangles, it's better to just accept them here and do point collision checks against them later.
            else if (collider.GetType() == typeof(Grid) || e.CollideRect(rect)) {
                var bounds = collider.Bounds;
                colliderBounds = colliderBounds.Width == 0 ? bounds : RectangleExt.Merge(colliderBounds, bounds);
                hits.Add(e);
            }
        }
    }

    private struct Rain {
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
            Color = generator.IsRainbow ? ColorHelper.GetHue(generator.Scene, position.ToXna()) : random.Choose(generator.Colors);
        }
    }
}