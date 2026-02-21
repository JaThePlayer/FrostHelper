using FrostHelper.Helpers;
using FrostHelper.ModIntegration;

namespace FrostHelper;

[CustomEntity("FrostHelper/HeldRefill")]
public class HeldRefill : Entity {
    public Vector2[] Nodes;
    public float SpeedMult;

    public VertexLight Light;
    public BloomPoint Bloom;
    public Sprite Sprite;
    public Sprite Flash;

    private readonly Image? _respawnImage;
    public SineWave SpriteSineWave;

    public float TravelPercent;

    private readonly Color _lineColor;

    private readonly Vector2 _startPosition;
    
    private readonly CustomLightningRenderer.Config _lightningConfig;
    private readonly CustomLightningRenderer.Edge[]? _edges;

    private CustomLightningRenderer? _renderer;
    
    private readonly float _respawnTime;
    private float _respawnTimer;

    private readonly string _respawnSfx;

    private bool OneUse => _respawnTime < 0f;
    
    private readonly LegacyOptions _legacyOption;

    [Flags]
    private enum LegacyOptions {
        Original = 0,
        FixGravityHelper = 1,
        NewVisuals = 2,
        
        /// <summary>
        /// Most up-to-date, default in Loenn
        /// </summary>
        Modern = FixGravityHelper,
    }
    
    internal static readonly EquatableArray<CustomLightningRenderer.Config.BoltConfig> DefaultBolts = new([
        new(Calc.HexToColor("ffff00"), 1f),
    ]);

    public HeldRefill(EntityData data, Vector2 offset) : base(data.Position + offset) {
        _startPosition = Position;
        _legacyOption = data.Enum("legacyOptions", LegacyOptions.Original);
        var directory = data.Attr("directory", "objects/refill").TrimEnd('/');
        _lineColor = data.GetColor("lineColor", "ffff00");
        _respawnTime = data.Float("respawnTime", -1f);
        _respawnSfx = data.Attr("respawnSfx", "event:/game/general/diamond_return");
        
        if (data.Nodes[0] == data.Position) {
            Nodes = data.NodesOffset(offset);
        } else {
            var dataNodes = data.Nodes;
            Nodes = new Vector2[dataNodes.Length + 1];
            Nodes[0] = data.Position + offset;
            for (int i = 0; i < dataNodes.Length; i++) {
                Nodes[i + 1] = dataNodes[i] + offset;
            }
        }

        if (_legacyOption.HasFlag(LegacyOptions.NewVisuals)) {
            _lightningConfig = new CustomLightningRenderer.Config(
                data.Attr("group", "heldRefills"),
                AffectedByBreakerBoxes: false, 
                data.ParseArray("bolts", ';', DefaultBolts.Backing).ToArray(),
                Depth, 
                FillColor: Color.White
            );

            var fakeEntity = new Entity();
            _edges = new CustomLightningRenderer.Edge[Nodes.Length];
            for (int i = 1; i < Nodes.Length; i++) {
                _edges[i] = new CustomLightningRenderer.Edge(fakeEntity, Nodes[i - 1], Nodes[i]);
            }
            _edges[0] = new CustomLightningRenderer.Edge(fakeEntity, Position, Nodes[0]);
        }

        SpeedMult = Math.Max(data.Float("speed", 6f), 0);

        Add(Sprite = new Sprite(GFX.Game, $"{directory}/idle"));
        Sprite.AddLoop("idle", "", 0.1f);
        Sprite.Play("idle", false, false);
        Sprite.CenterOrigin();

        Add(Flash = new Sprite(GFX.Game, $"{directory}/flash"));
        Flash.Add("flash", "", 0.05f);
        Flash.OnFinish = _ => Flash.Visible = false;
        Flash.CenterOrigin();

        if (!OneUse) {
            _respawnImage = new Image(GFX.Game[$"{directory}/outline"]);
            _respawnImage.CenterOrigin();
            _respawnImage.Visible = false;
            Add(_respawnImage);
        }

        Add(Bloom = new BloomPoint(0.8f, 16f));
        Add(Light = new VertexLight(Color.White, 1f, 16, 48));
        Add(new MirrorReflection());
        if (!_legacyOption.HasFlag(LegacyOptions.NewVisuals))
            Add(new CustomBloom(() => RenderPath(true)));

        Add(SpriteSineWave = new SineWave(0.6f, 0f));
        SpriteSineWave.Randomize();
        UpdateY();

        Depth = -100;

        Collider = data.Collider("hitbox") ?? new Hitbox(24f, 24, -12f, -12f);
        Add(new PlayerCollider(OnPlayer, null, null));
    }

