using System.Runtime.CompilerServices;

namespace FrostHelper;

[CustomEntity("FrostHelper/CustomFireBarrier")]
public class CustomFireBarrier : Entity {
    public bool IsIce;
    public bool IgnoreCoreMode;
    
    private readonly bool CanBeCollidable;

    internal readonly CustomLavaRect Lava;
    
    public CustomFireBarrier(EntityData data, Vector2 offset) : base(data.Position + offset) {
        float width = data.Width;
        float height = data.Height;
        IsIce = data.Bool("isIce", false);
        CanBeCollidable = data.Bool("canCollide", true);
        
        Tag = Tags.TransitionUpdate;
        IgnoreCoreMode = data.Bool("ignoreCoreMode", false);

        if (CanBeCollidable) {
            Collider = new Hitbox(width, height);
            Add(new PlayerCollider(OnPlayer));
        }

        if (!IgnoreCoreMode) {
            Add(new CoreModeListener(OnChangeMode));
        }

        var lava = Lava = new CustomLavaRect(width, height, IsIce ? 2 : 4, data.Float("bubbleAmountMultiplier", 1f));
        Add(lava);
        lava.SurfaceColor = ColorHelper.GetColor(data.Attr("surfaceColor"));
        lava.EdgeColor = ColorHelper.GetColor(data.Attr("edgeColor"));
        lava.CenterColor = ColorHelper.GetColor(data.Attr("centerColor"));
        lava.SmallWaveAmplitude = data.Float("smallWaveAmplitude", 2f);
        lava.BigWaveAmplitude = data.Float("bigWaveAmplitude", 1f);
        lava.CurveAmplitude = data.Float("curveAmplitude", 1f);
        lava.OnlyMode = data.Enum("surfaces", CustomLavaRect.OnlyModes.All);
        if (IsIce) {
            lava.UpdateMultiplier = 0f;
            lava.Spikey = 3f;
            lava.SmallWaveAmplitude /= 2f;
        }

        lavaRect = new Rectangle((int) (data.Position + offset).X, (int) (data.Position + offset).Y, (int) width, (int) height);
        Depth = -8500;

        var silentFlag = data.Attr("silentFlag");
        if (!string.IsNullOrWhiteSpace(silentFlag)) {
            idleSfx = new SoundSource();
            Add(new FlagListener(silentFlag, onSilentFlagChange, true, true));
        } else if (!IsIce && !data.Bool("silent", false)) {
            SetupIdleSfx();
        }
    }

    private void onSilentFlagChange(Session session, string? flag, bool value) {
        // we need to delay adding/removing the sfx, because this func will be called in flagListener.EntityAwake, and the game will crash if something is added at that time :/
        Scene.OnEndOfFrame += () => {
            if (value) {
                SetupIdleSfx();
            } else {
                Remove(idleSfx);
            }
        };
    }

    private void SetupIdleSfx() {
        Add(idleSfx ??= new SoundSource());
        idleSfx.Position = new Vector2(Width, Height) / 2f;
        if (idleSfx.EventName is { })
            idleSfx.Play(idleSfx.EventName);
    }

    public void SetCollidable() {
        if (IgnoreCoreMode) {
            Collidable = true;
            return;
        }

        if (!IsIce)
            Collidable = SceneAs<Level>().CoreMode == Session.CoreModes.Hot;
        else
            Collidable = SceneAs<Level>().CoreMode == Session.CoreModes.Cold;
        
        if (solid is { })
            solid.Collidable = Collidable;
    }

    public override void Added(Scene scene) {
        base.Added(scene);
        if (CanBeCollidable)
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool InView() {
        return CameraCullHelper.IsRectangleVisible(lavaRect, lenience: 16f);
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

    private Rectangle lavaRect;

    private Solid? solid;

    private SoundSource idleSfx;
}
