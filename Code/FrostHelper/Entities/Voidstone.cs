using FrostHelper.ModIntegration;

namespace FrostHelper;

[CustomEntity("FrostHelper/Voidstone")]
[Tracked]
public class Voidstone : Solid {
    public static ParticleType BoostParticle => _boostParticle ??= new ParticleType {
        Source = GFX.Game["particles/shard"],
        Color = FillColor,
        Color2 = FillColor * 0.7f,
        ColorMode = ParticleType.ColorModes.Fade,
        FadeMode = ParticleType.FadeModes.Late,
        RotationMode = ParticleType.RotationModes.Random,
        Size = 0.8f,
        SizeRange = 0.4f,
        SpeedMin = 20f,
        SpeedMax = 40f,
        SpeedMultiplier = 0.2f,
        LifeMin = 0.4f,
        LifeMax = 0.6f,
        DirectionRange = 6.28318548f
    };
    private static ParticleType? _boostParticle;

    private static bool _loadedHooks;

    public static void LoadHooksIfNeeded() {
        if (_loadedHooks)
            return;
        _loadedHooks = true;

        On.Celeste.Player.SuperWallJump += Player_SuperWallJump;
    }

    [OnUnload]
    public static void Unload() {
        if (!_loadedHooks)
            return;
        _loadedHooks = false;
        On.Celeste.Player.SuperWallJump -= Player_SuperWallJump;
    }



    // Check if the player is next to a voidstone when wallbouncing
    private static void Player_SuperWallJump(On.Celeste.Player.orig_SuperWallJump orig, Player self, int dir) {
        orig(self, dir);

        Voidstone stone;
        if ((stone = self.CollideFirst<Voidstone>(self.Position - Vector2.UnitX * dir * 5f)) != null) {
            stone.Used(self);
        }
    }

    public static Color FillColor = Calc.HexToColor("282a2e");

    public static void CreateTrail(Player player) {
        Vector2 scale = new Vector2(Math.Abs(player.Sprite.Scale.X) * (float) player.Facing, player.Sprite.Scale.Y);

        TrailManager.Add(player.Position - Vector2.UnitY, player.Get<PlayerSprite>(), player.Get<PlayerHair>(), scale, FillColor, player.Depth + 1, 1f);

    }

    public Player? PlayerThatWallbounced;

    public Voidstone(EntityData data, Vector2 offset) : base(data.Position + offset, data.Width, data.Height, true) {
        Add(new DashListener(OnDash));
        Add(new ClimbBlocker(false));
        Depth = Depths.Top;

        LoadHooksIfNeeded();
    }

    public void OnDash(Vector2 dir) {
        PlayerThatWallbounced = null;
    }

    public override void Update() {
        base.Update();
        if (PlayerThatWallbounced != null) {
            if (PlayerThatWallbounced.Speed.Length() > 300f) {
                if (Scene.OnInterval(0.1f))
                    CreateTrail(PlayerThatWallbounced);
                SceneAs<Level>().ParticlesBG.Emit(BoostParticle, PlayerThatWallbounced.Position);
            } else {
                PlayerThatWallbounced = null;
            }

        }
    }

    public void Used(Player player) {
        player.Speed *= 2f;
        PlayerThatWallbounced = player;
    }
}

[Tracked]
[CustomEntity("FrostHelper/VoidstoneRenderer")]
public class VoidstoneRenderer : Entity {
    public string Shader;

    public VoidstoneRenderer(EntityData data, Vector2 offset) : base(data.Position + offset) {
        Shader = data.Attr("shader", "");
    }

    public override void Render() {
        //return;


        Camera c = SceneAs<Level>().Camera;
        //Draw.Rect(Collider, Color.Black);
        VirtualRenderTarget tempA = GameplayBuffers.TempA;
        GameplayRenderer.End();
        Engine.Instance.GraphicsDevice.SetRenderTarget(tempA);

        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);

        Engine.Instance.GraphicsDevice.Clear(Color.Transparent);
        //Draw.Rect(0, 0, GameplayBuffers.Gameplay.Width, GameplayBuffers.Gameplay.Height, Color.DarkSlateGray);
        foreach (var item in Scene.Tracker.SafeGetEntities<Voidstone>()) {
            Draw.Rect(item.Position - c.Position, item.Width, item.Height, Color.Wheat);
        }
        GameplayRenderer.End();

        Effect eff = ShaderHelperIntegration.GetEffect(Shader);
        ShaderHelperIntegration.ApplyStandardParameters(eff, c);

        Engine.Instance.GraphicsDevice.SetRenderTarget(GameplayBuffers.Gameplay);
        int s = GameplayBuffers.Gameplay.Width / 320;
        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, eff, c.Matrix * s);

        foreach (var item in Scene.Tracker.SafeGetEntities<Voidstone>()) {
            Draw.SpriteBatch.Draw(tempA, (item.Position - c.Position) * s + c.Position, new Rectangle((int) (item.Position.X) * s, (int) (item.Position.Y) * s, (int) (item.Width) * s, (int) (item.Height) * s), Color.Black);
        }
        

        GameplayRenderer.End();
        GameplayRenderer.Begin();
        /*
        foreach (var item in Scene.Tracker.GetEntities<Voidstone>()) {
            float x = item.Position.X;
            float y = item.Position.Y;
            float w = item.Width - 1f;
            float h = item.Height;
            //top
            if (!item.CollideCheck<Voidstone>(item.Position + new Vector2(0, -2f)))
                Draw.Rect(x, y, w, 1f, Color.White);
            // bottom
            if (!item.CollideCheck<Voidstone>(item.Position + new Vector2(0, 2f)))
                Draw.Rect(x, y + h - 1f, w + 1f, 1f, Color.White);
            // left
            Draw.Rect(x, y, 1f, h, Color.White);
            // right
            Draw.Rect(x + w, y, 1f, h, Color.White);
        }*/
    }
}