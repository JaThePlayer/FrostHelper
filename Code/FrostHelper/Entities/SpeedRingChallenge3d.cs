using Celeste.Mod.CommunalHelper.Components;
using FrostHelper.Helpers;
using FrostHelper.ModIntegration;
using System.Runtime.CompilerServices;

namespace FrostHelper.Entities;

[CustomEntity("FrostHelper/SpeedRingChallenge3d")]
internal sealed class SpeedRingChallenge3d : SpeedRingChallenge {
    private readonly List<Node> _nodes;
    private readonly Vector2[] _dataNodes;
    private readonly bool _showAllRings;
    private readonly float _rainbowMix = 0.1f;
    
    private class Node {
        public required Vector2 A;
        public required Vector2 B;
        public required Vector2 Direction;
        public required Matrix Orientation;
        public required object Mesh;
        public required Component Back, Front;
        
        public Vector2 Center => (A + B) / 2f;
    }
    
    public SpeedRingChallenge3d(EntityData data, Vector2 offset, EntityID id) : base(data, offset, id) {
        if (!CommunalHelperIntegration.LoadIfNeeded() || !CommunalHelperIntegration.Available) {
            PostcardHelper.Start(
                "3D Speed Ring Challenge used, but Communal Helper is disabled. Report this missing dependency to the mapmaker!");
            return;
        }

        _nodes = [];
        _dataNodes = data.GetNodesWithOffsetWithPositionPrepended(offset);
        if (_dataNodes.Length % 2 == 0 && SpawnBerry) {
            NotificationHelper.Notify($"3D Speed Ring '{ChallengeNameId}' is missing a node to search for berry rewards!");
        }
        CurrentNodeId = 0;
        _showAllRings = data.Bool("showAllRings", false);
    }

    public override void Added(Scene scene) {
        CreateNodes(_dataNodes, scene);
        base.Added(scene);
    }

    private Vector4 GetFrontTint(Color ringColor) => ringColor.ToVector4();
    private Vector4 GetBackTint(Color ringColor) => new(ringColor.ToVector3() * 0.5f, 1.0f);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void CreateNodes(Vector2[] dataNodes, Scene scene) {
        for (int i = 0; i + 1 < dataNodes.Length; i += 2) {
            var a = dataNodes[i];
            var b = dataNodes[i+1];
            var center = (a + b) / 2f;
            
            var direction = (a - b).Perpendicular().SafeNormalize();
            Matrix tilt = Matrix.CreateRotationY(0.25f);
            var orientation = Matrix.CreateRotationZ(-direction.Angle());

            var pos = new Vector3(center - Position, 0f);
            var ringColor = GetRingColor(scene);
            var mesh = CommunalHelperShapes.HalfRing(Vector2.Distance(a, b), /*4*/6.0f, Color.White);
            var frontMatrix = tilt * orientation;
            var texture = GFX.Game["frostHelper/speedRing3d/texture"].Texture.Texture; // CommunalHelperGFX.Blank
            
            Component front = new Shape3D(mesh) {
                Depth = Depths.FGTerrain,
                HighlightStrength = 0.8f,
                NormalEdgeStrength = 0.0f,
                RainbowMix = _rainbowMix,
                Texture = texture,
                Matrix = frontMatrix,
                Position = pos,
                Visible = i == 0 || _showAllRings,
                Tint = GetFrontTint(ringColor)
            };
            Component back = new Shape3D(mesh) {
                Depth = Depths.BGTerrain,
                HighlightStrength = 0.4f,
                NormalEdgeStrength = 0.0f,
                RainbowMix = _rainbowMix,
                Texture = texture,
                Matrix = Matrix.CreateRotationX(MathHelper.Pi) * frontMatrix,
                Position = pos,
                Visible = i == 0 || _showAllRings,
                Tint = GetBackTint(ringColor),
            };
            
            Add(front);
            Add(back);
            
            _nodes.Add(new Node {
                A = a,
                B = b,
                Mesh = mesh,
                Back = back,
                Front = front,
                Direction = direction,
                Orientation = orientation,
            });
        }
    }

    protected override Rectangle GetStrawberrySearchHitbox() {
        var last = _dataNodes[^1].ToPoint();

        return new Rectangle(last.X - 4, last.Y - 4, 8, 8);
    }

    protected override void MoveToNextNode() {
        if (CurrentNodeId > 0) {
            var prevNode = _nodes[CurrentNodeId - 1];
            prevNode.Front.Visible = false;
            prevNode.Back.Visible = false;
        }
        
        var node = _nodes[CurrentNodeId];
        node.Front.Visible = true;
        node.Back.Visible = true;
    }

    protected override Vector2 NodeCenterPos(int index) => _nodes[index].Center;

    protected override int NodeCount() => _nodes.Count;

    private void UpdateNode(Node node, bool current) {
        var front = (Shape3D)node.Front;
        var back = (Shape3D)node.Back;
        
        Matrix m = Matrix.CreateRotationY(0.25f + (float) Math.Sin(Scene.TimeActive * 3f) * 0.1f) * node.Orientation;
        front.Matrix = m;
        back.Matrix = Matrix.CreateRotationX(MathHelper.Pi) * m;

        if (current) {
            var ringColor = GetRingColor(Scene);
            front.Tint = GetFrontTint(ringColor);
            back.Tint = GetBackTint(ringColor);
            front.RainbowMix = _rainbowMix;
            back.RainbowMix = _rainbowMix;
        } else {
            var ringColor = Color.DarkGray;
            front.Tint = GetFrontTint(ringColor);
            back.Tint = GetBackTint(ringColor);
            front.RainbowMix = 0f;
            back.RainbowMix = 0f;
        }
        
        //GFX.Game["frostHelper/speedRing3d/dot"]
        //    .DrawCentered(node.Center, Color.White * 0.6f, 1f + (float) Math.Sin(Scene.TimeActive * 3) * 0.1f, 0f/*MathHelper.PiOver4 + node.Direction.Angle()*/);
    }
    
    protected override void RenderRing() {
        if (_showAllRings) {
            for (int i = CurrentNodeId; i < _nodes.Count; i++) {
                UpdateNode(_nodes[i], i == CurrentNodeId);
            }
        } else {
            UpdateNode(_nodes[CurrentNodeId], true);
        }
    }

    protected override Vector2 ArrowPos() => CurrentNodeId >= 0 ? _nodes[CurrentNodeId].Center : Vector2.Zero;

    protected override bool CheckNodeCollision() {
        if (CurrentNodeId < 0 || Scene.Tracker.GetEntity<Player>() is not {} player)
            return false;
        
        var node = _nodes[CurrentNodeId];
        return player.Collider.Collide(node.A, node.B);
    }
}