    private CustomLightningRenderer GetOrAddLightningRenderer(Scene scene) =>
        _renderer ??= CustomLightningRenderer.GetOrCreate(scene, _lightningConfig);

    public override void Added(Scene scene) {
        base.Added(scene);

        if (_edges is { } edges) {
            var renderer = GetOrAddLightningRenderer(scene);
            foreach (var item in edges)
                renderer.Add(item);
        }
    }
    
    public override void Removed(Scene scene) {
        base.Removed(scene);

        if (_edges is { } edges) {
            var renderer = GetOrAddLightningRenderer(scene);
            foreach (var edge in edges)
                renderer.Remove(edge);
        }
    }

    public override void Update() {
        base.Update();

        if (_respawnTimer > 0f) {
            _respawnTimer -= Engine.DeltaTime;
            if (_respawnTimer <= 0f) {
                Respawn();
            }
        }

        if (Sprite.Visible && Scene.OnInterval(0.1f)) {
            (Scene as Level)!.ParticlesFG.Emit(_pGlow, 1, Position, Vector2.One * 5f);
        }
        UpdateY();
        Light.Alpha = Calc.Approach(Light.Alpha, Sprite.Visible ? 1f : 0f, 4f * Engine.DeltaTime);
        Bloom.Alpha = Light.Alpha * 0.8f;
        if (Sprite.Visible && Scene.OnInterval(2f)) {
            Flash.Play("flash", true, false);
            Flash.Visible = true;
        }

        _respawnImage?.RenderPosition = _startPosition;
    }

    public override void Render() {
        if (!OneUse)
            RenderRespawnIndicatorPath();
        
        if (Sprite.Visible) {
            Sprite.DrawOutline(1);
            
            if (!_legacyOption.HasFlag(LegacyOptions.NewVisuals))
                RenderPath(false);
            else
                UpdateEdges();
        }

        base.Render();
    }

    private void UpdateEdges() {
        if (_edges is not {} edges)
            return;

        int startIndex = PercentageToIndex(TravelPercent);
        for (int i = 0; i < Nodes.Length; i++) {
            edges[i].ForcedInvisible = i < startIndex;
            var start = i > 0 ? Nodes[i - 1] : _startPosition;
            var end = Nodes[i];
            if (i == startIndex && TravelPercent > 0f) {
                float percent = TravelPercent - (float) Math.Floor(TravelPercent);
                
                float angle = Calc.Angle(end, start);
                float fullLength = Vector2.Distance(end, start);
                var angleVec = Calc.AngleToVector(angle, (float) Math.Floor(fullLength * (percent)));

                edges[i].A = CenterLinePos(start) - angleVec;
            } else {
                edges[i].A = CenterLinePos(start);
            }
            edges[i].B = CenterLinePos(end);
        }
    }
    
