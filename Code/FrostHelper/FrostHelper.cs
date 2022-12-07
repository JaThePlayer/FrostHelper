using Celeste.Mod;
using FrostHelper.Entities.Boosters;
using FrostHelper.EXPERIMENTAL;
using FrostHelper.ModIntegration;
using MonoMod.ModInterop;
using YamlDotNet.Serialization;

namespace FrostHelper;

public class FrostHelperSettings : EverestModuleSettings {

    [SettingSubText("""
        Only loads hooks once they're needed, reducing load times and debloating stack traces.
        Can cause small lag spikes on transitions.
        """)]
    public bool HookLazyLoading { get; set; } = false;

    [YamlIgnore]
    private bool _wireCulling = false; 
    
    [SettingSubText("""
        [EXPERIMENTAL]
        Culls Wires, Cobwebs and Flaglines,
        making them not render if not visible.
        Make sure to report any issues if using this!
        """)]
    public bool WireCulling {
        get => _wireCulling;
        set {
            switch (value) {
                case true:
                    FlaglineCull.Load();
                    break;
                case false:
                    FlaglineCull.Unload();
                    break;
            }

            _wireCulling = value;
        }
    }

    [YamlIgnore]
    private bool _fastShapeDraw = false;
    [SettingSubText("""
        [EXPERIMENTAL]
        Optimises any Monocle.Draw.* functions to use a Texture2D instead of a VirtTexture.
        This reduces the overhead of drawing shapes, improving performance.
        Make sure to report any issues if using this!
        """)]
    public bool FastShapeDraw {
        get => _fastShapeDraw;
        set {
            switch (value) {
                case true:
                    MonocleDrawShapeFixer.Load();
                    break;
                case false:
                    MonocleDrawShapeFixer.Unload();
                    break;
            }

            _fastShapeDraw = value;
        }
    }
}

public class FrostModule : EverestModule {
    static bool outBackHelper = false;
    public static SpriteBank SpriteBank;
    // Only one alive module instance can exist at any given time.
    public static FrostModule Instance;

    public FrostModule() {
        Instance = this;
    }
    // no save data needed
    public override Type SaveDataType => typeof(FrostHelperSaveData);
    public static FrostHelperSaveData SaveData => (FrostHelperSaveData) Instance._SaveData;
    public override Type SessionType => typeof(FrostHelperSession);
    public static FrostHelperSession Session => (FrostHelperSession) Instance._Session;

    public override Type SettingsType => typeof(FrostHelperSettings);
    public static FrostHelperSettings Settings => (FrostHelperSettings) Instance._Settings;

#if SPEEDCHALLENGES
    public override void PrepareMapDataProcessors(MapDataFixup context) {
        base.PrepareMapDataProcessors(context);

        FrostMapDataProcessor.GlobalEntityMarkers.Remove(context.AreaKey.SID);
        context.Add<FrostMapDataProcessor>();
    }
#endif
    public override void LoadContent(bool firstLoad) {
        SpriteBank = new SpriteBank(GFX.Game, "Graphics/FrostHelper/CustomSprites.xml");
        BadelineChaserBlock.Load();
        BadelineChaserBlockActivator.Load();

#if PORTALGUN
            if (Everest.Loader.DependencyLoaded(new EverestModuleMetadata() { Name = "OutbackHelper" }))
            {
                outBackHelper = true;
                typeof(FrostModule).Assembly.GetType("FrostTempleHelper.Entities.azcplo1k.abcdhr").GetMethod("Load").Invoke(null, new object[0]);
            }
#endif

        AttributeHelper.InvokeAllWithAttribute(typeof(OnLoadContent));
        GravityHelperIntegration.Load();
    }

    private static List<ILHook> registeredHooks = new List<ILHook>();
    public static void RegisterILHook(ILHook hook) {
        registeredHooks.Add(hook);
    }

    // Set up any hooks, event handlers and your mod in general here.
    // Load runs before Celeste itself has initialized properly.
    public override void Load() {
        typeof(API.API).ModInterop();

        // Legacy entity creation (for back when we didn't have the CustomEntity attribute)
        Everest.Events.Level.OnLoadEntity += OnLoadEntity;

        // Register new states
        On.Celeste.Player.ctor += Player_ctor;

        // Custom dream blocks and feathers
        On.Celeste.Player.UpdateSprite += Player_UpdateSprite;

        AttributeHelper.InvokeAllWithAttribute(typeof(OnLoad));

        if (!Settings.HookLazyLoading) {
            AttributeHelper.InvokeAllWithAttribute(typeof(HookPreload));
        }
    }

