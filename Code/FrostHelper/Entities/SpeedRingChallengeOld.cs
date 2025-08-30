namespace FrostHelper.Entities;

[CustomEntity("FrostHelper/SpeedRingChallenge")]
internal sealed class SpeedRingChallengeOld : SpeedRingChallenge {
    private Vector2[] _nodes;
    private readonly float _width;
    private readonly float _height;
    private float _lerp;
    
    public SpeedRingChallengeOld(EntityData data, Vector2 offset, EntityID id) : base(data, offset, id)
    {
        _nodes = data.NodesOffset(offset);
        _width = data.Width;
        _height = data.Height;
        
        var last = _nodes.Last();
        Collider = new Hitbox(_width, _height, 0f, 0f);

        Depth = Depths.Top;
    }

    protected override Rectangle GetStrawberrySearchHitbox() {
        var last = _nodes.Last();

        return new Rectangle((int) last.X, (int) last.Y, (int) Width, (int) Height);
    }
    
    protected override void MoveToNextNode() {
        Collider.Position = _nodes[CurrentNodeId] - Position;
    }

    protected override Vector2 NodeCenterPos(int index) {
        return (CurrentNodeId == -1 ? Position : _nodes[CurrentNodeId]);
    }

    protected override int NodeCount() => _nodes.Length - (SpawnBerry ? 1 : 0);

    protected override bool CheckNodeCollision() => CollideCheck<Player>();

    protected override void RenderRing() {
        if (!Scene.ToLevel().Paused) {
            _lerp += 3f * Engine.DeltaTime;
            if (_lerp >= 1f) {
                _lerp = 0f;
            }
        }
        
        DrawRing(Collider.Center + Position);
    }

    protected override Vector2 ArrowPos() => Center;

    private void DrawRing(Vector2 position) {
        float maxRadiusY = MathHelper.Lerp(4f, Height / 2, _lerp);
        float maxRadiusX = MathHelper.Lerp(4f, Width, _lerp);
        Vector2 value = GetVectorAtAngle(0f);
        var color = GetRingColor(Engine.Scene);
        for (int i = 1; i <= 8; i++) {
            float radians = i * 0.3926991f;
            Vector2 vectorAtAngle = GetVectorAtAngle(radians);
            Draw.Line(position + value, position + vectorAtAngle, color);
            Draw.Line(position - value, position - vectorAtAngle, color);
            value = vectorAtAngle;
        }

        Vector2 GetVectorAtAngle(float radians) {
            Vector2 vector = Calc.AngleToVector(radians, 1f);
            Vector2 scaleFactor = new Vector2(
                MathHelper.Lerp(maxRadiusX, maxRadiusX * 0.5f, Math.Abs(Vector2.Dot(vector, Calc.AngleToVector(0f, 1f)))), 
                MathHelper.Lerp(maxRadiusY, maxRadiusY * 0.5f, Math.Abs(Vector2.Dot(vector, Calc.AngleToVector(0f, 1f)))));
            return vector * scaleFactor;
        }
    }
}