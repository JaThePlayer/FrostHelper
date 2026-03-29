using Celeste.Mod.Helpers;
using FrostHelper.Helpers;

namespace FrostHelper.Entities;

[CustomEntity("FrostHelper/SantaRefill")]
internal sealed class SantaRefill : Entity {
    private readonly bool _oneUse;
    private readonly Image _outline;
    private readonly Sprite _sprite;
    private readonly Sprite _flash;
    private readonly Wiggler _wiggler;
    private readonly BloomPoint _bloom;
    private readonly VertexLight _light;
    private readonly SineWave _sine;
    private readonly float _respawnTime;
    
    private Level _level;
    private float _respawnTimer;

    public SantaRefill(EntityData data, Vector2 offset) : base(data.Position + offset) {
        _oneUse = data.Bool("oneUse");
        _respawnTime = data.Float("respawnTime", 2.5f);
        Collider = data.Collider("hitbox") ?? new Hitbox(16f, 16f, -8f, -8f);
        
        Add(new PlayerCollider(OnPlayer, null, null));
        
        string dir = data.Attr("directory", "objects/FrostHelper/santaRefill");
        if (!dir.EndsWith('/'))
            dir += '/';
        Add(_outline = new Image(GFX.Game[dir + "outline"]));
        _outline.CenterOrigin();
        _outline.Visible = false;
        
        Add(_sprite = new Sprite(GFX.Game, dir + "idle"));
        _sprite.AddLoop("idle", "", 0.1f);
        _sprite.Play("idle");
        _sprite.CenterOrigin();
        
        Add(_flash = new Sprite(GFX.Game, dir + "flash"));
        _flash.Add("flash", "", 0.05f);
        _flash.OnFinish = _ => {
            _flash.Visible = false;
        };
        _flash.CenterOrigin();
        
        Add(_wiggler = Wiggler.Create(1f, 4f, t =>
        {
            _sprite.Scale = _flash.Scale = Vector2.One * (1f + t * 0.2f);
        }));
        Add(new MirrorReflection());
        Add(_bloom = data.GetBloomPoint("bloom", 0.8f, 16f));
        Add(_light = data.GetVertexLight("light", Color.White, 1f, 16, 40));
        Add(_sine = new SineWave(0.6f));
        _sine.Randomize();
        UpdateY();

        Depth = Depths.DreamBlocks - 1;
    }

    public override void Added(Scene scene) {
        base.Added(scene);
        _level = SceneAs<Level>();
    }

    public override void Update() {
        base.Update();
        
        if (_respawnTimer > 0f)
        {
            _respawnTimer -= Engine.DeltaTime;
            if (_respawnTimer <= 0f)
            {
                Respawn();
            }
        }
        else if (Scene.OnInterval(0.1f))
        {
            _level.ParticlesFG.Emit(Refill.P_Glow, 1, Position, Vector2.One * 5f);
        }

        UpdateY();
        _light.Alpha = Calc.Approach(_light.Alpha, _sprite.Visible ? 1f : 0f, 4f * Engine.DeltaTime);
        _bloom.Alpha = _light.Alpha * 0.8f;
        if (Scene.OnInterval(2f) && _sprite.Visible)
        {
            _flash.Play("flash", true, false);
            _flash.Visible = true;
        }
    }

    private void Respawn() {
        if (Collidable)
            return;

        Collidable = true;
        _sprite.Visible = true;
        _outline.Visible = false;
        Depth = Depths.DreamBlocks - 1;
        _wiggler.Start();
        Audio.Play("event:/game/general/diamond_return", Position);
        _level.ParticlesFG.Emit(Refill.P_Regen, 16, Position, Vector2.One * 2f);
    }

    private void UpdateY()
    {
        _flash.Y = _sprite.Y = _bloom.Y = _sine.Value * 2f;
    }

    public override void Render()
    {
        if (_sprite.Visible)
            _sprite.DrawOutline(1);
        
        base.Render();
    }

    private void OnPlayer(Player player) {
        var santaBoostHandler = SantaBoostHandler.GetOrCreate(player);
        
        if (!santaBoostHandler.HasBoost)
        {
            Audio.Play("event:/game/general/diamond_touch", Position);
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
            Collidable = false;
            santaBoostHandler.HasBoost = true;
            Add(new Coroutine(RefillRoutine(player)));
            _respawnTimer = _respawnTime;
        }
    }

