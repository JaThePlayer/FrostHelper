using FrostHelper.Helpers;
using NLua;

namespace FrostHelper.Entities;

[CustomEntity("FrostHelper/LuaBoss")]
internal sealed class LuaBadelineBoss : FinalBoss {
    #region Hooks
    private static bool _HooksLoaded;

    internal static void LoadHooksIfNeeded() {
        if (_HooksLoaded)
            return;

        _HooksLoaded = true;

        On.Celeste.FinalBoss.StartAttacking += FinalBoss_StartAttacking;
        On.Celeste.FinalBoss.OnPlayer += FinalBoss_OnPlayer;
        On.Celeste.FinalBoss.CreateBossSprite += FinalBoss_CreateBossSprite;
    }

    private static void FinalBoss_CreateBossSprite(On.Celeste.FinalBoss.orig_CreateBossSprite orig, FinalBoss self) {
        if (self is not LuaBadelineBoss luaBoss) {
            orig(self);
            return;
        }

        luaBoss.CreateCustomBossSprite();
    }

    private static void FinalBoss_OnPlayer(On.Celeste.FinalBoss.orig_OnPlayer orig, FinalBoss self, Player player) {
        if (self is not LuaBadelineBoss luaBoss) {
            orig(self, player);
            return;
        }

        luaBoss.BeforeOnPlayer();
        orig(self, player);
        luaBoss.AfterOnPlayer();
    }

    private static void FinalBoss_StartAttacking(On.Celeste.FinalBoss.orig_StartAttacking orig, FinalBoss self) {
        if (self is not LuaBadelineBoss luaBoss) {
            orig(self);
            return;
        }

        self.attackCoroutine.Replace(luaBoss.LuaFuncToIEnumerator(luaBoss.Func));
    }

    [OnUnload]
    internal static void Unload() {
        if (!_HooksLoaded)
            return;

        _HooksLoaded = false;

        On.Celeste.FinalBoss.StartAttacking -= FinalBoss_StartAttacking;
        On.Celeste.FinalBoss.OnPlayer -= FinalBoss_OnPlayer;
        On.Celeste.FinalBoss.CreateBossSprite -= FinalBoss_CreateBossSprite;
    }
    #endregion

    public string Filename;
    private LuaFunction? Func;
    private LuaFunction? OnEndFunc;
    private LuaFunction? OnHitFunc;
    private EntityData EntityData;
    private LuaTable LuaCtx;

    private List<Entity> CreatedShots { get; set; } = new();

    private List<Coroutine> CreatedCoroutines { get; set; } = new();

    public LuaBadelineBoss(EntityData e, Vector2 offset) : base(e, offset) {
        LoadHooksIfNeeded();

        EntityData = e;
        patternIndex = -1;
        canChangeMusic = false;
        dialog = false;

        Filename = e.Attr("filename");

        LuaCtx = LuaHelper.DictionaryToLuaTable(new() {
            ["self"] = this
        });

        UpdateLuaCtx();

        var env = LuaHelper.RunLua(@"Assets/FrostHelper/LuaBoss/env", env: null, LuaCtx)[0] as LuaTable;//Env.Value;
        var returned = LuaHelper.RunLua(Filename, env, LuaCtx, "getBossData");

        if (returned is [LuaFunction luaFunc, ..]) {
            Func = luaFunc;
        } else {
            NotificationHelper.Notify("Failed to load lua boss! Check log.txt.");
            Logger.Log(LogLevel.Error, "FrostHelper.LuaBoss", $"Failed to load lua boss from {Filename}");
        }

        if (returned is [_, LuaFunction onEndFunc, ..]) {
            OnEndFunc = onEndFunc;
        }
        if (returned is [_, _, LuaFunction onHitFunc, ..]) {
            OnHitFunc = onHitFunc;
        }

        if (!e.Bool("lockCamera", true)) {
            Remove(Get<CameraLocker>());
        }
    }

