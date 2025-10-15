using System.Diagnostics.CodeAnalysis;

namespace FrostHelper.ModIntegration;

public static class IntegrationUtils {
    // From Communal Helper
    // Modified version of Everest.Loader.DependencyLoaded
    public static bool TryGetModule(EverestModuleMetadata meta, [NotNullWhen(true)] out EverestModule? module) {
        foreach (EverestModule other in Everest.Modules) {
            EverestModuleMetadata otherData = other.Metadata;
            if (otherData.Name != meta.Name)
                continue;

            Version version = otherData.Version;
            if (Everest.Loader.VersionSatisfiesDependency(meta.Version, version)) {
                module = other;
                return true;
            }
        }

        module = null;
        return false;
    }

    internal static bool TryGetModule(string modName, [NotNullWhen(true)] out EverestModule? module) {
        foreach (EverestModule other in Everest.Modules) {
            EverestModuleMetadata otherData = other.Metadata;
            if (otherData.Name != modName)
                continue;

            module = other;
            return true;
        }

        module = null;
        return false;
    }
}
