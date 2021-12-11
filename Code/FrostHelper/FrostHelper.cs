using Celeste.Mod.Meta;
using FrostHelper.Entities.Boosters;
using System.IO;

namespace FrostHelper;

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
#if SPEEDCHALLENGES
    public override void PrepareMapDataProcessors(MapDataFixup context) {
        base.PrepareMapDataProcessors(context);


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
    }

    private static List<ILHook> registeredHooks = new List<ILHook>();
    public static void RegisterILHook(ILHook hook) {
        registeredHooks.Add(hook);
    }

    //[Command("createrainbow", "REMOVE THIS JA AAAAAA")]
    public static void CmdCreateRainbowImg() {
        int width = 1920;
        int height = 1080;
        Texture2D texture = new Texture2D(Engine.Graphics.GraphicsDevice, width, height);
        Color[] colors = new Color[width * height];

        for (int i = 0; i < width * height; i++) {
            colors[i] = ColorHelper.GetHue(Engine.Scene, new Vector2(i % width, i / width));
        }


        texture.SetData(colors);
        using (FileStream stream = File.Create(@"C:\Users\Jasio\Desktop\rainbow4.png"))
            texture.SaveAsPng(stream, width, height);
        texture.Dispose();
    }

    // Set up any hooks, event handlers and your mod in general here.
    // Load runs before Celeste itself has initialized properly.
    public override void Load() {
        // Legacy entity creation (for back when we didn't have the CustomEntity attribute)
        Everest.Events.Level.OnLoadEntity += OnLoadEntity;

        // Register new states
        On.Celeste.Player.ctor += Player_ctor;

        // For custom Boosters
        //On.Celeste.Player.CallDashEvents += Player_CallDashEvents;
        RegisterILHook(new ILHook(typeof(Player).GetMethod("CallDashEvents", BindingFlags.NonPublic | BindingFlags.Instance), modCallDashEvents));
        RegisterILHook(new ILHook(typeof(Player).GetMethod("orig_WindMove", BindingFlags.NonPublic | BindingFlags.Instance), modBoosterState));

        // Custom dream blocks and feathers
        On.Celeste.Player.UpdateSprite += Player_UpdateSprite;

        AttributeHelper.InvokeAllWithAttribute(typeof(OnLoad));

        //On.Celeste.CassetteBlock.BoxSide.Render += BoxSide_Render;
    }

    // TODO:REMOVE
    private void BoxSide_Render(On.Celeste.CassetteBlock.BoxSide.orig_Render orig, Entity self) {

    }

    public static List<Entity> CollideAll(Entity entity) {
        List<Entity> collided = new List<Entity>();
        foreach (Entity e in entity.Scene.Entities) {
            if (entity.CollideCheck(e))
                collided.Add(e);
        }

        return collided;
    }

    void modBoosterState(ILContext il) {
        ILCursor cursor = new ILCursor(il);
        while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcI4(Player.StBoost) && instr.Previous.MatchCallvirt<StateMachine>("get_State"))) {
            //cursor.Emit(OpCodes.Pop);
            cursor.Emit(OpCodes.Ldarg_0); // this
            //cursor.EmitDelegate<Func<Player, int>>(GetBoosterState);
            cursor.Emit(OpCodes.Call, typeof(FrostModule).GetMethod(nameof(GetBoosterState)));
        }
    }

    public static int GetFeatherState(int old, Player player) {
        return player.StateMachine.State == CustomFeather.CustomFeatherState ? CustomFeather.CustomFeatherState : old;
    }

    public static int GetBoosterState(int old, Player player) {
        int state = player.StateMachine.State;
        if (API.API.IsInCustomBoostState(player))
            return GenericCustomBooster.CustomBoostState;
        if (state == YellowBoostState)
            return YellowBoostState;
        return old;
    }

    public static int GetRedDashState(int orig, Player player) {
        return player.StateMachine.State == GenericCustomBooster.CustomRedBoostState ? GenericCustomBooster.CustomRedBoostState : orig;
    }

    #region CustomDreamBlock

    public static int CustomDreamDashState;

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

    public static Player StateGetPlayer() {
        // TODO: Make smarter
        return (Engine.Scene as Level).Tracker.GetEntity<Player>();
    }

    private void modCallDashEvents(ILContext il) {
        var cursor = new ILCursor(il);

        if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchStfld<Player>("calledDashEvents"))) {
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<Player, bool>>((Player self) => {
                if (GenericCustomBooster.GetBoosterThatIsBoostingPlayer(self) != null) {
                    player_calledDashEvents.SetValue(self, false);
                    return false;
                }


                foreach (YellowBooster b in self.Scene.Tracker.GetEntities<YellowBooster>()) {
                    b.sprite.SetColor(Color.White);
                    if (b.StartedBoosting) {
                        b.PlayerBoosted(self, self.DashDir);
                        player_calledDashEvents.SetValue(self, false);
                        return false;
                    }
                    if (b.BoostingPlayer) {
                        player_calledDashEvents.SetValue(self, false);
                        return false;
                    }
                }
                foreach (GenericCustomBooster b in self.Scene.Tracker.GetEntities<GenericCustomBooster>()) {
                    if (b.StartedBoosting && b.CollideCheck(self)) {
                        b.PlayerBoosted(self, self.DashDir);
                        return false;
                    }
                    if (b.BoostingPlayer && GenericCustomBooster.GetBoosterThatIsBoostingPlayer(self) == b) {
                        return false;
                    }
                }

                return true;
            });

            // if delegateOut == false: return
            cursor.Emit(OpCodes.Brtrue, cursor.Instrs[cursor.Index]);
            cursor.Emit(OpCodes.Ret);
        }
    }

    private void Player_ctor(On.Celeste.Player.orig_ctor orig, Player self, Vector2 position, PlayerSpriteMode spriteMode) {
        orig(self, position, spriteMode);
        new DynData<Player>(self).Set("lastDreamSpeed", 0f);
        // Let's define new states
        // .AddState is defined in StateMachineExt
        YellowBoostState = self.StateMachine.AddState(YellowBoostUpdate, YellowBoostCoroutine, YellowBoostBegin, YellowBoostEnd);
        GenericCustomBooster.CustomBoostState = self.StateMachine.AddState(GenericCustomBooster.BoostUpdate, GenericCustomBooster.BoostCoroutine, GenericCustomBooster.BoostBegin, GenericCustomBooster.BoostEnd);
        GenericCustomBooster.CustomRedBoostState = self.StateMachine.AddState(GenericCustomBooster.RedDashUpdate, GenericCustomBooster.RedDashCoroutine, GenericCustomBooster.RedDashBegin, GenericCustomBooster.RedDashEnd);
#pragma warning disable CS0618 // Type or member is obsolete
        CustomDreamDashState = self.StateMachine.AddState(CustomDreamBlock.DreamDashUpdate, null, CustomDreamBlock.DreamDashBegin, CustomDreamBlock.DreamDashEnd);
#pragma warning restore CS0618 // Type or member is obsolete
        CustomFeather.CustomFeatherState = self.StateMachine.AddState(CustomFeather.StarFlyUpdate, CustomFeather.CustomFeatherCoroutine, CustomFeather.CustomFeatherBegin, CustomFeather.CustomFeatherEnd);
        HeldRefill.HeldDashState = self.StateMachine.AddState(HeldRefill.HeldDashUpdate, HeldRefill.HeldDashRoutine, HeldRefill.HeldDashBegin, HeldRefill.HeldDashEnd);
        WASDMovementState.ID = self.StateMachine.AddState(WASDMovementState.Update, null, WASDMovementState.Begin, WASDMovementState.End);

        ModIntegration.CelesteTASIntegration.RegisterState(YellowBoostState, "Yellow Boost");
        ModIntegration.CelesteTASIntegration.RegisterState(GenericCustomBooster.CustomBoostState, "Custom Boost");
        ModIntegration.CelesteTASIntegration.RegisterState(GenericCustomBooster.CustomRedBoostState, "Custom Red Boost");
        ModIntegration.CelesteTASIntegration.RegisterState(CustomDreamDashState, "Custom Dream Dash (Obsolete)");
        ModIntegration.CelesteTASIntegration.RegisterState(CustomFeather.CustomFeatherState, "Custom Feather");
        ModIntegration.CelesteTASIntegration.RegisterState(HeldRefill.HeldDashState, "Held Dash");
        ModIntegration.CelesteTASIntegration.RegisterState(WASDMovementState.ID, WASDMovementState.GetTasToolsDisplayName());
    }

    public static FieldInfo player_boostTarget = typeof(Player).GetField("boostTarget", BindingFlags.Instance | BindingFlags.NonPublic);
    public static FieldInfo player_calledDashEvents = typeof(Player).GetField("calledDashEvents", BindingFlags.Instance | BindingFlags.NonPublic);
    #region YellowBoost

    public static int YellowBoostState;

    private void YellowBoostBegin(Entity e) {
        Player player = e as Player;
        player.CurrentBooster = null;
        Level level = player.SceneAs<Level>();
        bool? flag;
        if (level == null) {
            flag = null;
        } else {
            MapMetaModeProperties meta = level.Session.MapData.GetMeta();
            flag = meta?.TheoInBubble;
        }
        bool? flag2 = flag;
        //GenericCustomBooster.GetBoosterThatIsBoostingPlayer(e).
        YellowBooster GetBoosterThatIsBoostingPlayer() {
            return new DynData<Player>(e as Player).Get<YellowBooster>("fh.customyellowBooster");
        }
        YellowBooster booster = GetBoosterThatIsBoostingPlayer();
        if (booster.DashRecovery == -1) {
            player.RefillDash();
        } else {
            player.Dashes = booster.DashRecovery;
        }

        player.RefillStamina();
        if (flag2.GetValueOrDefault()) {
            return;
        }
        player.Drop();
    }

    private int YellowBoostUpdate(Entity e) {
        Player player = e as Player;
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
        Player player = e as Player;
        Vector2 boostTarget = (Vector2) player_boostTarget.GetValue(player);
        Vector2 vector = (boostTarget - player.Collider.Center).Floor();
        player.MoveToX(vector.X, null);
        player.MoveToY(vector.Y, null);
    }

    private IEnumerator YellowBoostCoroutine(Entity e) {
        Player player = e as Player;
        YellowBooster booster = null;
        foreach (YellowBooster b in player.Scene.Tracker.GetEntities<YellowBooster>()) {
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

        On.Celeste.CassetteBlock.BoxSide.Render -= BoxSide_Render;
    }

    // Optional, initialize anything after Celeste has initialized itself properly.
    public override void Initialize() {
        foreach (EverestModule mod in Everest.Modules) {
            if (mod.Metadata.Name == "FrostHelperExtension") {
                throw new Exception("MOD CONFLICT: Please uninstall the FrostHelperExtension mod");
            }
        }
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
    /// Returns a list of colors from a comma-separated string of types
    /// </summary>
    public static Type[] GetTypes(string typeString) {
        string[] split = typeString.Trim().Split(',');
        Type[] parsed = new Type[split.Length];
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
        if (Engine.Scene is Level lvl) {
            return lvl;
        }

        if (Engine.Scene is AssetReloadHelper) {
            return AssetReloadHelper.ReturnToScene as Level;
        }

        return null;
    }
}