    internal void CreateCustomBossSprite() {
        Add(Sprite = GFX.SpriteBank.Create(EntityData.Attr("sprite", "badeline_boss")));
        Sprite.Color = EntityData.GetColor("color", "ffffff");

        Sprite.OnFrameChange = (string anim) => {
            if (anim == "idle" && Sprite.CurrentAnimationFrame == 18) {
                Audio.Play("event:/char/badeline/boss_idle_air", Position);
            }
        };
        facing = -1;
        if (NormalSprite != null) {
            Sprite.Position = NormalSprite.Position;
            Remove(NormalSprite);
        }
        if (normalHair != null) {
            Remove(normalHair);
        }
        NormalSprite = null;
        normalHair = null;
    }

    public override void Update() {
        base.Update();

        foreach (var cor in CreatedCoroutines) {
            cor.Update();
        }

        if (OnEndFunc is { } onEndFunc && !level.IsInBounds(Position, 24f)) {
            var helperEntity = new Entity() {
                Visible = false,
                Collidable = false,
                Active = true,
            };
            helperEntity.Add(new Coroutine(LuaFuncToIEnumerator(OnEndFunc), false));
            Scene.Add(helperEntity);
            return;
        }
    }

    private void UpdateLuaCtx() {
        LuaCtx["nodeIndex"] = nodeIndex;
        LuaCtx["isFinalNode"] = nodeIndex == nodes.Length - 1;
        LuaCtx["player"] = level?.Tracker.GetEntity<Player>();
    }

    private IEnumerator LuaFuncToIEnumerator(LuaFunction? func) {
        if (func is { }) {
            UpdateLuaCtx();

            return LuaHelper.LuaFuncToIEnumerator(func);
        }

        return new object[0].GetEnumerator();
    }

    internal void BeforeOnPlayer() {
    }

    internal void AfterOnPlayer() {
        CleanupShotsAndCoroutines();

        if (OnHitFunc is { } onHit) {
            StartCoroutine(onHit);
        }
    }

    internal void CleanupShotsAndCoroutines() {
        foreach (var shot in CreatedShots) {
            shot.RemoveSelf();
        }

        CreatedShots.Clear();
        CreatedCoroutines.Clear();
    }

    private void ShootImpl(Vector2? location, LuaTable? args) {
        if (!chargeSfx.Playing) {
            chargeSfx.Play("event:/char/badeline/boss_bullet", "end", 1f);
        } else {
            chargeSfx.Param("end", 1f);
        }
        Sprite.Play("attack1Recoil", true, false);

        if (level.Tracker.GetEntity<Player>() is { } player) {
            var shot = Engine.Pooler.Create<CustomBossShot>().Init(this, player, args, location);
            CreatedShots.Add(shot);
            level.Add(shot);
        }
    }

    #region Lua API
    public new void StartShootCharge() {
        Sprite.Play("attack1Begin", false, false);
        chargeSfx.Play("event:/char/badeline/boss_bullet", null, 0f);
    }

    public void Shoot(LuaTable? args = null) {
        ShootImpl(null, args);
    }

    public void ShootAt(Vector2 location, LuaTable? args = null) {
        ShootImpl(location, args);
    }

    public IEnumerator Beam(LuaTable? args = null) {
        laserSfx.Play("event:/char/badeline/boss_laser_charge", null, 0f);
        Sprite.Play("attack2Begin", true, false);
        yield return 0.1f;

        var followTime = args.GetOrDefault("followTime", 0.9f);
        var lockTime = args.GetOrDefault("lockTime", 0.5f);

        if (level.Tracker.GetEntity<Player>() is { } player) {
            var beam = Engine.Pooler.Create<CustomBossBeam>().Init(this, player, args);

            CreatedShots.Add(beam);
            level.Add(beam);
        }
        yield return followTime;

        Sprite.Play("attack2Lock", true, false);
        yield return lockTime;

        laserSfx.Stop(true);
        Audio.Play("event:/char/badeline/boss_laser_fire", Position);
        Sprite.Play("attack2Recoil", false, false);
    }

    public void StartCoroutine(LuaFunction func) {
        var c = new Coroutine(LuaFuncToIEnumerator(func));
        CreatedCoroutines.Add(c);
    }

