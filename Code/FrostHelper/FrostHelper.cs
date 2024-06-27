using Celeste.Mod;
using FrostHelper.Entities;
using FrostHelper.Entities.Boosters;
using FrostHelper.EXPERIMENTAL;
using FrostHelper.Helpers;
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

    [YamlIgnore]
    private bool _noRespawnGC = false;
    [SettingSubText("""
        [EXPERIMENTAL]
        Removes the aggressive GC call in Level.Reload.
        In some cases, this can massively reduce load times.
        Make sure to report any new lag spikes if using this!
        """)]
    [SettingName("No Respawn GC")]
    public bool NoRespawnGC {
        get => _noRespawnGC;
        set {
            switch (value) {
                case true:
                    ReloadGCEdit.Load();
                    break;
                case false:
                    ReloadGCEdit.Unload();
                    break;
            }

            _noRespawnGC = value;
        }
    }

    [YamlIgnore]
    private bool _DebugMapCulling = false;
    [SettingSubText("""
        [EXPERIMENTAL]
        Adds camera culling to the debug map,
        massively improving performance in large maps.
        """)]
    [SettingName("Debug Map Culling")]
    public bool DebugMapCulling {
        get => _DebugMapCulling;
        set {
            switch (value) {
                case true:
                    OptimiseDebugMap.Load();
                    break;
                case false:
                    OptimiseDebugMap.Unload();
                    break;
            }

            _DebugMapCulling = value;
        }
    }
    /*
    [YamlIgnore]
    private bool _noSceneChangeGC = false;
    [SettingSubText("""
        [EXPERIMENTAL]
        Removes the aggressive GC call in Engine.OnSceneTransition.
        This can massively reduce load times when loading a map from the debug map.
        Make sure to report any new lag spikes if using this!
        """)]
    [SettingName("No Scene Change GC")]
    public bool NoSceneChangeGC {
        get => _noSceneChangeGC;
        set {
            switch (value) {
                case true:
                    ReloadGCEdit.LoadSceneTransition();
                    break;
                case false:
                    ReloadGCEdit.UnloadSceneTransition();
                    break;
            }

            _noSceneChangeGC = value;
        }
    }*/
}

public enum FrameworkType {
    FNA, XNA
}

public class FrostModule : EverestModule {
    public static SpriteBank SpriteBank;
    // Only one alive module instance can exist at any given time.
    public static FrostModule Instance;

    /// <summary>
    /// The framework celeste is running on - either xna or fna
    /// </summary>
    public static readonly FrameworkType Framework;

    static FrostModule() {
        // from communal helper - https://github.com/CommunalHelper/CommunalHelper/blob/6877bdf1e3527656adcdb56a89071da6fe4e42bf/src/Entities/Misc/Shape3DRenderer.cs#L185-L187
        Framework = typeof(Game).Assembly.FullName!.Contains("FNA")
        ? FrameworkType.FNA
        : FrameworkType.XNA;
    }

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

#if MAP_PROCESSOR
    public override void PrepareMapDataProcessors(MapDataFixup context) {
        base.PrepareMapDataProcessors(context);

        FrostMapDataProcessor.GlobalEntityMarkers.Remove(context.AreaKey.SID);
        context.Add<FrostMapDataProcessor>();
    }
#else
#warning ENABLE MAP PROCESSOR IN RELEASES!!!
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
        SpeedrunToolIntegration.LoadIfNeeded();
        
