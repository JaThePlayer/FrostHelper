namespace FrostHelper;

[CustomEntity("FrostHelper/CustomFireBarrier")]
public class CustomFireBarrier : Entity {
    public bool IsIce;
    public bool IgnoreCoreMode;

    public CustomFireBarrier(EntityData data, Vector2 offset) : base(data.Position + offset) {
        float width = data.Width;
        float height = data.Height;
        IsIce = data.Bool("isIce", false);
        Tag = Tags.TransitionUpdate;
        Collider = new Hitbox(width, height, 0f, 0f);
        IgnoreCoreMode = data.Bool("ignoreCoreMode", false);

        Add(new PlayerCollider(new Action<Player>(OnPlayer), null, null));

        if (!IgnoreCoreMode) {
            Add(new CoreModeListener(new Action<Session.CoreModes>(OnChangeMode)));
        }

        Lava = new LavaRect(width, height, IsIce ? 2 : 4);
        Add(Lava);
        Lava.SurfaceColor = ColorHelper.GetColor(data.Attr("surfaceColor"));
        Lava.EdgeColor = ColorHelper.GetColor(data.Attr("edgeColor"));
        Lava.CenterColor = ColorHelper.GetColor(data.Attr("centerColor"));
        Lava.SmallWaveAmplitude = 2f;
        Lava.BigWaveAmplitude = 1f;
        Lava.CurveAmplitude = 1f;
        if (IsIce) {
            Lava.UpdateMultiplier = 0f;
            Lava.Spikey = 3f;
            Lava.SmallWaveAmplitude = 1f;
        }

        lavaRect = new Rectangle((int) (data.Position + offset).X, (int) (data.Position + offset).Y, (int) width, (int) height);
        Depth = -8500;
        if (!IsIce && !data.Bool("silent", false)) {
            Add(idleSfx = new SoundSource());
            idleSfx.Position = new Vector2(Width, Height) / 2f;
        }

    }

    public void SetCollidable() {
        if (IgnoreCoreMode) {
            Collidable = true;
            return;
        }

        if (!IsIce) {
            Collidable = solid.Collidable = SceneAs<Level>().CoreMode == Session.CoreModes.Hot;
        } else {
            Collidable = solid.Collidable = SceneAs<Level>().CoreMode == Session.CoreModes.Cold;
        }
    }

    public override void Added(Scene scene) {
        base.Added(scene);
        scene.Add(solid = new Solid(Position + new Vector2(2f, 3f), Width - 4f, Height - 5f, false));
        SetCollidable();

        if (Collidable) {
            idleSfx?.Play("event:/env/local/09_core/lavagate_idle", null, 0f);
        }
    }

    private void OnChangeMode(Session.CoreModes mode) {
        SetCollidable();

        if (!Collidable) {
            Level level = SceneAs<Level>();
            Vector2 center = Center;
            int num = 0;
            while (num < Width) {
                int num2 = 0;
                while (num2 < Height) {
                    Vector2 vector = Position + new Vector2(num + 2, num2 + 2) + Calc.Random.Range(-Vector2.One * 2f, Vector2.One * 2f);
                    level.Particles.Emit(FireBarrier.P_Deactivate, vector, (vector - center).Angle());
                    num2 += 4;
                }
                num += 4;
            }
            idleSfx?.Stop(true);
        } else {
            idleSfx?.Play("event:/env/local/09_core/lavagate_idle", null, 0f);
        }
    }

    private void OnPlayer(Player player) {
        player.Die((player.Center - Center).SafeNormalize(), false, true);
    }

    private bool InView() {
        Camera camera = (Scene as Level)!.Camera;
        //Logger.Log(lavaRect.Location.ToString(), new Point((int)camera.Position.X, (int)camera.Position.Y).ToString());
        return lavaRect.Location.X + lavaRect.Width > camera.X - 16f && lavaRect.Location.Y + lavaRect.Height > camera.Y - 16f && lavaRect.Location.X < camera.X + 320f + 16f && lavaRect.Location.Y < camera.Y + 180f + 16f;//lavaRect.Contains(new Point((int)camera.Position.X, (int)camera.Position.Y));//
    }

    public override void Update() {
        Visible = Collidable && InView();
        if ((Scene as Level)!.Transitioning) {
            idleSfx?.UpdateSfxPosition();
        } else {
            if (Visible)
                base.Update();
        }
    }

    public override void Render() {
        base.Render();
    }

    private Rectangle lavaRect;
    public LavaRect Lava;

    private Solid solid;

    private SoundSource idleSfx;
}