    private IEnumerator RefillRoutine(Player player) {
        Celeste.Celeste.Freeze(0.05f);
        yield return null;
        _level.Shake(0.3f);
        _sprite.Visible = _flash.Visible = false;
        if (!_oneUse)
            _outline.Visible = true;
        Depth = 8999;
        yield return 0.05f;
        float num = player.Speed.Angle();
        _level.ParticlesFG.Emit(Refill.P_Shatter, 5, Position, Vector2.One * 4f, Color.White);
        _level.ParticlesFG.Emit(Refill.P_Shatter, 5, Position, Vector2.One * 4f, Color.Red);
        SlashFx.Burst(Position, num);
        if (_oneUse)
            RemoveSelf();
    }
}

internal sealed class SantaBoostHandler : Component {
    #region Hooks

    private static bool _hooksLoaded;
    
    [HookPreload]
    internal static void LoadHooksIfNeeded() {
        if (_hooksLoaded)
            return;
        _hooksLoaded = true;
        
        On.Celeste.PlayerHair.GetHairColor += PlayerHairOnGetHairColor;
        FrostModule.RegisterILHook(EasierILHook.Hook<Player>(nameof(Player.orig_Update), PlayerOrigUpdate));
    }

    /// <summary>
    /// Patch all instances of `player.Dashes > 1` to `player.Dashes > (hasSantaBoost ? -1 : 1)`.
    /// This way, santa boosts make the player hair more visible.
    /// </summary>
    private static void PlayerOrigUpdate(ILContext il) {
        var cursor = new ILCursor(il);

        while (cursor.TryGotoNextBestFit(MoveType.After, 
                   i => i.MatchLdarg(0),
                   i => i.MatchLdfld<Player>(nameof(Player.Dashes)),
                   i => i.MatchLdcI4(1))) {
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate(ReplaceTargetDashCount);
        }

        return;

        static int ReplaceTargetDashCount(int one, Player player) {
            if (GetOrNull(player) is not { } handler) {
                return one;
            }

            return handler.HasBoost ? -1 : one;
        }
    }


    [OnUnload]
    internal static void Unload() {
        if (!_hooksLoaded)
            return;
        _hooksLoaded = false;
        
        On.Celeste.PlayerHair.GetHairColor -= PlayerHairOnGetHairColor;
    }
    
    private static Color PlayerHairOnGetHairColor(On.Celeste.PlayerHair.orig_GetHairColor orig, PlayerHair self, int index) {
        if (self.EntityAs<Player>() is not { } player || GetOrNull(player) is not { HasBoost: true }) {
            return orig(self, index);
        }
        
        return index % 2 == 0 ? orig(self, index) : Color.White;
    }
    #endregion
    
    private readonly Player _player;

    public bool HasBoost { get; set; }
    
    public float BoostParticleTimer { get; set; }

    public SantaBoostHandler(Player player) : base(true, false) {
        LoadHooksIfNeeded();
        _player = player;
    }

    public override void Update() {
        base.Update();
        
        if (_player.Scene.MaybeLevel() is not { } level) {
            return;
        }
        
        if (HasBoost && Engine.FreezeTimer <= 0)
        {
            if (FrostModule.Settings.SantaBoostKey.Pressed)
            {
                FrostModule.Settings.SantaBoostKey.ConsumeBuffer();
                FrostModule.Settings.SantaBoostKey.ConsumePress();
                
                HasBoost = false;
                
                // We don't want to recover the dash, so let's set the inventory to no refills.
                bool prevNoRefills = level.Session.Inventory.NoRefills;
                level.Session.Inventory.NoRefills = true;
                _player.SuperBounce(_player.Y);
                level.Session.Inventory.NoRefills = prevNoRefills;
                
                BoostParticleTimer = 0.25f;
            }
        }
            
        if (BoostParticleTimer > 0f && _player.Scene.OnInterval(0.03f))
        {
            BoostParticleTimer -= Engine.DeltaTime;
            level.ParticlesFG.Emit(BadelineBoost.P_Move, 1, _player.Center, Vector2.One * 4f, Color.Red);
            level.ParticlesFG.Emit(BadelineBoost.P_Move, 1, _player.Center, Vector2.One * 4f, Color.White);
        }
    }

    public static SantaBoostHandler? GetOrNull(Player player) {
        if (player.Get<SantaBoostHandler>() is { } handler) {
            return handler;
        }

        return null;
    }
    
    public static SantaBoostHandler GetOrCreate(Player player) {
        if (GetOrNull(player) is { } handler) {
            return handler;
        }

        handler = new SantaBoostHandler(player);
        player.Add(handler);

        return handler;
    }
}
