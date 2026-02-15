// ReSharper disable InconsistentNaming
namespace FrostHelper.ModIntegration;

// Heavily based on Communal Helper
internal static class CelesteTASIntegration {
    [OnLoadContent]
    public static void Load() {
        EverestModuleMetadata celesteTASMeta = new EverestModuleMetadata { Name = "CelesteTAS", VersionString = "3.47.0" };
        if (IntegrationUtils.TryGetModule(celesteTASMeta, out var celesteTASModule)) {
            var managerType = celesteTASModule.GetType().Module.GetType("TAS.Manager");
            if (managerType is null)
                return;

            if (managerType.GetMethod("EnableRun", BindingFlags.Public | BindingFlags.Static) is not { } enableRun)
                return;
            Manager_EnableRun = enableRun;
            //     public static readonly InputController Controller
            if (managerType.GetField("Controller", BindingFlags.Public | BindingFlags.Static) is not { } controller)
                return;
            Manager_Controller = controller;
            
            var inputControllerType = celesteTASModule.GetType().Module.GetType("TAS.Input.InputController");
            if (inputControllerType is null)
                return;
            
            
            CelesteTASLoaded = true;
        }
    }

    private static bool CelesteTASLoaded;

    private static MethodInfo? Manager_EnableRun;
    private static FieldInfo? Manager_Controller;

    public static void LoadTas(string path) {
        // Based on https://github.com/EverestAPI/CelesteTAS-EverestInterop/blob/master/CelesteTAS-EverestInterop/Source/Tools/PlayTasAtLaunch.cs
        if (!CelesteTASLoaded)
            return;
        if (Manager_Controller!.GetValue(null) is not { } controller)
            return;

        var controllerData = DynamicData.For(controller);
        controllerData.Set("FilePath", path);
        Manager_EnableRun!.Invoke(null, null);
    }
}
