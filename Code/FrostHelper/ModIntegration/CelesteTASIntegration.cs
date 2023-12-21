// ReSharper disable InconsistentNaming
namespace FrostHelper.ModIntegration;

// Heavily based on Communal Helper
public static class CelesteTASIntegration {
    [OnLoadContent]
    public static void Load() {
        EverestModuleMetadata celesteTASMeta = new EverestModuleMetadata { Name = "CelesteTAS", VersionString = "3.4.5" };
        if (IntegrationUtils.TryGetModule(celesteTASMeta, out var celesteTASModule)) {
            var playerStatesType = celesteTASModule.GetType().Module.GetType("TAS.PlayerStates");
            if (playerStatesType is null)
                return;
            
            CelesteTAS_PlayerStates_Register = playerStatesType.GetMethod("Register", BindingFlags.Public | BindingFlags.Static);
            CelesteTAS_PlayerStates_Unregister = playerStatesType.GetMethod("Unregister", BindingFlags.Public | BindingFlags.Static);
            
            CelesteTASLoaded = true;
        }
    }

    private static bool CelesteTASLoaded;
    private static MethodInfo? CelesteTAS_PlayerStates_Register;
    private static MethodInfo? CelesteTAS_PlayerStates_Unregister;

    public static void RegisterState(int state, string stateName) {
        if (CelesteTASLoaded)
            CelesteTAS_PlayerStates_Register?.Invoke(null, new object[] { state, stateName });
    }

    public static void UnregisterState(int state) {
        if (CelesteTASLoaded)
            CelesteTAS_PlayerStates_Unregister?.Invoke(null, new object[] { state });
    }
}