    private void RenderPath(bool bloom) {
        int startIndex = PercentageToIndex(TravelPercent) - 1;
        // Draw the path
        for (int i = startIndex; i < Nodes.Length - 1; i++) {
            if (i == startIndex && TravelPercent > 0f) {
                float percent = TravelPercent - (float) Math.Floor(TravelPercent);
                float angle = Calc.Angle(Nodes[i + 1], Nodes[i]);
                float fullLength = Vector2.Distance(Nodes[i + 1], Nodes[i]);

                Draw.LineAngle(CenterLinePos(Nodes[i + 1]), angle, (float) Math.Floor(fullLength * (1f - percent)), _lineColor);
            } else {
                Draw.Line(CenterLinePos(Nodes[i]), CenterLinePos(Nodes[i + 1]), bloom ? Color.White * 0.3f : _lineColor, bloom ? 3 : 1);
            }
        }
    }
    
    private void RenderRespawnIndicatorPath() {
        var animOffset = Position.Length() + Scene.TimeActive * 50f;
        for (int i = 0; i < Nodes.Length - 1; i++) {
            float percent = i;
            if (percent >= TravelPercent)
                break;
            var start = Nodes[i];
            var end = Nodes[i + 1];
            float angle = Calc.Angle(start, end);
            float fullLength = Vector2.Distance(end, start);
            const float dotLen = 4f;
            const float dotGap = 2f;
            var angleVecBetweenDots = Calc.AngleToVector(angle, dotLen + dotGap);

            var steps = float.Ceiling(fullLength / (dotLen + dotGap));
            var actualSteps = 0;
            var pos = CenterLinePos(start);
            var target = CenterLinePos(end);
            if (i == 0)
                pos = Calc.Approach(pos, target, dotLen + dotGap);
            
            while (pos != target) {
                float gradientSize = 180f;
                var col = 0.6f + Calc.YoYo((animOffset + (percent * 30f)) % gradientSize / gradientSize) * 0.4f;
                
                Draw.LineAngle(pos, angle, float.Min(dotLen, Vector2.Distance(pos, target)),  ColorHelper.MultiplyWithoutAlpha(Color.White, col));
                pos = Calc.Approach(pos, target, dotLen + dotGap);
                percent += 1f / steps;
                actualSteps++;
                
                if (percent >= TravelPercent)
                    break;
            }
        }
    }

    private Vector2 CenterLinePos(Vector2 pos) {
        return pos;
    }

    private IEnumerator RefillRoutine(Player player) {
        yield return null;
        var level = (Scene as Level)!;
        level.Shake(0.3f);
        Sprite.Visible = (Flash.Visible = false);

        Depth = 8999;
        yield return 0.05f;
        float angle = player.Speed.Angle();
        level.ParticlesFG.Emit(_pShatter, 5, Position, Vector2.One * 4f, angle - 1.57079637f);
        level.ParticlesFG.Emit(_pShatter, 5, Position, Vector2.One * 4f, angle + 1.57079637f);
        SlashFx.Burst(Position, angle);

        if (OneUse) {
            RemoveSelf();
        } else {
            Position = _startPosition;
            _respawnTimer = _respawnTime;
        }
    }
    
    private void Respawn()
    {
        if (Collidable || OneUse)
            return;

        TravelPercent = 0f;
        Collidable = true;
        Sprite.Visible = true;
        _respawnImage!.Visible = false;
        Depth = -100;
        Audio.Play(_respawnSfx, Position);
        Scene.ToLevel().ParticlesFG.Emit(_pRegen, 16, Position, Vector2.One * 2f);
    }
    
    public void OnPlayer(Player player) {
        if (AnyDashPressed() && player.Holding is null) {
            Input.Dash.ConsumePress();
            Input.CrouchDash.ConsumePress();
            Collidable = false;
            SetHeldRefillUsedByPlayer(player, this);
            player.StateMachine.State = HeldDashState;
            player.RefillDash();
        }
    }

    public void UpdateY() {
        Flash.Y = Sprite.Y = Bloom.Y = SpriteSineWave.Value * 2f;
    }

    public int PercentageToIndex(float percent) {
        return ((int) Math.Floor(percent))/* % (Nodes.Length - 1)*/ + 1;
    }


