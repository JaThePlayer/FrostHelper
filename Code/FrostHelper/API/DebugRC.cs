using static Celeste.Mod.Everest;

namespace FrostHelper.API;

public static class DebugRCExt {
    private static readonly List<RCEndPoint> EndPoints = new() {
        new RCEndPoint {
            Path = "/frostHelper/csharpTypesToEntityID",
            Name = "CSharp Types To Entity IDs",
            InfoHTML = "Translates a list of CSharp type names OR entity IDs to a list of Entity IDs",
            Handle = c => {
                var types = c.Request.Headers["types"].Split(',');
                DebugRC.Write(c, string.Join(",", API.EntityNamesFromTypeNames(types)));
            }
        },
    };

    [OnLoad]
    public static void Load() {
        DebugRC.EndPoints.AddRange(EndPoints);

        Events.Celeste.OnShutdown += Unload;
    }

    [OnUnload]
    public static void Unload() {
        DebugRC.EndPoints.RemoveAll(endPoint => EndPoints.Contains(endPoint));
        Events.Celeste.OnShutdown -= Unload;
    }
}
