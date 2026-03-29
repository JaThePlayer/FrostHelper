using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace FrostHelper.Materials;

[Tracked]
internal sealed class MaterialManager : Entity {
    private readonly Dictionary<string, Lazy<IMaterial>> _materials = [];

    public MaterialManager() {
        Tag |= Tags.Persistent;
    }
    
    public static MaterialManager GetFor(Scene scene) {
        return ControllerHelper<MaterialManager>.AddToSceneIfNeeded(scene);
    }
    
    public bool TryGet(string name, [NotNullWhen(true)] out IMaterial? material) {
        if (_materials.TryGetValue(name, out var materialFactory)) {
            material = materialFactory.Value;
            return true;
        }

        material = null;
        return false;
    }

    public void Register(string name, Func<IMaterial> material) {
        _materials[name] = new Lazy<IMaterial>(material, LazyThreadSafetyMode.ExecutionAndPublication);
    }
}