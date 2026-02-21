using FrostHelper.Components;
using FrostHelper.Helpers;
using System.Runtime.CompilerServices;

namespace FrostHelper.Entities;

[CustomEntity("FrostHelper/CastedArbitraryLight")]
internal sealed class RaycastedArbitraryLight : Entity {
    private struct BeamNode {
        // Position at which the node reaches solid tiles
        // Let's just hope the tiles don't get changed :p
        public Vector2 MaximumPos;

        // The position at which the node begins, this has already escaped solids
        public Vector2 StartPos;
        public Vector2 Pos;
    }

    private struct BeamNodePosGetter : IFunc<BeamNode, Vector2> {
        public Vector2 Invoke(BeamNode arg) {
            return arg.Pos;
        }
    }
    
    
    private readonly bool _dynamic;
    private readonly BeamNode[] _nodes;
    private readonly ConditionHelper.Condition _dynamicFlag;
    
    private readonly ArbitraryLight _light;
    
    private Rectangle _levelBounds;

    private readonly List<Entity> _collisionCache = [];

    private Rectangle _maxNodeBounds;
    
    public RaycastedArbitraryLight(EntityData data, Vector2 offset) : base(data.Position + offset) {
        Add(_light = new ArbitraryLight(data, offset));
        Visible = true;
        Active = true;
        
        _dynamic = data.Bool("dynamic", false);
        _dynamicFlag = data.GetCondition("dynamicFlag");
        _levelBounds = data.Level.Bounds;
        
        if (data.Nodes is not [var n1, var n2])
            return;

        var offsetPerBeam = data.Float("offsetPerBeam", 1f);
        var angle = Calc.AngleToVector(Calc.Angle(n1, n2), offsetPerBeam);
        var dist = Vector2.Distance(n1, n2);
        var pos = n1 + offset;

        _nodes = new BeamNode[(int)(dist / offsetPerBeam) + 1];
        for (int i = 0; i < _nodes.Length; i++) {
            _nodes[i] = new() { Pos = pos };
            pos += angle;
        }
    }

    public override void Awake(Scene scene) {
        base.Awake(scene);

        FindMaximumPositions();
        UpdateVertexes();
    }

    public override void Update() {
        base.Update();

        if (_dynamic
            && CameraCullHelper.IsRectangleVisible(_maxNodeBounds)
            && _light.Visible
            && _dynamicFlag.Check(Scene.ToLevel().Session)) {
            UpdateVertexes();
        }
    }

    /// <summary>
    /// Find the maximum distance each node can move, dictated by solid tiles.
    /// </summary>
    private void FindMaximumPositions() {
        var from = Position;
        var nodes = _nodes;

        Rectangle maxRect = RectangleExt.CreateTruncating(Position.X, Position.Y, 1, 1);
        for (int i = 0; i < _nodes.Length; i++) {
            var pos = nodes[i].Pos;
            var angle = Calc.AngleToVector(Calc.Angle(from, pos), 1f);

            nodes[i].MaximumPos = LightUtils.RaycastOnlySolids(Scene as Level, ref _levelBounds, from, angle);
            nodes[i].StartPos = LightUtils.EscapeSolids(Scene as Level, ref _levelBounds, angle, from);
            
            maxRect = RectangleExt.Merge(maxRect,
                RectangleExt.CreateTruncating(nodes[i].MaximumPos.X, nodes[i].MaximumPos.Y, 1, 1));
        }

        _maxNodeBounds = maxRect;
    }
    
    private void UpdateVertexes() {
        var from = Position;
        var nodes = _nodes;
        
        // broad phase collision
        _collisionCache.Clear();
        Rectangle collisionBounds = default;
        DynamicRainGenerator.CollideInto(Scene, _maxNodeBounds, _collisionCache, typeof(Solid), ref collisionBounds);
        
        for (int i = 0; i < _nodes.Length; i++) {
            ref var node = ref nodes[i];
            node.Pos = LightUtils.RaycastUpTo(Scene as Level, ref _levelBounds, node.StartPos, node.MaximumPos, false, _collisionCache);
        }

        _light.UpdateNodes(from, nodes, default(BeamNodePosGetter));
    }
}

internal static class LightUtils {
    public static Vector2 RaycastAtAngle(Level level, ref Rectangle bounds, Vector2 from, Vector2 angle, bool escapeSolids, List<Entity> candidates) {
        var pos = from;

        if (escapeSolids)
            pos = EscapeSolids(level, ref bounds, angle, pos);

        var daqMult = 64f;
        while (bounds.Contains(new Point((int) pos.X, (int) pos.Y)) && daqMult > 4f) {
            var daqEnd = pos + (angle * daqMult);
            if (CollideExt.CollideFirst(pos, daqEnd, candidates) is null) {
                pos = daqEnd;
                break;
            }

            daqMult /= 2f;
        }

        while(bounds.Contains(new Point((int) pos.X, (int) pos.Y)) && CollideExt.CollideFirst(pos, candidates) is null) {
            pos += angle;
        }
        return pos;
    }

    public static Vector2 EscapeSolids(Level level, ref Rectangle bounds, Vector2 angle, Vector2 pos) {
        // if we're in a solid, first get out of it
        while (CollideCheckSolidTiles(level, ref bounds, pos))
        {
            pos += angle;
        }

        return pos;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsInBounds(ref Rectangle bounds, Vector2 at) {
        return bounds.Contains(new Point((int) at.X, (int) at.Y));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool CollideCheckSolidTiles(Level level, ref Rectangle bounds, Vector2 at) {
        if (!bounds.Contains(new Point((int) at.X, (int) at.Y)))
            return true;
        return level.CollideCheck<SolidTiles>(at);
    }

    public static Vector2 RaycastOnlySolids(Level level, ref Rectangle bounds, Vector2 from, Vector2 angle) {
        from = EscapeSolids(level, ref bounds, angle, from);
        while (IsInBounds(ref bounds, from) && !level.SolidTiles.Collider.Collide(from)) {
            from += angle;
        }

        return from;
    }

    internal static Vector2 RaycastUpTo(Level level, ref Rectangle bounds, Vector2 from, Vector2 maximumPos, bool escapeSolids,
        List<Entity> collisionCache) {
        var angle = Calc.AngleToVector(Calc.Angle(from, maximumPos), 4f);
        if (escapeSolids)
            from = EscapeSolids(level, ref bounds, angle, from);
        
        if (collisionCache.Count == 0) {
            return maximumPos;
        }

        if (collisionCache is [SolidTiles st] && st == level.SolidTiles)
            return maximumPos;
        
        return RaycastAtAngle(level, ref bounds, from, angle, false, collisionCache);
    }
}