    public void ShatterSpinners(LuaTable? args = null) {
        bool anySpinnersBroken = false;

        Type?[] types;
        if (args.GetOrDefault<object?>("types", null) is LuaTable typesStringList) {
            types = new Type?[typesStringList.Values.Count];
            var i = 0;
            foreach (var typeName in typesStringList.Values.OfType<string>()) {
                if (API.API.EntityNameToTypeOrNull(typeName) is { } t) {
                    types[i++] = t;
                } else {
                    if (!args.GetOrDefault("silenceTypeNotFoundNotifications", false))
                        NotificationHelper.Notify($"Failed to find type {typeName}");
                }
            }
        } else {
            NotificationHelper.Notify("No Types???");
            types = new Type?[0];
        }

        LuaFunction? filterFunc = args.GetOrDefault<LuaFunction?>("filter", null);

        foreach (var t in types) {
            if (t is null)
                continue;

            #region Fast Paths
            if (t == typeof(CrystalStaticSpinner)) {
                foreach (CrystalStaticSpinner spinner in Scene.Tracker.GetEntities<CrystalStaticSpinner>()) {
                    if (!ShouldSkip(spinner)) {
                        spinner.Destroy(true);
                        anySpinnersBroken = true;
                    }
                }
                continue;
            }

            if (t == typeof(CustomSpinner)) {
                foreach (CustomSpinner spinner in Scene.Tracker.GetEntities<CustomSpinner>()) {
                    if (!ShouldSkip(spinner)) {
                        spinner.Destroy(true);
                        anySpinnersBroken = true;
                    }
                }
                continue;
            }
            #endregion

            var allEntities = Scene.Tracker.GetEntitiesOrNull(t) ?? Scene.Entities.Where(e => e.GetType() == t);
            MethodInfo? destroyMethod = null;

            foreach (var entity in allEntities) {
                if (ShouldSkip(entity)) {
                    continue;
                }

                anySpinnersBroken = true;
                destroyMethod ??= t.GetMethod("Destroy", new Type[] { typeof(bool) }) ?? t.GetMethod("Destroy", new Type[] { });

                switch (destroyMethod?.GetParameters()) {
                    case [var oneArg] when oneArg.ParameterType == typeof(bool):
                        destroyMethod.Invoke(entity, new object[] { true });
                        break;
                    case []:
                        destroyMethod.Invoke(entity, null);
                        break;
                    default:
                        entity.RemoveSelf();
                        break;
                }
            }
        }

        if (anySpinnersBroken) {
            Audio.Play(args.GetOrDefault("sfx", "event:/game/06_reflection/boss_spikes_burst"));
        }

        bool ShouldSkip(Entity entity) => filterFunc?.Call(entity) is [not true, ..];
    }
    #endregion
}

[Pooled]
[Tracked(false)]
public sealed class CustomBossShot : Entity {

    public float MoveSpeed;

    private const float CantKillTime = 0.15f;

    private const float AppearTime = 0.1f;

    public float WaveStrength = 3f;

    public CustomBossShot() : base(Vector2.Zero) {
        Add(sprite = GFX.SpriteBank.Create("badeline_projectile"));
        Collider = new Hitbox(4f, 4f, -2f, -2f);
        Add(new PlayerCollider(OnPlayer, null, null));
        Depth = -1000000;
        Add(sine = new SineWave(1.4f, 0f));
    }

    internal CustomBossShot Init(LuaBadelineBoss boss, Player target, LuaTable? args, Vector2? targetLoc) {
        if (targetLoc is { }) {
            return Init(boss, targetLoc.Value, args);
        }

        if (args.GetOrDefault<Vector2?>("target", null) is { } v) {
            return Init(boss, v, args);
        }

        if (args.GetFloatOrNull("angle") is { } angle) {
            return Init(boss, boss.Center + Calc.AngleToVector(angle.ToRad(), 100f), args);
        }

        this.target = target;
        SharedInit(boss, args);
        return this;
    }

    internal CustomBossShot Init(LuaBadelineBoss boss, Vector2 target, LuaTable? args) {
        this.target = null;
        targetPt = target;
        SharedInit(boss, args);
        return this;
    }

    private void SharedInit(LuaBadelineBoss boss, LuaTable? args) {
        this.boss = boss;
        anchor = Position = boss.Center;

        dead = hasBeenInCamera = false;
        cantKillTimer = CantKillTime;
        appearTimer = AppearTime;
        sine.Reset();
        sineMult = 0f;
        sprite.Play("charge", true, false);

        angleOffset = args.GetOrDefault("angleOffset", 0f).ToRad();
        MoveSpeed = args.GetOrDefault("speed", 100f);
        WaveStrength = args.GetOrDefault("waveStrength", 3f);


        InitSpeed();
    }