        SpeedrunToolIntegration.AddReturnSameObjectProcessor?.Invoke(t => 
            t.IsAssignableTo(typeof(ISavestatePersisted))
        );
    }

    private static List<ILHook> registeredHooks = new List<ILHook>();
    public static void RegisterILHook(ILHook hook) {
        registeredHooks.Add(hook);
    }

    // Set up any hooks, event handlers and your mod in general here.
    // Load runs before Celeste itself has initialized properly.
    public override void Load() {
        typeof(API.API).ModInterop();

        // Custom dream blocks and feathers
        On.Celeste.Player.UpdateSprite += Player_UpdateSprite;

        AttributeHelper.InvokeAllWithAttribute(typeof(OnLoad));

        if (!Settings.HookLazyLoading) {
            AttributeHelper.InvokeAllWithAttribute(typeof(HookPreload));
        }
        
        Everest.Events.Player.OnRegisterStates += PlayerOnOnRegisterStates;
        Everest.Content.OnUpdate += ContentOnOnUpdate;
    }

    internal delegate void OnSpriteChangedHandler(ModAsset old, ReadOnlySpan<char> texturePath);

    internal static event OnSpriteChangedHandler OnSpriteChanged;

    private void ContentOnOnUpdate(ModAsset? from, ModAsset? to) {
        if (from is null)
            return;
        if (to is {})
            ShaderHelperIntegration.Content_OnUpdate(from, to);

        if (from.PathVirtual is null)
            return;
        
        var virtPath = from.PathVirtual.AsSpan();
        if (virtPath.StartsWith("Graphics/Atlases/Gameplay/")) {
            virtPath = virtPath["Graphics/Atlases/Gameplay/".Length..];

            OnSpriteChanged?.Invoke(from, virtPath);
        }
    }

    private void PlayerOnOnRegisterStates(Player self) {
        // used for dream blocks, this runs in the ctor so let's do that
        DynamicData.For(self).Set("lastDreamSpeed", 0f);

        GenericCustomBooster.CustomBoostState = self.AddState("Custom Boost", GenericCustomBooster.BoostUpdate, GenericCustomBooster.BoostCoroutine, GenericCustomBooster.BoostBegin, GenericCustomBooster.BoostEnd);
        GenericCustomBooster.CustomRedBoostState = self.AddState("Custom Red Boost", GenericCustomBooster.RedDashUpdate, GenericCustomBooster.RedDashCoroutine, GenericCustomBooster.RedDashBegin, GenericCustomBooster.RedDashEnd);
#pragma warning disable CS0618 // Type or member is obsolete
        CustomDreamDashState = self.AddState("Custom Dream Dash (Obsolete)", CustomDreamBlock.DreamDashUpdate, null!, CustomDreamBlock.DreamDashBegin, CustomDreamBlock.DreamDashEnd);
#pragma warning restore CS0618 // Type or member is obsolete
        
        CustomFeather.CustomFeatherState = self.AddState("Custom Feather", CustomFeather.StarFlyUpdate, CustomFeather.CustomFeatherCoroutine, CustomFeather.CustomFeatherBegin, CustomFeather.CustomFeatherEnd);
        HeldRefill.HeldDashState = self.AddState("Held Dash", HeldRefill.HeldDashUpdate, HeldRefill.HeldDashRoutine, HeldRefill.HeldDashBegin, HeldRefill.HeldDashEnd);
        WASDMovementState.ID = self.AddState(WASDMovementState.GetTasToolsDisplayName(), WASDMovementState.Update, null!, WASDMovementState.Begin, WASDMovementState.End);
    }

    public static List<Entity> CollideAll(Entity entity) {
        List<Entity> collided = [];
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

    public static FieldInfo player_boostTarget = typeof(Player).GetField("boostTarget", BindingFlags.Instance | BindingFlags.NonPublic)!;
    public static FieldInfo player_calledDashEvents = typeof(Player).GetField("calledDashEvents", BindingFlags.Instance | BindingFlags.NonPublic)!;
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
        Everest.Events.Player.OnRegisterStates -= PlayerOnOnRegisterStates;

        // For custom Boosters
        //On.Celeste.Player.CallDashEvents -= Player_CallDashEvents;

        // Custom dream blocks and feathers
        On.Celeste.Player.UpdateSprite -= Player_UpdateSprite;

        foreach (var hook in registeredHooks) {
            hook.Dispose();
        }
        registeredHooks = [];

        AttributeHelper.InvokeAllWithAttribute(typeof(OnUnload));

        OutlineHelper.Dispose();
        
        Everest.Content.OnUpdate -= ContentOnOnUpdate;
    }

    // Optional, initialize anything after Celeste has initialized itself properly.
    public override void Initialize() {
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
            return Type.EmptyTypes;
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
            return [];
        }

        string[] split = typeString.Trim().Split(',');
        var parsed = new List<Type>(split.Length);
        for (int i = 0; i < split.Length; i++) {
            parsed[i] = TypeHelper.EntityNameToType(split[i].Trim());
        }
        return parsed;
    }

    /// <summary>
    /// Returns a hash set of types from a comma-separated string of types
    /// </summary>
    public static HashSet<Type> GetTypesAsHashSet(string typeString) {
        if (typeString == string.Empty) {
            return [];
        }

        string[] split = typeString.Trim().Split(',');
        var parsed = new HashSet<Type>();
        for (int i = 0; i < split.Length; i++) {
            parsed.Add(TypeHelper.EntityNameToType(split[i].Trim()));
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

    public static Level? TryGetCurrentLevel() {
        return Engine.Scene switch {
            Level level => level,
            LevelLoader loader => loader.Level,
            AssetReloadHelper => AssetReloadHelper.ReturnToScene as Level,
            _ => null
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
        Console.WriteLine("Components:");
        Print(Engine.Scene.Entities.SelectMany(s => s));
        Console.WriteLine("Entities:");
        Print(Engine.Scene.Entities);

        void Print(IEnumerable<object> obj) {
            var types = obj.Select(e => e.GetType()).ToList();
            var longestType = types.Max(t => (t.FullName ?? t.Name).Length);

            types.Distinct()
                 .ToDictionary(t => t.FullName ?? t.Name, t => types.Count(t2 => t2 == t))
                 .OrderBy(p => p.Value) // while OrderByDescending might make more sense, this ordering makes it easier to read in the console
                 .Select(p => $"{p.Key}{new string(' ', longestType - p.Key.Length)} {p.Value}")
                 .Foreach(Console.WriteLine);
        }
    }

    [Command("shader_info", "[Frost Helper] Prints out information about a shader")]
    public static void CmdShaderInfo(string shaderName) {
        var shader = ShaderHelperIntegration.GetEffect(shaderName);

        Console.WriteLine($"{shaderName}:\n{shader.Parameters.Aggregate("Parameters:", (string p1, EffectParameter p2) => $"{p1}\n{p2.Name}: {p2.ParameterType}")}");
    }

    [Command("fh_attached", "[Frost Helper] Prints out information about all data attached to entities by Frost Helper")]
    public static void CmdListAttached() {
        Console.WriteLine("Attached Data:");
        foreach (var item in Engine.Scene.Entities.entities.Cast<object>().Concat(GetCurrentLevel().Foreground.Backdrops).Concat(GetCurrentLevel().Background.Backdrops)) {
            if (AttachedDataHelper.GetAllData(item) is { } data)
                Console.WriteLine($"{item} -> {string.Join(",", data)}");
        }
    }
}
