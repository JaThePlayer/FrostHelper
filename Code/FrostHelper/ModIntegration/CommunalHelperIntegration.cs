using MonoMod.ModInterop;

namespace FrostHelper.ModIntegration;

// ReSharper disable InconsistentNaming 
// ReSharper disable UnassignedField.Global
#pragma warning disable CS0649 // Field is never assigned to
#pragma warning disable CA2211

[ModImportName("CommunalHelper.DashStates")]
internal static class CommunalHelperIntegration {
    public static bool LoadIfNeeded()
    {
        if (Loaded)
            return true;

        typeof(CommunalHelperIntegration).ModInterop();

        Loaded = true;

        return GetDreamTunnelDashState is {};
    }

    private static bool Loaded { get; set; }

    public static bool Available => LoadIfNeeded() && GetDreamTunnelDashState is { };

    // int GetDreamTunnelDashState()
    public static Func<int>? GetDreamTunnelDashState;
}