    public static List<Entity> CollideAll(Entity entity) {
        List<Entity> collided = new List<Entity>();
        foreach (Entity e in entity.Scene.Entities) {
            if (entity.CollideCheck(e))
                collided.Add(e);
        }

        return collided;
    }

    public static int GetFeatherState(int old, Player player) {
        return player.StateMachine.State == CustomFeather.CustomFeatherState ? CustomFeather.CustomFeatherState : old;
    }

    public static int GetBoosterState(int old, Player player) {
        int state = player.StateMachine.State;
        if (API.API.IsInCustomBoostState(player))
            return GenericCustomBooster.CustomBoostState;
#if OLD_YELLOW_BOOSTER
        if (state == YellowBoostState)
            return YellowBoostState;
#endif
        return old;
    }

    public static int GetRedDashState(int orig, Player player) {
        return player.StateMachine.State == GenericCustomBooster.CustomRedBoostState ? GenericCustomBooster.CustomRedBoostState : orig;
    }

    #region CustomDreamBlock

    public static int CustomDreamDashState = int.MaxValue;

    private static void Player_UpdateSprite(On.Celeste.Player.orig_UpdateSprite orig, Player self) {

        if (self.StateMachine.State == CustomDreamDashState) {
            if (self.Sprite.CurrentAnimationID != "dreamDashIn" && self.Sprite.CurrentAnimationID != "dreamDashLoop") {
                self.Sprite.Play("dreamDashIn", false, false);
            }
        } else if (self.StateMachine.State == CustomFeather.CustomFeatherState) {
            self.Sprite.Scale.X = Calc.Approach(self.Sprite.Scale.X, 1f, 1.75f * Engine.DeltaTime);
            self.Sprite.Scale.Y = Calc.Approach(self.Sprite.Scale.Y, 1f, 1.75f * Engine.DeltaTime);
        } else {
            orig(self);
        }
    }
    #endregion

    private void Player_ctor(On.Celeste.Player.orig_ctor orig, Player self, Vector2 position, PlayerSpriteMode spriteMode) {
        orig(self, position, spriteMode);
        DynamicData.For(self).Set("lastDreamSpeed", 0f);
        // Let's define new states
        // .AddState is defined in StateMachineExt
#if OLD_YELLOW_BOOSTER
        YellowBoostState = self.StateMachine.AddState(YellowBoostUpdate, YellowBoostCoroutine, YellowBoostBegin, YellowBoostEnd);
        ModIntegration.CelesteTASIntegration.RegisterState(YellowBoostState, "Yellow Boost");
#endif
        GenericCustomBooster.CustomBoostState = self.StateMachine.AddState(GenericCustomBooster.BoostUpdate, GenericCustomBooster.BoostCoroutine, GenericCustomBooster.BoostBegin, GenericCustomBooster.BoostEnd);
        GenericCustomBooster.CustomRedBoostState = self.StateMachine.AddState(GenericCustomBooster.RedDashUpdate, GenericCustomBooster.RedDashCoroutine, GenericCustomBooster.RedDashBegin, GenericCustomBooster.RedDashEnd);
#pragma warning disable CS0618 // Type or member is obsolete
        CustomDreamDashState = self.StateMachine.AddState(CustomDreamBlock.DreamDashUpdate, null!, CustomDreamBlock.DreamDashBegin, CustomDreamBlock.DreamDashEnd);
#pragma warning restore CS0618 // Type or member is obsolete
        CustomFeather.CustomFeatherState = self.StateMachine.AddState(CustomFeather.StarFlyUpdate, CustomFeather.CustomFeatherCoroutine, CustomFeather.CustomFeatherBegin, CustomFeather.CustomFeatherEnd);
        HeldRefill.HeldDashState = self.StateMachine.AddState(HeldRefill.HeldDashUpdate, HeldRefill.HeldDashRoutine, HeldRefill.HeldDashBegin, HeldRefill.HeldDashEnd);
        WASDMovementState.ID = self.StateMachine.AddState(WASDMovementState.Update, null!, WASDMovementState.Begin, WASDMovementState.End);

        CelesteTASIntegration.RegisterState(GenericCustomBooster.CustomBoostState, "Custom Boost");
        CelesteTASIntegration.RegisterState(GenericCustomBooster.CustomRedBoostState, "Custom Red Boost");
        CelesteTASIntegration.RegisterState(CustomDreamDashState, "Custom Dream Dash (Obsolete)");
        CelesteTASIntegration.RegisterState(CustomFeather.CustomFeatherState, "Custom Feather");
        CelesteTASIntegration.RegisterState(HeldRefill.HeldDashState, "Held Dash");
        CelesteTASIntegration.RegisterState(WASDMovementState.ID, WASDMovementState.GetTasToolsDisplayName());
    }

