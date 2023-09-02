namespace FrostHelper;

[CustomEntity($"FrostHelper/KeyIce = {nameof(Load_ThanksCrystallineHelper_I_LOVE_WHEN_MODS_HOOK_OTHER_MODS)}")]
[Tracked]
public sealed class KeyIce : Key {
    #region Hooks
    private static bool _hooksLoaded = false;

    [OnLoad]
    public static void LoadHooksIfNeeded() {
        if (_hooksLoaded) {
            return;
        }
        _hooksLoaded = true;

        On.Celeste.Level.LoadLevel += Level_LoadLevel;
        On.Celeste.Key.RegisterUsed += Key_RegisterUsed;
    }

    private static void Key_RegisterUsed(On.Celeste.Key.orig_RegisterUsed orig, Key self) {
        orig(self);

        if (self is KeyIce iceKey) {
            iceKey.OnRegisterUsed();
        }
    }

    private static void Level_LoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerIntro, bool isFromLoader) {
        orig(self, playerIntro, isFromLoader);

        var dissolvedBeforeDeath = FrostModule.Session.PersistentIceKeysDissolvedThisRun;
        foreach (var info in dissolvedBeforeDeath) {
            if (info.RespawnPoint != self.Session.RespawnPoint || playerIntro == Player.IntroTypes.Transition) {
                FrostModule.Session.PersistentIceKeys.RemoveWhere(k => k.ID.Key == info.ID.Key);
                self.Session.DoNotLoad.Remove(info.ID);
            }
        }
        dissolvedBeforeDeath.Clear();

        if (playerIntro != Player.IntroTypes.Transition) {
            foreach (var id in FrostModule.Session.PersistentIceKeys) {
                self.Add(new KeyIce(self.Tracker.GetEntity<Player>(), id));
                //self.Session.DoNotLoad.Add(id.ID);
            }
        }
    }

    [OnUnload]
    public static void Unload() {
        if (!_hooksLoaded) {
            return;
        }
        _hooksLoaded = false;

        On.Celeste.Level.LoadLevel -= Level_LoadLevel;
        On.Celeste.Key.RegisterUsed -= Key_RegisterUsed;
    }
    #endregion

    public new bool Turning { get; private set; }

    public string? OnCarryFlag { get; private set; }

    public bool Persistent { get; private set; }

    public bool LoadedFromPersistence { get; private set; }

    public EntityData SourceData { get; private set; }

    private FrostHelperSession.DissolvedIceKeyInfo? DissolveInfo { get; set; }


    // a hook on the ctor for ice keys forces me to use a ctor that's invalid for the CustomEntity attribute :/
    public static Entity Load_ThanksCrystallineHelper_I_LOVE_WHEN_MODS_HOOK_OTHER_MODS(Level level, LevelData levelData, Vector2 offset, EntityData entityData) {
        return new KeyIce(entityData, offset, new(levelData.Name, entityData.ID), entityData.NodesOffset(offset));
    }

    // hooked by CrystallineHelper
    public KeyIce(EntityData data, Vector2 offset, EntityID id, Vector2[] nodes) : this(data, offset, id) { }

    public KeyIce(EntityData data, Vector2 offset, EntityID id) : base(data, offset, id) {
        SourceData = data;

        OnCarryFlag = data.Attr("onCarryFlag", "") is { } f && !string.IsNullOrWhiteSpace(f) ? f : null;
        Persistent = data.Bool("persistent", false);

        sprite = Get<Sprite>();
        this.follower = Get<Follower>();
        FrostModule.SpriteBank.CreateOn(sprite, "keyice");

        Follower follower = this.follower;
        // don't dissolve persistent keys, as that would toggle the onCarryFlag
        if (!Persistent) {
            follower.OnLoseLeader += Dissolve;
        }
        this.follower.PersistentFollow = true; // was false

        // fix bug where dying immediately after grabbing the key would make you gain a regular key after death
        var pc = Get<PlayerCollider>();
        var origOnCollide = pc.OnCollide;
        pc.OnCollide = (Player p) => {
            origOnCollide(p);

            var session = p.SceneAs<Level>().Session;
            RemoveFromDoNotLoad(session);

            if (Persistent && DissolveInfo is { })
                FrostModule.Session.PersistentIceKeysDissolvedThisRun.Remove(DissolveInfo);

            if (OnCarryFlag is { }) {
                session.SetFlag(OnCarryFlag);
            }
        };

        Add(new DashListener {
            OnDash = OnDash
        });
        Add(new TransitionListener {
            OnOut = delegate (float f) {
                StartedUsing = false;
                if (!IsUsed) {
                    if (tween != null) {
                        tween.RemoveSelf();
                        tween = null;
                    }
                    if (alarm != null) {
                        alarm.RemoveSelf();
                        alarm = null;
                    }
                    Turning = false;

                    if (Visible) {
                        sprite.Rate = 1f;
                        sprite.Scale = Vector2.One;
                        sprite.Play("idle", false, false);
                        sprite.Rotation = 0f;
                        this.follower.MoveTowardsLeader = true;
                    }
                    /*
                    Visible = true;
                    sprite.Visible = true;
                    */


                    if (Persistent && follower.HasLeader) {
                        LoadedFromPersistence = true;
                        Persistent = false;
                    }
                }
            }
        });

        start = Position;
    }

    public KeyIce(Player player, FrostHelperSession.IceKeyInfo info) : this(new() { Values = info.Data }, player.Position + new Vector2((-12) * (int) player.Facing, -8f), info.ID) {
        player.Leader.GainFollower(follower);
        Collidable = false;
        Depth = -1000000;
        LoadedFromPersistence = true;
        Persistent = false;

        startLevel = info.ID.Level;
        start = info.KeyStartPos;
    }

    private void OnRegisterUsed() {
        if (Persistent || LoadedFromPersistence) {
            FrostModule.Session.PersistentIceKeys.RemoveWhere(info => info.ID.Key == ID.Key);
        }

        if (OnCarryFlag is { } && SceneAs<Level>()?.Session is { } session) {
            session.SetFlag(OnCarryFlag, false);
        }
    }

    private void OnDash(Vector2 dir) {
        if (follower.Leader != null) {
            Dissolve();
        }
    }

    public override void Added(Scene scene) {
        base.Added(scene);
        if (scene is Level level) {
            UpdateStartLocation(level, false);

            if (follower.HasLeader && OnCarryFlag is { } && level.Session is { } session) {
                session.SetFlag(OnCarryFlag);
            }
        }
    }

    public override void Awake(Scene scene) {
        base.Awake(scene);

        if (LoadedFromPersistence /*.Cast<KeyIce>().Any(k => k.LoadedFromPersistence && k.ID.Key == ID.Key)*/) {
            foreach (KeyIce k in scene.Tracker.SafeGetEntities<KeyIce>()) {
                Console.WriteLine(k.ID.Key);

                if (!k.LoadedFromPersistence && k.ID.Key == ID.Key) {
                    k.Visible = false;
                    k.Active = false;
                    k.RemoveSelf();
                }
            }
        }
    }

    private void UpdateStartLocation(Level level, bool forceSetStartLevel) {
        if (forceSetStartLevel)
            startLevel = null!;

        startLevel ??= level.Session.Level;
    }

    internal void RemoveFromDoNotLoad(Session session, bool dissolving = false) {
        session.Keys.Remove(ID);

        if (dissolving)
            DissolveInfo ??= new(ID, session.RespawnPoint);

        if (Persistent) {
            FrostModule.Session.PersistentIceKeys.Add(new(ID, SourceData.Values, start));

            if (dissolving)
                FrostModule.Session.PersistentIceKeysDissolvedThisRun.Add(DissolveInfo!);
            session.UpdateLevelStartDashes();

            if (dissolving)
                session.DoNotLoad.Remove(ID);
        } else if (LoadedFromPersistence) {
            if (dissolving)
                FrostModule.Session.PersistentIceKeysDissolvedThisRun.Add(DissolveInfo!);
            
            session.DoNotLoad.Remove(ID);
        } else {
            session.DoNotLoad.Remove(ID);
        }
    }

    public override void Update() {
        var session = (Scene as Level)?.Session;

        if (IsUsed && !wasUsed) {
            session?.DoNotLoad.Add(ID);
            wasUsed = true;
        }

        if (!dissolved && !IsUsed && !base.Turning) {
            if (session != null && session.Keys.Contains(ID)) {
                RemoveFromDoNotLoad(session);
            }
        }

        base.Update();
    }

    public void Dissolve() {
        if (!(dissolved || IsUsed || base.Turning)) {
            dissolved = true;
            if (follower.Leader != null) {
                Player player = (follower.Leader.Entity as Player)!;
                player.StrawberryCollectResetTimer = 2.5f;
                follower.Leader.LoseFollower(follower);
            }

            if (OnCarryFlag is { }) {
                SceneAs<Level>().Session.SetFlag(OnCarryFlag, false);
            }

            Add(new Coroutine(DissolveRoutine(), true));
        }
    }

    // hooked by CrystallineHelper
    private IEnumerator DissolveRoutine() {
        Level level = (Scene as Level)!;
        RemoveFromDoNotLoad(level.Session, true);

        Audio.Play("event:/game/general/seed_poof", Position);
        Collidable = false;
        sprite.Scale = Vector2.One * 0.5f;
        yield return 0.05f;
        Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
        int num;
        for (int i = 0; i < 6; i = num + 1) {
            float dir = Calc.Random.NextFloat(6.28318548f);
            level.ParticlesFG.Emit(StrawberrySeed.P_Burst, 1, Position + Calc.AngleToVector(dir, 4f), Vector2.Zero, dir);
            num = i;
        }
        sprite.Scale = Vector2.Zero;
        Visible = false;

        if (level.Session.Level != startLevel) {
            RemoveSelf();
            yield break;
        }
        yield return 0.3f;

        dissolved = false;
        Audio.Play("event:/game/general/seed_reappear", Position);
        Position = start;
        sprite.Scale = Vector2.One;
        Visible = true;
        Collidable = true;
        level.Displacement.AddBurst(Position, 0.2f, 8f, 28f, 0.2f, null, null);
        yield break;
    }

    private Vector2 start;

    private string startLevel;

    private bool dissolved;

    private bool wasUsed;
}