    private void InitSpeed() {
        if (target != null) {
            speed = (target.Center - Center).SafeNormalize(MoveSpeed);
        } else {
            speed = (targetPt - Center).SafeNormalize(MoveSpeed);
        }
        if (angleOffset != 0f) {
            speed = speed.Rotate(angleOffset);
        }
        perp = speed.Perpendicular().SafeNormalize();
        particleDir = (-speed).Angle();
    }

    public override void Added(Scene scene) {
        base.Added(scene);
        level = SceneAs<Level>();
        if (boss.Moving) {
            RemoveSelf();
        }
    }

    public override void Removed(Scene scene) {
        base.Removed(scene);
        level = null!;

        dead = true;
    }

    public override void Update() {
        base.Update();
        if (appearTimer > 0f) {
            Position = (anchor = boss.ShotOrigin);
            appearTimer -= Engine.DeltaTime;
            return;
        }
        if (cantKillTimer > 0f) {
            cantKillTimer -= Engine.DeltaTime;
        }
        anchor += speed * Engine.DeltaTime;
        Position = anchor + perp * sineMult * sine.Value * WaveStrength;
        sineMult = Calc.Approach(sineMult, 1f, 2f * Engine.DeltaTime);
        if (!dead) {
            bool visible = level.IsInCamera(Position, 8f);
            if (visible && !hasBeenInCamera) {
                hasBeenInCamera = true;
            } else if (!visible && hasBeenInCamera) {
                Destroy();
            }
            if (Scene.OnInterval(0.04f)) {
                level.ParticlesFG.Emit(FinalBossShot.P_Trail, 1, Center, Vector2.One * 2f, particleDir);
            }
        }
    }

    public override void Render() {
        sprite.DrawOutline(Color.Black);

        base.Render();
    }

    public void Destroy() {
        dead = true;
        RemoveSelf();
    }

    private void OnPlayer(Player player) {
        if (!dead) {
            if (cantKillTimer > 0f) {
                Destroy();
                return;
            }
            player.Die((player.Center - Position).SafeNormalize(), false, true);
        }
    }

    public static ParticleType P_Trail;

    private FinalBoss boss;

    private Level level;

    private Vector2 speed;

    private float particleDir;

    private Vector2 anchor;

    private Vector2 perp;

    private Player? target;

    private Vector2 targetPt;

    private float angleOffset;

    private bool dead;

    private float cantKillTimer;

    private float appearTimer;

    private bool hasBeenInCamera;

    private SineWave sine;

    private float sineMult;

    private Sprite sprite;

    public enum ShotPatterns {
        Single,
        Double,
        Triple
    }
}

[Pooled]
[Tracked(false)]
public class CustomBossBeam : Entity {
    public CustomBossBeam() {
        fade = new VertexPositionColor[24];
        Add(beamSprite = GFX.SpriteBank.Create("badeline_beam"));
        beamSprite.OnLastFrame = (string anim) => {
            if (anim == "shoot") {
                Destroy();
            }
        };
        Add(beamStartSprite = GFX.SpriteBank.Create("badeline_beam_start"));
        beamSprite.Visible = false;
        Depth = -1000000;
    }

    internal CustomBossBeam Init(LuaBadelineBoss boss, Player target, LuaTable? args) {
        this.boss = boss;

        RotationSpeed = args.GetOrDefault("rotationSpeed", 200f);
        followTimer = args.GetOrDefault("followTime", 0.9f);
        chargeTimer = followTimer + args.GetOrDefault("lockTime", 0.5f);
        activeTimer = args.GetOrDefault("activeTime", 0.12f); // todo: fix

        beamSprite.Play("charge", false, false);
        sideFadeAlpha = 0f;
        beamAlpha = 0f;
        int num;
        if (target.Y <= boss.Y + 16f) {
            num = 1;
        } else {
            num = -1;
        }
        if (target.X >= boss.X) {
            num *= -1;
        }
        angle = Calc.Angle(boss.BeamOrigin, target.Center);
        Vector2 vector = Calc.ClosestPointOnLine(boss.BeamOrigin, boss.BeamOrigin + Calc.AngleToVector(angle, BeamLength), target.Center);
        vector += (target.Center - boss.BeamOrigin).Perpendicular().SafeNormalize(AngleStartOffset) * num;
        angle = Calc.Angle(boss.BeamOrigin, vector);
        return this;
    }