    public static FieldInfo player_boostTarget = typeof(Player).GetField("boostTarget", BindingFlags.Instance | BindingFlags.NonPublic);
    public static FieldInfo player_calledDashEvents = typeof(Player).GetField("calledDashEvents", BindingFlags.Instance | BindingFlags.NonPublic);
    #region YellowBoost
#if OLD_YELLOW_BOOSTER
    public static int YellowBoostState;

    private void YellowBoostBegin(Entity e) {
        Player player = (e as Player)!;
        player.CurrentBooster = null;
        Level level = player.SceneAs<Level>();
        bool? dontDrop;
        if (level == null) {
            dontDrop = null;
        } else {
            MapMetaModeProperties meta = level.Session.MapData.GetMeta();
            dontDrop = meta?.TheoInBubble;
        }

        YellowBoosterOLD GetBoosterThatIsBoostingPlayer() {
            return DynamicData.For(e as Player).Get<YellowBoosterOLD>("fh.customyellowBooster");
        }
        YellowBoosterOLD booster = GetBoosterThatIsBoostingPlayer();
        if (booster.DashRecovery == -1) {
            player.RefillDash();
        } else {
            player.Dashes = booster.DashRecovery;
        }

        player.RefillStamina();
        if (dontDrop.GetValueOrDefault()) {
            return;
        }
        player.Drop();
    }

    private int YellowBoostUpdate(Entity e) {
        Player player = (e as Player)!;
        Vector2 boostTarget = (Vector2) player_boostTarget.GetValue(player);
        Vector2 value = Input.Aim.Value * 3f;
        Vector2 vector = Calc.Approach(player.ExactPosition, boostTarget - player.Collider.Center + value, 80f * Engine.DeltaTime);
        player.MoveToX(vector.X, null);
        player.MoveToY(vector.Y, null);
        bool pressed = Input.Dash.Pressed || Input.CrouchDashPressed;
        // the state we should be in afterwards
        int result;
        if (pressed) {
            player.SetValue("demoDashed", Input.CrouchDashPressed);
            Input.Dash.ConsumePress();
            Input.CrouchDash.ConsumePress();
            result = Player.StDash;
        } else {
            result = YellowBoostState;
        }
        return result;
    }

    private void YellowBoostEnd(Entity e) {
        Player player = (e as Player)!;
        Vector2 boostTarget = (Vector2) player_boostTarget.GetValue(player);
        Vector2 vector = (boostTarget - player.Collider.Center).Floor();
        player.MoveToX(vector.X, null);
        player.MoveToY(vector.Y, null);
    }

    private IEnumerator YellowBoostCoroutine(Entity e) {
        Player player = (e as Player)!;
        YellowBoosterOLD booster = null!;
        foreach (YellowBoosterOLD b in player.Scene.Tracker.GetEntities<YellowBoosterOLD>()) {
            if (b.StartedBoosting) {
                booster = b;
                break;
            }
        }
        yield return booster.BoostTime / 6; // was 0.25
        booster.sprite.SetColor(booster.FlashTint);
        yield return booster.BoostTime / 3;
        booster.sprite.SetColor(Color.White);
        yield return booster.BoostTime / 6;
        booster.sprite.SetColor(booster.FlashTint);
        yield return booster.BoostTime / 3;
        booster.sprite.SetColor(Color.White);
        // Player didn't dash out, time to kill them :(
        player.Die(player.DashDir);
        booster.PlayerDied();
        //player.StateMachine.State = Player.StDash;
        yield break;
    }
#endif
    #endregion

    // Unload the entirety of your mod's content, remove any event listeners and undo all hooks.
    public override void Unload() {
        // Legacy entity creation (for back when we didn't have the CustomEntity attribute)
        Everest.Events.Level.OnLoadEntity -= OnLoadEntity;

        // Register new states
        On.Celeste.Player.ctor -= Player_ctor;

        // For custom Boosters
        //On.Celeste.Player.CallDashEvents -= Player_CallDashEvents;

        // Custom dream blocks and feathers
        On.Celeste.Player.UpdateSprite -= Player_UpdateSprite;

        if (outBackHelper)
            typeof(FrostModule).Assembly.GetType("FrostTempleHelper.Entities.azcplo1k.abcdhr").GetMethod("Unload").Invoke(null, new object[0]);

        foreach (var hook in registeredHooks) {
            hook.Dispose();
        }
        registeredHooks = new List<ILHook>();

        AttributeHelper.InvokeAllWithAttribute(typeof(OnUnload));

        OutlineHelper.Dispose();
    }

