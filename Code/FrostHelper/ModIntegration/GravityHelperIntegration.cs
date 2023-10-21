using MonoMod.ModInterop;

namespace FrostHelper.ModIntegration;

[ModImportName("GravityHelper")]
public static class GravityHelperIntegration {
    public static void Load() {
        typeof(GravityHelperIntegration).ModInterop();

        if (IsLoaded) {
            RegisterModSupportBlacklist!("FrostHelper");

            GravitySpring_InvertedSuperBounce = new(() =>
                TypeHelper.EntityNameToTypeSafe("GravityHelper/GravitySpringFloor")
                ?.GetMethod("InvertedSuperBounce")
                ?.CreateDelegate<Action<Player, float>>()
                ?? throw new Exception("GravityHelper is loaded, but couldn't find GravitySpring.InvertedSuperBounce!"));
        }
    }

    public static bool IsLoaded => RegisterModSupportBlacklist is { };

    public static Action<string>? RegisterModSupportBlacklist;

    public static Func<bool>? IsPlayerInverted;

    public static Action? BeginOverride;

    public static Action? EndOverride;

    //NON-API
    public static float InvertIfPlayerInverted(float f) {
        return IsPlayerInverted?.Invoke() ?? false ? -f : f;
    }

    //NON-API
    public static Vector2 InvertIfPlayerInverted(Vector2 v) {
        return IsPlayerInverted?.Invoke() ?? false ? new(v.X, -v.Y) : v;
    }

    //NON-API
    private static Lazy<Action<Player, float>> GravitySpring_InvertedSuperBounce;

    //NON-API
    public static void InvertedSuperBounce(Player player, float fromY) {
        if (IsLoaded) {
            GravitySpring_InvertedSuperBounce.Value(player, fromY);
        }
    }

    //NON-API
    public static void SuperBounce(Player player, float fromY) {
        if (IsLoaded && IsPlayerInverted!()) {
            InvertedSuperBounce(player, fromY);
        } else {
            player.SuperBounce(fromY);
        }
    }
}