    public override void Added(Scene scene) {
        base.Added(scene);
        if (boss.Moving) {
            RemoveSelf();
        }
    }

    public override void Update() {
        base.Update();
        player = Scene.Tracker.GetEntity<Player>();
        beamAlpha = Calc.Approach(beamAlpha, 1f, 2f * Engine.DeltaTime);
        if (chargeTimer > 0f) {
            sideFadeAlpha = Calc.Approach(sideFadeAlpha, 1f, Engine.DeltaTime);
            if (player != null && !player.Dead) {
                followTimer -= Engine.DeltaTime;
                chargeTimer -= Engine.DeltaTime;
                if (followTimer > 0f && player.Center != boss.BeamOrigin) {
                    Vector2 vector = Calc.ClosestPointOnLine(boss.BeamOrigin, boss.BeamOrigin + Calc.AngleToVector(angle, BeamLength), player.Center);
                    Vector2 center = player.Center;
                    vector = Calc.Approach(vector, center, RotationSpeed * Engine.DeltaTime);
                    angle = Calc.Angle(boss.BeamOrigin, vector);
                } else if (beamSprite.CurrentAnimationID == "charge") {
                    beamSprite.Play("lock", false, false);
                }
                if (chargeTimer <= 0f) {
                    SceneAs<Level>().DirectionalShake(Calc.AngleToVector(angle, 1f), 0.15f);
                    Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
                    DissipateParticles();
                    return;
                }
            }
        } else if (activeTimer > 0f) {
            sideFadeAlpha = Calc.Approach(sideFadeAlpha, 0f, Engine.DeltaTime * 8f);
            if (beamSprite.CurrentAnimationID != "shoot") {
                beamSprite.Play("shoot", false, false);
                beamStartSprite.Play("shoot", true, false);
            }
            activeTimer -= Engine.DeltaTime;
            if (activeTimer > 0f) {
                PlayerCollideCheck();
            }
        }
    }

    private void DissipateParticles() {
        Level level = SceneAs<Level>();
        Vector2 vector = level.Camera.Position + new Vector2(160f, 90f);
        Vector2 vector2 = boss.BeamOrigin + Calc.AngleToVector(angle, BeamStartDist);
        Vector2 vector3 = boss.BeamOrigin + Calc.AngleToVector(angle, BeamLength);
        Vector2 vector4 = (vector3 - vector2).Perpendicular().SafeNormalize();
        Vector2 value = (vector3 - vector2).SafeNormalize();
        Vector2 min = -vector4 * 1f;
        Vector2 max = vector4 * 1f;
        float direction = vector4.Angle();
        float direction2 = (-vector4).Angle();
        float num = Vector2.Distance(vector, vector2) - BeamStartDist;
        vector = Calc.ClosestPointOnLine(vector2, vector3, vector);
        for (int i = 0; i < 200; i += 12) {
            for (int j = -1; j <= 1; j += 2) {
                level.ParticlesFG.Emit(FinalBossBeam.P_Dissipate, vector + value * i + vector4 * 2f * j + Calc.Random.Range(min, max), direction);
                level.ParticlesFG.Emit(FinalBossBeam.P_Dissipate, vector + value * i - vector4 * 2f * j + Calc.Random.Range(min, max), direction2);
                if (i != 0 && i < num) {
                    level.ParticlesFG.Emit(FinalBossBeam.P_Dissipate, vector - value * i + vector4 * 2f * j + Calc.Random.Range(min, max), direction);
                    level.ParticlesFG.Emit(FinalBossBeam.P_Dissipate, vector - value * i - vector4 * 2f * j + Calc.Random.Range(min, max), direction2);
                }
            }
        }
    }

    private void PlayerCollideCheck() {
        Vector2 vector = boss.BeamOrigin + Calc.AngleToVector(angle, BeamStartDist);
        Vector2 vector2 = boss.BeamOrigin + Calc.AngleToVector(angle, BeamLength);
        Vector2 value = (vector2 - vector).Perpendicular().SafeNormalize(CollideCheckSep);

        Player player = Scene.CollideFirst<Player>(vector + value, vector2 + value);
        player ??= Scene.CollideFirst<Player>(vector - value, vector2 - value);
        player ??= Scene.CollideFirst<Player>(vector, vector2);
        player?.Die((player.Center - boss.BeamOrigin).SafeNormalize(), false, true);
    }