    // Optional, initialize anything after Celeste has initialized itself properly.
    public override void Initialize() {
    }

    private static bool OnLoadEntity(Level level, LevelData levelData, Vector2 offset, EntityData entityData) {
        switch (entityData.Name) {
            case "FrostHelper/KeyIce":
                level.Add(new KeyIce(entityData, offset, new EntityID(levelData.Name, entityData.ID), entityData.NodesOffset(offset)));
                return true;
            case "FrostHelper/CustomDreamBlock":
                if (entityData.Bool("old", false)) {
#pragma warning disable CS0618 // Type or member is obsolete
                    level.Add(new CustomDreamBlock(entityData, offset));
#pragma warning restore CS0618 // Type or member is obsolete
                } else {
                    level.Add(new CustomDreamBlockV2(entityData, offset));
                }
                return true;
            default:
                return false;
        }
    }

    public static Vector2 StringToVec2(string str) {
        string[] strSplit = str.Split(',');
        if (strSplit.Length < 2) {
            return new Vector2(float.Parse(strSplit[0]), float.Parse(strSplit[0]));
        }
        return new Vector2(float.Parse(strSplit[0]), float.Parse(strSplit[1]));
    }

    /// <summary>
    /// Returns a list of types from a comma-separated string of types
    /// </summary>
    public static Type[] GetTypes(string typeString) {
        if (typeString == string.Empty) {
            return new Type[0];
        }

        string[] split = typeString.Trim().Split(',');
        Type[] parsed = new Type[split.Length];
        for (int i = 0; i < split.Length; i++) {
            parsed[i] = TypeHelper.EntityNameToType(split[i].Trim());
        }
        return parsed;
    }

    /// <summary>
    /// Returns a list of types from a comma-separated string of types
    /// </summary>
    public static List<Type> GetTypesAsList(string typeString) {
        if (typeString == string.Empty) {
            return new();
        }

        string[] split = typeString.Trim().Split(',');
        var parsed = new List<Type>(split.Length);
        for (int i = 0; i < split.Length; i++) {
            parsed[i] = TypeHelper.EntityNameToType(split[i].Trim());
        }
        return parsed;
    }

    public static char[] GetCharArrayFromCommaSeparatedList(string list) {
        string[] split = list.Trim().Split(',');
        char[] ret = new char[split.Length];
        for (int i = 0; i < split.Length; i++) {
            ret[i] = split[i][0];
        }
        return ret;
    }

    public static Level GetCurrentLevel() {
        return Engine.Scene switch {
            Level level => level,
            LevelLoader loader => loader.Level,
            AssetReloadHelper => (Level) AssetReloadHelper.ReturnToScene,
            _ => throw new Exception("GetCurrentLevel called outside of a level... how did you manage that?")
        };
    }

    [Command("gc", "[Frost Helper] Forces an aggresive GC run")]
    public static void CmdGC() {
        for (int i = 0; i < 5; i++) {
            GC.Collect(3);
        }
    }

    [Command("flags", "[Frost Helper] Lists all flags")]
    public static void CmdFlags() {
        GetCurrentLevel().Session.Flags.Foreach(Console.WriteLine);
    }

    [Command("detailed_count", "[Frost Helper] Lists all entities by type")]
    public static void CmdDetailedCount() {
        var types = Engine.Scene.Entities.Select(e => e.GetType());
        var longestType = types.Max(t => t.FullName.Length);

        types.Distinct()
             .ToDictionary(t => t.FullName, t => types.Count(t2 => t2 == t))
             .OrderBy(p => p.Value) // while OrderByDescending might make more sense, this ordering makes it easier to read in the console
             .Select(p => $"{p.Key}{new string(' ', longestType - p.Key.Length)} {p.Value}")
             .Foreach(Console.WriteLine);
    }

    [Command("shader_info", "[Frost Helper] Prints out information about a shader")]
    public static void CmdShaderInfo(string shaderName) {
        var shader = ShaderHelperIntegration.GetEffect(shaderName);

        Console.WriteLine($"{shaderName}:\n{shader.Parameters.Aggregate("Parameters:", (string p1, EffectParameter p2) => $"{p1}\n{p2.Name}: {p2.ParameterType}")}");
    }
}