    private readonly ParticleType _pGlow = Refill.P_Glow;
    private readonly ParticleType _pShatter = Refill.P_Shatter;
    private readonly ParticleType _pRegen = Refill.P_Regen;

    #region State
    public static int HeldDashState = int.MaxValue;

    public static void HeldDashBegin(Entity e) {
        Player player = (e as Player)!;
        var refill = GetHeldRefillUsedByPlayer(player);
        if (refill is null)
            return;
        
        player.Position = refill.Position;
        player.Speed = Vector2.Zero;
        
        refill._respawnImage?.Visible = true;
    }
    public static int HeldDashUpdate(Entity e) {
        Player player = (e as Player)!;
        var refill = GetHeldRefillUsedByPlayer(player);
        if (refill is null)
            return Player.StNormal;
        //player.Speed = Vector2.Zero;
        //var tunnelNode = TunnelNodes[i];

        int index = refill.PercentageToIndex(refill.TravelPercent);
        Vector2 start = refill.Nodes[index - 1];
        Vector2 end = refill.Nodes[index];

        float travelPercentDelta = Engine.DeltaTime * (refill.SpeedMult / (Vector2.Distance(start, end) / 64f));
        refill.TravelPercent += travelPercentDelta;

        player.Speed = Calc.AngleToVector(Calc.Angle(start, end), Vector2.Distance(start, end) * travelPercentDelta / Engine.DeltaTime).Floor();

        if (refill.TravelPercent > refill.Nodes.Length - 1) {
            return Player.StNormal;
        }

        if (refill.PercentageToIndex(refill.TravelPercent) != index) {
            index = refill.PercentageToIndex(refill.TravelPercent);
            start = refill.Nodes[index - 1];
            end = refill.Nodes[index];

            player.Position = start;
            player.Speed = Calc.AngleToVector(Calc.Angle(start, end), Vector2.Distance(start, end) * travelPercentDelta / Engine.DeltaTime).Floor();
        }

        if (refill._legacyOption.HasFlag(LegacyOptions.FixGravityHelper)) {
            player.Speed = GravityHelperIntegration.InvertIfPlayerInverted(player.Speed);
        }
        
        refill.Position = player.Position + player.Speed * Engine.DeltaTime;

        if (player.OnGround() && player.CanUnDuck) {
            if (Input.Jump.Pressed && player.jumpGraceTimer > 0f) {
                player.Jump(true, true);
            }

            return Player.StNormal;
        }

        if (AnyDashHeld()) {
            return HeldDashState;
        } else {
            return Player.StNormal;
        }
    }

    public static void HeldDashEnd(Entity e) {
        Player player = (e as Player)!;
        var refill = GetHeldRefillUsedByPlayer(player);
        if (refill is null)
            return;
        SetHeldRefillUsedByPlayer(player, null!);

        refill.TravelPercent = refill.Nodes.Length - 1;
        refill.Position = player.Position;
        refill.Add(new Coroutine(refill.RefillRoutine(player)));
    }

    public static IEnumerator HeldDashRoutine(Entity e) {
        Player player = (e as Player)!;
        Level level = (player.Scene as Level)!;
        
        while (true) {
            var refill = GetHeldRefillUsedByPlayer(player);
            if (refill is null)
                yield break;
            level.ParticlesFG.Emit(ZipMover.P_Sparks, 64, player.Position, new Vector2(4f), refill._lineColor);
            yield return null;
        }
    }

    public static HeldRefill? GetHeldRefillUsedByPlayer(Player player) {
        return DynamicData.For(player).Get<HeldRefill>("fh.heldRefill");
    }

    public static void SetHeldRefillUsedByPlayer(Player player, HeldRefill refill) {
        DynamicData.For(player).Set("fh.heldRefill", refill);
    }

    public static bool AnyDashHeld() {
        return Input.Dash.Check || Input.CrouchDash.Check;
    }

    public static bool AnyDashPressed() {
        return Input.Dash.Pressed || Input.CrouchDash.Pressed;
    }
    #endregion
}
