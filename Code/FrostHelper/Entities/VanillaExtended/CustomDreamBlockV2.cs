using FrostHelper.Helpers;
using System.Diagnostics.CodeAnalysis;

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

    internal readonly bool Connected;
    
    internal EquatableArray<DreamBlockParticleLayer> ParticleLayers { get; init; }

    private readonly bool _hasCustomParticleData;

    private static DreamBlockParticleLayer[] DefaultParticleLayers { get; } =
        new SpanParser(
            "6969697f;ffef11,ff00d0,08a310;0.3;3,2,1,0;1~9e9e9ebf;5fcde4,7fb25e,e0564c;0.55;1,2;2~d3d3d3ff;5b6ee1,cc3b3b,7daa64;0.80;2;3")
            .ParseList<DreamBlockParticleLayer>('~').ToArray();

    internal DreamBlockGroup? Group { get; set; }

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

        Depth = data.Int("depth", Depth);
        ActiveBackColor = ColorHelper.GetColor(data.Attr("activeBackColor", "Black"));
        DisabledBackColor = ColorHelper.GetColor(data.Attr("disabledBackColor", "1f2e2d"));
        ActiveLineColor = ColorHelper.GetColor(data.Attr("activeLineColor", "White"));
        DisabledLineColor = ColorHelper.GetColor(data.Attr("disabledLineColor", "6a8480"));
        DashSpeed = data.Float("speed", 240f);
        AllowRedirects = data.Bool("allowRedirects");
        AllowRedirectsInSameDir = data.Bool("allowSameDirectionDash");
        SameDirectionSpeedMultiplier = data.Float("sameDirectionSpeedMultiplier", 1f);
        node = data.FirstNodeNullable(offset);
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
        Connected = data.Bool("connected");
        ParticleLayers = data.ParseArray("particles", '~', DefaultParticleLayers);
        _hasCustomParticleData = data.Has("particles");
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
            tween.OnUpdate = t => {
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

    internal readonly struct OverrideDreamColors : IDisposable {
        private readonly CustomDreamBlockV2 _block;
        private readonly Color _prevBackColor, _prevLineColor;
        
        public OverrideDreamColors(CustomDreamBlockV2 block) {
            _block = block;
            if (block.playerHasDreamDash) {
                _prevBackColor = activeBackColor;
                _prevLineColor = activeLineColor;
                activeLineColor = block.ActiveLineColor;
                activeBackColor = block.ActiveBackColor;
            } else {
                _prevBackColor = disabledBackColor;
                _prevLineColor = disabledLineColor;
                disabledLineColor = block.DisabledLineColor;
                disabledBackColor = block.DisabledBackColor;
            }
        }
        
        public void Dispose() {
            if (_block.playerHasDreamDash) {
                activeLineColor = _prevLineColor;
                activeBackColor = _prevBackColor;
            } else {
                disabledLineColor = _prevLineColor;
                disabledBackColor = _prevBackColor;
            }
        }
    }

    internal Rectangle Bounds => RectangleExt.CreateTruncating(X, Y, Width, Height);
    
    public override void Render() {
        if (Connected) {
            return;
        }
        
        // Copy-paste of vanilla code, as we want to completely replace particle rendering and this is simpler than an IL hook.
        using var _ = new OverrideDreamColors(this);
        
        var camera = SceneAs<Level>().Camera;
        if (Right < (double) camera.Left || Left > (double) camera.Right || Bottom < (double) camera.Top || Top > (double) camera.Bottom)
            return;
        Draw.Rect(shake.X + X, shake.Y + Y, Width, Height, playerHasDreamDash ? activeBackColor : disabledBackColor);
        
        RenderParticles(Bounds, particles, new ConstTrueFilter<Vector2>());
        
        if (whiteFill > 0.0)
          Draw.Rect(X + shake.X, Y + shake.Y, Width, Height * whiteHeight, Color.White * whiteFill);
        WobbleLine(shake + new Vector2(X, Y), shake + new Vector2(X + Width, Y), 0.0f);
        WobbleLine(shake + new Vector2(X + Width, Y), shake + new Vector2(X + Width, Y + Height), 0.7f);
        WobbleLine(shake + new Vector2(X + Width, Y + Height), shake + new Vector2(X, Y + Height), 1.5f);
        WobbleLine(shake + new Vector2(X, Y + Height), shake + new Vector2(X, Y), 2.5f);
        Draw.Rect(shake + new Vector2(X, Y), 2f, 2f, playerHasDreamDash ? activeLineColor : disabledLineColor);
        Draw.Rect(shake + new Vector2((float) (X + (double) Width - 2.0), Y), 2f, 2f, playerHasDreamDash ? activeLineColor : disabledLineColor);
        Draw.Rect(shake + new Vector2(X, (float) (Y + (double) Height - 2.0)), 2f, 2f, playerHasDreamDash ? activeLineColor : disabledLineColor);
        Draw.Rect(shake + new Vector2((float) (X + (double) Width - 2.0), (float) (Y + (double) Height - 2.0)), 2f, 2f, playerHasDreamDash ? activeLineColor : disabledLineColor);
    }

    public override void Awake(Scene scene) {
        base.Awake(scene);

        if (!Connected || Group is not null)
            return;

        Group = new DreamBlockGroup(this);
        scene.Add(Group);
    }

    internal bool CanGroupWith(CustomDreamBlockV2 other) {
        return this != other
               && other.Connected
               && other.Group is null
               && Depth == other.Depth
               && Speed == other.Speed
               && SameDirectionSpeedMultiplier == other.SameDirectionSpeedMultiplier
               && ActiveBackColor == other.ActiveBackColor
               && ActiveLineColor == other.ActiveLineColor
               && DisabledBackColor == other.DisabledBackColor
               && DisabledLineColor == other.DisabledLineColor
               && moveSpeedMult == other.moveSpeedMult
               && ConserveSpeed == other.ConserveSpeed
               && AllowRedirects == other.AllowRedirects
               && AllowRedirectsInSameDir == other.AllowRedirectsInSameDir
               && ParticleLayers.Equals(other.ParticleLayers)
               && oneUse == other.oneUse;
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
        On.Celeste.DreamBlock.OneUseDestroy += DreamBlockOnOneUseDestroy;
        On.Celeste.DreamBlock.Setup += DreamBlockOnSetup;
    }

    [OnUnload]
    public static void Unload() {
        if (!_hooksLoaded)
            return;
        _hooksLoaded = false;

        On.Celeste.Player.DreamDashUpdate -= Player_DreamDashUpdate;
        On.Celeste.Player.DreamDashEnd -= Player_DreamDashEnd;
        IL.Celeste.Player.DreamDashBegin -= Player_DreamDashBegin;
        On.Celeste.DreamBlock.OneUseDestroy -= DreamBlockOnOneUseDestroy;
        On.Celeste.DreamBlock.Setup -= DreamBlockOnSetup;
    }
    
    private static void DreamBlockOnSetup(On.Celeste.DreamBlock.orig_Setup orig, DreamBlock self) {
        if (self is CustomDreamBlockV2 { _hasCustomParticleData: true } block) {
            block.particles = block.CreateCustomParticles(block.Bounds);
            return;
        }
        orig(self);
    }
    
    internal DreamParticle[] CreateCustomParticles(Rectangle bounds) {
        var random = VisualRandom.CreateAt(bounds.X, bounds.Y);
        var newParticles = new DreamParticle[(int) (bounds.Width / 8.0 * (bounds.Height / 8.0) * 0.699999988079071)];

        var layers = ParticleLayers;
        
        var weighedLayerIds = layers.Backing
            .SelectMany((x, i) => Enumerable.Repeat(i, x.Weight))
            .ToArray();
        
        foreach (ref var particle in newParticles.AsSpan())
        {
            particle.Position = new Vector2(random.NextFloat(bounds.Width), random.NextFloat(bounds.Height));
            particle.TimeOffset = random.NextFloat();
            particle.Layer = random.Choose(weighedLayerIds);
            var layerData = layers[particle.Layer];
            particle.Color = layerData.InactiveColor;
            if (playerHasDreamDash) {
                particle.Color = random.Choose(layerData.Colors.Backing);
            }
        }

        return newParticles;
    }
    
    internal void RenderParticles<TFilter>(Rectangle bounds, DreamParticle[] toRender, TFilter filter)
    where TFilter : IFunc<Vector2, bool> {
        // TODO: use a shader for particle rendering with masking to block size.
        Vector2 camPos = SceneAs<Level>().Camera.Position;
        var layers = ParticleLayers;
        
        foreach (ref var particle in toRender.AsSpan())
        {
            int layer = particle.Layer;
            var layerData = layers[layer];
            
            Vector2 particlePos = PutInside(bounds, particle.Position + camPos * layerData.Parallax);
            if (!(particlePos.X >= bounds.X + 2.0) || !(particlePos.Y >= bounds.Y + 2.0) ||
                !(particlePos.X < bounds.Right - 2.0) || !(particlePos.Y < bounds.Bottom - 2.0))
                continue;
            
            if (!filter.Invoke(particlePos + new Vector2(-2f, 0f)) 
                || !filter.Invoke(particlePos + new Vector2(2f, 0f))
                || !filter.Invoke(particlePos + new Vector2(0f, 2f))
                || !filter.Invoke(particlePos + new Vector2(0f, -2f))
               )
                continue;

            var frameCount = (double)layerData.Frames.Length;
            var particleTexture = particleTextures[layerData.Frames[(int)((particle.TimeOffset * frameCount + animTimer) % frameCount)]];
            particleTexture.DrawCentered(particlePos + shake, particle.Color);
        }
    }
    
    private static Vector2 PutInside(Rectangle bounds, Vector2 pos) {
        var right = bounds.Right;
        var width = bounds.Width;
        var left = bounds.Left;
        var height = bounds.Height;
        var bottom = bounds.Bottom;
        var top = bounds.Top;
        
        if (pos.X > right)
            pos.X -= float.Ceiling((pos.X - right) / width) * width;
        else if (pos.X < left)
            pos.X += float.Ceiling((left - pos.X) / width) * width;
        if (pos.Y > bottom)
            pos.Y -= float.Ceiling((pos.Y - bottom) / height) * height;
        else if (pos.Y < top)
            pos.Y += float.Ceiling((top - pos.Y) / height) * height;
        return pos;
    }
    
    /// <summary>
    /// If a one-use dream block is removed, also remove all connected blocks.
    /// </summary>
    private static void DreamBlockOnOneUseDestroy(On.Celeste.DreamBlock.orig_OneUseDestroy orig, DreamBlock self) {
        if (self is not CustomDreamBlockV2 block) {
            orig(self);
            return;
        }

        if (block.Group is { } group) {
            group.DestroyAll();
        }
        
        orig(self);
    }

    private static void Player_DreamDashBegin(ILContext il) {
        ILCursor cursor = new ILCursor(il);

        while (cursor.TryGotoNextBestFitLogged(MoveType.After, instr => instr.MatchStfld<Player>(nameof(Player.Speed)))) {
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

internal sealed class DreamBlockGroup : Entity {
    private Rectangle _bounds;
    private HashSet<CustomDreamBlockV2> _blocks = [];

    private bool _baked;

    private List<DreamLine> _lines = [];
    private List<DreamCorner> _corners = [];

    private readonly CustomDreamBlockV2 _leader;

    private DreamBlock.DreamParticle[] _particles;
    private byte[] _existMap;
    private int _gridHeight, _gridWidth;

    public DreamBlockGroup(CustomDreamBlockV2 baseBlock) {
        _leader = baseBlock;
        Depth = baseBlock.Depth;
        Visible = true;
        Active = true;
        
        var possibleBlocks = _leader.Scene.Tracker.SafeGetEntities<CustomDreamBlockV2>()
            .Cast<CustomDreamBlockV2>()
            .Where(x => x.Connected && x.CanGroupWith(_leader))
            .ToList();
        AddBlock(baseBlock, possibleBlocks);
    }

    private void SetupParticles() {
        _particles = _leader.CreateCustomParticles(_bounds);
    }

    private void AddBlock(CustomDreamBlockV2 block, List<CustomDreamBlockV2> possibleBlocks) {
        _blocks.Add(block);
        Depth = block.Depth;
        block.Group = this;
        
        _bounds = _bounds == default ? block.Collider.Bounds : RectangleExt.Merge(_bounds, block.Collider.Bounds);
        
        // Find all possible connections with this new block
        FindInGroup(block, possibleBlocks);
    }

    private void FindInGroup(CustomDreamBlockV2 block, List<CustomDreamBlockV2> possibleBlocks) {
        foreach (CustomDreamBlockV2 other in possibleBlocks)
        {
            if (other == block)
                continue;
            
            if (_blocks.Contains(other))
                continue;
            
            if (other.CollideRect(new Rectangle((int)block.X - 1, (int)block.Y, (int)block.Width + 2, (int)block.Height)) 
             || other.CollideRect(new Rectangle((int)block.X, (int)block.Y - 1, (int)block.Width, (int)block.Height + 2)))
            {
                AddBlock(other, possibleBlocks);
            }
        }
    }

    private bool IsBlockAt(Vector2 pos) {
        var gridX = Snap8((int)pos.X - _bounds.X);
        var gridY = Snap8((int)pos.Y - _bounds.Y);
        
        return gridX >= 0 && gridY >= 0 && gridX < _gridWidth && gridY < _gridHeight &&
               _existMap[gridY * _gridWidth + gridX] > 0;
    }

    private readonly struct IsBlockAtFilter(DreamBlockGroup self) : IFunc<Vector2, bool> {
        public bool Invoke(Vector2 pos) {
            return self.IsBlockAt(pos);
        }
    }
    
    private void RenderParticles() {
        _leader.RenderParticles(_bounds, _particles, new IsBlockAtFilter(this));
    }
    
    public override void Render() {
        using var _ = new CustomDreamBlockV2.OverrideDreamColors(_leader);

        if (!_baked)
            Bake();
        
        base.Render();
        
        if (!CameraCullHelper.IsRectangleVisible(_bounds, Scene))
            return;

        var shake = _leader.shake;
        var playerHasDreamDash = _leader.playerHasDreamDash;
        var backColor = playerHasDreamDash ? _leader.ActiveBackColor : _leader.DisabledBackColor;
        var fastDraw = new FastDraw();

        foreach (var block in _blocks) {
            if (CameraCullHelper.IsRectangleVisible(block.Bounds, Scene))
                fastDraw.Rect(shake.X + block.X, shake.Y + block.Y, block.Width, block.Height, backColor);
        }

        RenderParticles();

        var lineColor = playerHasDreamDash ? _leader.ActiveLineColor : _leader.DisabledLineColor;
        foreach (var line in _lines) {
            if (CameraCullHelper.IsLineVisible(_leader.shake + line.Start, _leader.shake + line.End))
                _leader.WobbleLine(_leader.shake + line.Start, _leader.shake + line.End, line.Direction switch {
                    DreamLine.Dir.Top => 0f,
                    DreamLine.Dir.Right => 0.7f,
                    DreamLine.Dir.Bottom => 1.5f,
                    DreamLine.Dir.Left => 2.5f,
                    _ => 0f
                });
        }

        foreach (var corner in _corners) {
            fastDraw.Rect(_leader.shake + new Vector2(corner.Position.X, corner.Position.Y), corner.Width, 2f, lineColor);
        }
    }

    private void Bake() {
        _baked = true;

        var w = _gridWidth = _bounds.Width / 8;
        var h = _gridHeight = _bounds.Height / 8;
        
        var offset = new Vector2(_bounds.X, _bounds.Y);

        Span<byte> existMap = _existMap = new byte[w*h];
        foreach (var b in _blocks) {
            var rect = b.Collider.Bounds;
            var blockWidth = Snap8(rect.Width);
            var blockHeight = Snap8(rect.Height);
            var sx = Snap8(rect.X - _bounds.X);
            var sy = Snap8(rect.Y - _bounds.Y);

            for (int ry = 0; ry < blockHeight; ry++) {
                var y = sy + ry;
                existMap.Slice((y * w) + sx, blockWidth).Fill(1);
            }
            
            /*
            for (int y = 0; y < h; y++) {
                for (int x = 0; x < w; x++) {
                    Console.Write(existMap[(y * w) + x]);
                }

                Console.WriteLine();
            }
            Console.WriteLine("-----");
            */
        }

        /*
        for (int y = 0; y < h; y++) {
            for (int x = 0; x < w; x++) {
                Console.Write(existMap[(y * w) + x]);
            }

            Console.WriteLine();
        }
        */

        var lines = _lines;
        var corners = _corners;
        
        // Horizontal lines
        for (int y = 0; y < h; y++) {
            var row = existMap.Slice(y * w, w);
            
            // Top lines
            for (int x = 0; x < w; x++) {
                if (row[x] == 0)
                    continue;
                
                var start = new Vector2(x * 8, y * 8) + offset;
                // If there's a tile above, there's no top line.
                if (y != 0 && existMap[(y - 1) * w + x] > 0)
                    continue;
                
                // Top-Left corner
                var topLeftCorner = new DreamCorner(start);
                // Inner corner: (x,y is at '-' here:)
                //  | 
                //  X-
                if (x > 0 && row[x - 1] > 0 && y > 0 && existMap[(y - 1) * w + (x - 1)] > 0) {
                    topLeftCorner = new DreamCorner(start + new Vector2(-2f, 0f), Width: 4);
                }
                corners.Add(topLeftCorner);
                
                // Expand the line as far right as possible.
                var len = 1;
                while (x < w - 1 && row[x + 1] > 0 && (y == 0 || existMap[(y - 1) * w + (x + 1)] == 0)) {
                    len++;
                    x++;
                }

                var end = start + new Vector2(len * 8f, 0f);
                // Top-Right corner
                var topRightCorner = new DreamCorner(end + new Vector2(-2f, 0f));
                // Inner corner: (x,y is at '-' here:)
                //  |
                // -X
                if (x < w - 1 && row[x + 1] > 0 && y > 0 && existMap[(y - 1) * w + (x + 1)] > 0) {
                    topRightCorner = topRightCorner with { Width = 4 };
                }
                corners.Add(topRightCorner);
                
                lines.Add(new DreamLine(start, end, DreamLine.Dir.Top));
            }
            
            // Bottom lines
            for (int x = 0; x < w; x++) {
                if (row[x] == 0)
                    continue;
                
                var start = new Vector2(x * 8, (y * 8) + 8) + offset;
                // If there's a tile below, there's no bottom line.
                if (y != h - 1 && existMap[(y + 1) * w + x] > 0)
                    continue;
                
                
                // Bottom-Left corner
                var botLeftCorner = new DreamCorner(start + new Vector2(0f, -2f));
                // Inner corner: (x,y is at '-' here:)
                //  X-
                //  |
                if (x > 0 && row[x - 1] > 0 && y + 1 < h && existMap[(y + 1) * w + (x - 1)] > 0) {
                    botLeftCorner = new DreamCorner(start + new Vector2(-2f, -2f), Width: 4);
                }
                corners.Add(botLeftCorner);

                // Expand the line as far right as possible.
                var len = 1;
                while (x < w - 1 && row[x + 1] > 0 && (y == h - 1 || existMap[(y + 1) * w + (x + 1)] == 0)) {
                    len++;
                    x++;
                }
                
                // Bottom-Right corner
                var end = start + new Vector2(len * 8f, 0f);
                var botRightCorner = new DreamCorner(end + new Vector2(-2f, -2f));
                // Inner corner: (x,y is at '-' here:)
                //  -X
                //   |
                if (x < w - 1 && row[x + 1] > 0 && y + 1 < h && existMap[(y + 1) * w + (x + 1)] > 0) {
                    botRightCorner = botRightCorner with { Width = 4 };
                }
                corners.Add(botRightCorner);
                
                lines.Add(new DreamLine(end, start, DreamLine.Dir.Bottom));
            }
        }
        
        // Vertical lines
        for (int x = 0; x < w; x++) {
            // Left lines
            for (int y = 0; y < h; y++) {
                var yIdx = y * w;
                if (existMap[yIdx + x] == 0)
                    continue;
                
                var start = new Vector2(x * 8, y * 8) + offset;
                // If there's a tile to the left, there's no left line.
                if (x != 0 && existMap[yIdx + x - 1] > 0)
                    continue;
                
                // Top-Left corner
                corners.Add(new DreamCorner(start));
                
                // Expand the line as far below as possible.
                var len = 1;
                while (y < h - 1 && existMap[(y+1)*w + x] > 0 && (x == 0 || existMap[(y+1)*w + x - 1] == 0)) {
                    len++;
                    y++;
                }

                var end = start + new Vector2(0f, len * 8f);
                // Bottom-Left corner
                corners.Add(new DreamCorner(end + new Vector2(0f, -2f)));
                
                lines.Add(new DreamLine(end, start, DreamLine.Dir.Left));
            }
            
            // Right lines
            for (int y = 0; y < h; y++) {
                var yIdx = y * w;
                if (existMap[yIdx + x] == 0)
                    continue;
                
                var start = new Vector2((x+1) * 8, y * 8) + offset;
                // If there's a tile to the right, there's no right line.
                if (x + 1 < w && existMap[yIdx + x + 1] > 0)
                    continue;
                
                // Top-Right corner
                corners.Add(new DreamCorner(start + new Vector2(-2f, 0f)));
                
                // Expand the line as far below as possible.
                var len = 1;
                while (y < h - 1 && existMap[(y+1)*w + x] > 0 && (x + 1 >= w || existMap[(y+1)*w + x + 1] == 0)) {
                    len++;
                    y++;
                }

                var end = start + new Vector2(0f, len * 8f);
                // Bottom-Right corner
                corners.Add(new DreamCorner(end + new Vector2(-2f, -2f)));
                
                lines.Add(new DreamLine(start, end, DreamLine.Dir.Right));
            }
        }

        SetupParticles();
    }

    private int Snap8(int x) => x / 8;
    
    public void DestroyAll() {
        foreach (var b in _blocks) {
            b.Group = null;
            b.OneUseDestroy();
        }
        _blocks.Clear();
        Visible = false;
        RemoveSelf();
    }

    private record struct DreamLine(Vector2 Start, Vector2 End, DreamLine.Dir Direction) {
        public enum Dir {
            Top,
            Bottom,
            Left,
            Right
        }
    }

    private record struct DreamCorner(Vector2 Position, int Width = 2);
}

internal record DreamBlockParticleLayer(Color InactiveColor, EquatableArray<Color> Colors, float Parallax, EquatableArray<int> Frames, int Weight) 
    : IDetailedParsable<DreamBlockParticleLayer> {
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out DreamBlockParticleLayer result,
        [NotNullWhen(false)] out string? errorMessage) {

        result = null;
        var parser = new SpanParser(s);
        
        //inactiveColor;color1,color2,..;parallax;frames;weight
        if (!parser.ReadUntil<RgbaOrXnaColor>(';').TryUnpack(out var inactiveColor)
            || !parser.SliceUntil(';').TryUnpack(out var colorsParser)
            || !colorsParser.TryParseList<RgbaOrXnaColor>(',', out var colors)
            || !parser.ReadUntil<float>(';').TryUnpack(out var parallax)
            || !parser.ReadUntil<CsvArrayWithTricks>(';').TryUnpack(out var frames)
            || !parser.ReadUntil<int>(';').TryUnpack(out var weight)
            || !parser.IsEmpty
            )
        {
            errorMessage = $"Failed to parse {s} as a {typeof(DreamBlockParticleLayer)}.";
            return false;
        }

        errorMessage = null;
        result = new DreamBlockParticleLayer(inactiveColor.Color, colors.Select(x => x.Color).ToArray(),
            parallax, frames.Array, weight);

        return true;
    }
}
