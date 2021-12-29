using FrostHelper.Entities.Boosters;
using MonoMod.ModInterop;

#if PLAYERSTATEHELPER
using Celeste.Mod.PlayerStateHelper.API;
using FrostHelper.CustomStates;
#endif

namespace FrostHelper.API;

[ModExportName("FrostHelper")]
public static class API {
    public static int Version => 1;

    public static void SetCustomBoostState(Player player, GenericCustomBooster booster) {
        new DynData<Player>(player).Set("fh.customBooster", booster);
#if PLAYERSTATEHELPER
        player.SetState(StateIDs.CustomBoost, booster);
#else
        player.StateMachine.State = GenericCustomBooster.CustomBoostState;
#endif
    }

    public static bool IsInCustomBoostState(Player player) {
#if PLAYERSTATEHELPER
        return player.IsInState(StateIDs.CustomBoost);
#else
        return player.StateMachine.State == GenericCustomBooster.CustomBoostState;
#endif
    }

    /// <summary>
    /// Converts an entity name to a Type.
    /// </summary>
    public static Type EntityNameToType(string entityName) {
        return TypeHelper.EntityNameToType(entityName);
    }

    /// <summary>
    /// Returns an array of types from a comma-separated string of types.
    /// These types could either be c# type names, OR entity ID's.
    /// In case of an empty string, an empty array is returned.
    /// 
    /// Example input: jumpthru,FrostHelper/SpringLeft,FrostHelper.DirectionalPuffer
    /// </summary>
    public static Type[] GetTypes(string typeString) {
        return FrostModule.GetTypes(typeString);
    }

    /// <summary>
    /// Converts a string representation of a color into an XNA color struct.
    /// Possible formats:
    /// RRGGBBAA,
    /// RRGGBB,
    /// Xna Color Name (case insensitive)
    /// 
    /// </summary>
    public static Color GetColor(string colorString) {
        return ColorHelper.GetColor(colorString);
    }

    /// <summary>
    /// Returns the color a rainbow spinner would have at the given position.
    /// Supports Max's Helping Hand rainbow spinner controllers.
    /// </summary>
    public static Color GetRainbowColor(Vector2 position) {
        return GetRainbowColor(position);
    }

    /// <summary>
    /// Returns the color a rainbow spinner would have at the given position in a given scene.
    /// Supports Max's Helping Hand rainbow spinner controllers.
    /// </summary>
    public static Color GetRainbowColor(Scene scene, Vector2 position) {
        return ColorHelper.GetHue(scene, position);
    }

    /// <summary>
    /// Gets the attach group of a given Entity
    /// </summary>
    public static int GetAttachGroup(Entity entity) {
        return entity switch {
            CustomSpinner sp => sp.AttachGroup,
            _ => entity.Get<GroupedStaticMover>()?.Group ?? -1,
        };
    }

    /// <summary>
    /// Converts a StaticMover into a GroupedStaticMover, returning it
    /// </summary>
    public static Component ToGroupedStaticMover(StaticMover staticMover, int attachGroup) {
        return new GroupedStaticMover(attachGroup) {
            JumpThruChecker = staticMover.JumpThruChecker,
            OnDestroy = staticMover.OnDestroy,
            OnDisable = staticMover.OnDisable,
            OnEnable = staticMover.OnEnable,
            Active = staticMover.Active,
            OnMove = staticMover.OnMove,
            OnShake = staticMover.OnShake,
        }.SetOnAttach(staticMover.OnAttach);
    }

    /// <summary>
    /// Destroys a CustomSpinner or vanilla spinner
    /// </summary>
    public static void DestroySpinner(Entity spinner, bool boss) {
        switch (spinner) {
            case CustomSpinner sp:
                sp.Destroy(boss); 
                break;
            case CrystalStaticSpinner sp:
                sp.Destroy(boss);
                break;
        };
    }
}
