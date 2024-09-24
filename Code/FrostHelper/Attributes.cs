namespace FrostHelper;

/// <summary>
/// Method gets called when FrostModule.Load() is called
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public class OnLoad : Attribute { }

/// <summary>
/// Method gets called when FrostModule.Load() is called,
/// only if the user wants to preload hooks
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public class HookPreload : Attribute { }

/// <summary>
/// Method gets called when FrostModule.LoadContent() is called
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public class OnLoadContent : Attribute { }

/// <summary>
/// Method gets called when FrostModule.Unload() is called
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public class OnUnload : Attribute { }

public static class AttributeHelper {
    private static Dictionary<Type, List<MethodInfo>>? _cached;
    
    public static void InvokeAllWithAttribute(Type attributeType) {
        if (_cached is not { }) {
            _cached = new() {
                [typeof(OnLoad)] = [],
                [typeof(OnUnload)] = [],
                [typeof(HookPreload)] = [],
                [typeof(OnLoadContent)] = [],
            };
            
            foreach (var type in typeof(FrostModule).Assembly.GetTypesSafe()) {
                foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)) {
                    foreach (var attr in method.CustomAttributes) {
                        if (_cached.TryGetValue(attr.AttributeType, out var cacheEntry)) {
                            cacheEntry.Add(method);
                        }
                    }
                }
            }
        }

        foreach (var method in _cached[attributeType]) {
            method.Invoke(null, null);
        }
    }
}
