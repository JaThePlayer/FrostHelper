namespace FrostHelper;

[CustomEntity("FrostHelper/HeldRefill")]
public class HeldRefill : Entity {
    public Vector2[] Nodes;
    public float SpeedMult;

    public VertexLight Light;
    public BloomPoint Bloom;
    public Sprite Sprite;
    public Sprite Flash;
    public SineWave SpriteSineWave;

    public float TravelPercent;
    public Vector2 LastTravelDelta;

    private readonly Color LineColor;

    public HeldRefill(EntityData data, Vector2 offset) : base(data.Position + offset) {
        var directory = data.Attr("directory", "objects/refill").TrimEnd('/');
        LineColor = data.GetColor("lineColor", "ffff00");
        
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


        SpeedMult = Math.Max(data.Float("speed", 6f), 0);

        Add(Sprite = new Sprite(GFX.Game, $"{directory}/idle"));
        Sprite.AddLoop("idle", "", 0.1f);
        Sprite.Play("idle", false, false);
        Sprite.CenterOrigin();

        Add(Flash = new Sprite(GFX.Game, $"{directory}/flash"));
        Flash.Add("flash", "", 0.05f);
        Flash.OnFinish = _ => Flash.Visible = false;
        Flash.CenterOrigin();

        Add(Bloom = new BloomPoint(0.8f, 16f));
        Add(Light = new VertexLight(Color.White, 1f, 16, 48));
        Add(new MirrorReflection());
        Add(new CustomBloom(() => RenderPath(true)));

        Add(SpriteSineWave = new SineWave(0.6f, 0f));
        SpriteSineWave.Randomize();
        UpdateY();

        Depth = -100;

        Collider = new Hitbox(24f, 24, -12f, -12f);
        Add(new PlayerCollider(OnPlayer, null, null));
    }

    public override void Update() {
        base.Update();
        if (Scene.OnInterval(0.1f)) {
            (Scene as Level)!.ParticlesFG.Emit(p_glow, 1, Position, Vector2.One * 5f);
        }
        UpdateY();
        Light.Alpha = Calc.Approach(Light.Alpha, Sprite.Visible ? 1f : 0f, 4f * Engine.DeltaTime);
        Bloom.Alpha = Light.Alpha * 0.8f;
        if (Scene.OnInterval(2f) && Sprite.Visible) {
            Flash.Play("flash", true, false);
            Flash.Visible = true;
        }
    }

    public override void Render() {
        if (Sprite.Visible) {
            Sprite.DrawOutline(1);
        }

        RenderPath(false);

        base.Render();
    }

    private void RenderPath(bool bloom) {
        int startIndex = PercentageToIndex(TravelPercent) - 1;
        // Draw the path
        for (int i = startIndex; i < Nodes.Length - 1; i++) {
            if (i == startIndex && TravelPercent > 0f) {
                float percent = TravelPercent - (float) Math.Floor(TravelPercent);
                float angle = Calc.Angle(Nodes[i + 1], Nodes[i]);
                float fullLength = Vector2.Distance(Nodes[i + 1], Nodes[i]);

                Draw.LineAngle(CenterLinePos(Nodes[i + 1]), angle, (float) Math.Floor(fullLength * (1f - percent)), LineColor);
            } else {
                Draw.Line(CenterLinePos(Nodes[i]), CenterLinePos(Nodes[i + 1]), bloom ? Color.White * 0.3f : LineColor, bloom ? 3 : 1);
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
        level.ParticlesFG.Emit(p_shatter, 5, Position, Vector2.One * 4f, angle - 1.57079637f);
        level.ParticlesFG.Emit(p_shatter, 5, Position, Vector2.One * 4f, angle + 1.57079637f);
        SlashFx.Burst(Position, angle);

        RemoveSelf();
        yield break;
    }
    public void OnPlayer(Player player) {
        // TODO
        if (AnyDashPressed() && player.Holding is null) {
            Collidable = false;
            SetHeldRefillUsedByPlayer(player, this);
            player.StateMachine.State = HeldDashState;
            player.RefillDash();
            //Add(new Coroutine(RefillRoutine(player)));
        }
    }

    public void UpdateY() {
        Flash.Y = Sprite.Y = Bloom.Y = SpriteSineWave.Value * 2f;
    }

    public int PercentageToIndex(float percent) {
        return ((int) Math.Floor(percent))/* % (Nodes.Length - 1)*/ + 1;
    }


    ParticleType p_glow = Refill.P_Glow; // TODO
    ParticleType p_shatter = Refill.P_Shatter; // TODO

    #region State
    public static int HeldDashState = int.MaxValue;

    public static void HeldDashBegin(Entity e) {
        Player player = (e as Player)!;
        var refill = GetHeldRefillUsedByPlayer(player);
        if (refill is null)
            return;
        
        player.Position = refill.Position;
        player.Speed = Vector2.Zero;
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

        // new:
        player.Speed = Calc.AngleToVector(Calc.Angle(start, end), Vector2.Distance(start, end) * travelPercentDelta / Engine.DeltaTime).Floor();

        if (refill.TravelPercent > refill.Nodes.Length - 1) {
            refill.Visible = false;
            return Player.StNormal;
        }

        // new if
        if (refill.PercentageToIndex(refill.TravelPercent) != index) {
            index = refill.PercentageToIndex(refill.TravelPercent);
            start = refill.Nodes[index - 1];
            end = refill.Nodes[index];

            //player.Speed = ((start +Calc.AngleToVector(Calc.Angle(start, end), Vector2.Distance(start, end) * (refill.TravelPercent - (int)Math.Floor(refill.TravelPercent)))).Floor() - player.Position) * (1f / Engine.DeltaTime);
            player.Position = start;
            player.Speed = Calc.AngleToVector(Calc.Angle(start, end), Vector2.Distance(start, end) * travelPercentDelta / Engine.DeltaTime).Floor();
        }

        refill.Position = player.Position + player.Speed * Engine.DeltaTime;

        if (player.OnGround() && player.CanUnDuck) {

            if (Input.Jump.Pressed && player.jumpGraceTimer > 0f) {
                //player.Invoke("SuperJump");
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
        //player.Speed = refill.LastTravelDelta * (1f / Engine.DeltaTime);
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
            level.ParticlesFG.Emit(ZipMover.P_Sparks, 64, player.Position, new Vector2(4f), refill.LineColor);
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