    public override void Render() {
        Vector2 vector = boss.BeamOrigin;
        Vector2 vector2 = Calc.AngleToVector(angle, beamSprite.Width);
        beamSprite.Rotation = angle;
        beamSprite.Color = Color.White * beamAlpha;
        beamStartSprite.Rotation = angle;
        beamStartSprite.Color = Color.White * beamAlpha;
        if (beamSprite.CurrentAnimationID == "shoot") {
            vector += Calc.AngleToVector(angle, 8f);
        }
        for (int i = 0; i < BeamsDrawn; i++) {
            beamSprite.RenderPosition = vector;
            beamSprite.Render();
            vector += vector2;
        }
        if (beamSprite.CurrentAnimationID == "shoot") {
            beamStartSprite.RenderPosition = boss.BeamOrigin;
            beamStartSprite.Render();
        }
        GameplayRenderer.End();
        Vector2 vector3 = vector2.SafeNormalize();
        Vector2 vector4 = vector3.Perpendicular();
        Color color = Color.Black * sideFadeAlpha * SideDarknessAlpha;
        Color transparent = Color.Transparent;
        vector3 *= 4000f;
        vector4 *= 120f;
        int index = 0;
        Quad(ref index, vector, -vector3 + vector4 * 2f, vector3 + vector4 * 2f, vector3 + vector4, -vector3 + vector4, color, color);
        Quad(ref index, vector, -vector3 + vector4, vector3 + vector4, vector3, -vector3, color, transparent);
        Quad(ref index, vector, -vector3, vector3, vector3 - vector4, -vector3 - vector4, transparent, color);
        Quad(ref index, vector, -vector3 - vector4, vector3 - vector4, vector3 - vector4 * 2f, -vector3 - vector4 * 2f, color, color);
        GFX.DrawVertices((Scene as Level)!.Camera.Matrix, fade, fade.Length, null, null);
        GameplayRenderer.Begin();
    }

    private void Quad(ref int v, Vector2 offset, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color ab, Color cd) {
        ref var vertex = ref fade[v++];
        vertex.Color = ab;
        vertex.Position.X = offset.X + a.X;
        vertex.Position.Y = offset.Y + a.Y;

        vertex = ref fade[v++];
        vertex.Color = ab;
        vertex.Position.X = offset.X + b.X;
        vertex.Position.Y = offset.Y + b.Y;

        vertex = ref fade[v++];
        vertex.Color = cd;
        vertex.Position.X = offset.X + c.X;
        vertex.Position.Y = offset.Y + c.Y;

        vertex = ref fade[v++];
        vertex.Color = ab;
        vertex.Position.X = offset.X + a.X;
        vertex.Position.Y = offset.Y + a.Y;

        vertex = ref fade[v++];
        vertex.Color = cd;
        vertex.Position.X = offset.X + c.X;
        vertex.Position.Y = offset.Y + c.Y;

        vertex = ref fade[v++];
        vertex.Color = cd;
        vertex.Position.X = offset.X + d.X;
        vertex.Position.Y = offset.Y + d.Y;
    }

    public void Destroy() {
        RemoveSelf();
    }

    public static ParticleType P_Dissipate;

    public const float ChargeTime = 1.4f;

    public const float FollowTime = 0.9f;

    public const float ActiveTime = 0.12f;

    private const float AngleStartOffset = 100f;

    public float RotationSpeed = 200f;

    private const float CollideCheckSep = 2f;

    private const float BeamLength = 2000f;

    private const float BeamStartDist = 12f;

    private const int BeamsDrawn = 15;

    private const float SideDarknessAlpha = 0.35f;

    private FinalBoss boss;

    private Player player;

    private Sprite beamSprite;

    private Sprite beamStartSprite;

    private float chargeTimer;

    private float followTimer;

    private float activeTimer;

    private float angle;

    private float beamAlpha;

    private float sideFadeAlpha;

    private VertexPositionColor[] fade;
}