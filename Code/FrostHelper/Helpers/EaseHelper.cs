using Celeste.Mod.Helpers;
using FrostHelper.Helpers;
using FrostHelper.SessionExpressions;

namespace FrostHelper;

public static class EaseHelper {
    private static void PrintStack(KeraLua.Lua state) {
        //Console.WriteLine("Stack:");
        //for (int i = 1; i <= state.GetTop(); i++) {
        //    Console.WriteLine($"[{i}]: {state.ToString(i)}");
        //}
    }

    /// <returns>An easer of the name specified by <paramref name="name"/>, defaulting to <paramref name="defaultValue"/> or <see cref="Ease.Linear"/> if <paramref name="defaultValue"/> is null </returns>
    // exposed via the api
    public static Ease.Easer GetEase(string name, Ease.Easer? defaultValue = null) {
        if (!Easers.TryGetValue(name, out var ease)) {
            if (name.StartsWith("expr:", StringComparison.Ordinal)) {
                ease = TryLoadSessionExpressionEaser(name["expr:".Length..]);
            } else {
                ease = TryLoadLuaEaser(name, ease);
            }

            Easers[name] = ease;
        }

        return ease ?? defaultValue ?? Ease.Linear;
    }

    
    private static readonly ExpressionContext ExprCtx = new(
        simpleCommands: new() {
            ["p"] = SimpleCommands.CreateCommandFromModFunc(static (s, userdata) => userdata ?? 0f)
        }, 
        functions: new()
    );
    
    private static Ease.Easer? TryLoadSessionExpressionEaser(string name) {
        if (!ConditionHelper.TryCreate(name, ExprCtx, out var cond)) {
            return null;
        }

        return p => {
            var session = FrostModule.TryGetCurrentLevel()?.Session;
            if (session is null)
                return 0f;

            return cond.GetFloat(session, p);
        };
    }

    private static Ease.Easer? TryLoadLuaEaser(string name, Ease.Easer? ease) {
        var code = $"return function(p){(name.Contains("return") ? "" : " return")} {name} end";
        try {
            // repeated calls to LuaFunction.Call eventually crash with a stack overflow....
            //LuaFunction f = (Everest.LuaLoader.Run(code, code)[0] as LuaFunction)!;
            // Time to do this ourselves, though this prevents access to c# stuff :(
            KeraLua.Lua lua = new();
            if (!lua.DoString(code)) {
                // no error - lua.DoString's return value is inverted compared to what you might think
                ease = (p) => {
                    lua.PushCopy(1);
                    lua.PushNumber(p);
                    lua.Call(1, 1);
                    var ret = lua.ToNumber(2);
                    lua.Pop(1);

                    return (float) ret;
                };
            }
        } catch (Exception e) {
            e.LogDetailed();
        }

        if (ease is null)
            NotificationHelper.Notify($"Failed to load lua easer '{name}' (generated code: {code}):");

        return ease;
    }

    /// <summary>Calls <see cref="GetEase(string, Ease.Easer)"/> on the <paramref name="data"/>'s attribute <paramref name="key"/>, using <paramref name="defaultValue"/> if it's not a valid easing type/is empty</summary>
    public static Ease.Easer Easing(this EntityData data, string key, Ease.Easer defaultValue) {
        return GetEase(data.Attr(key), defaultValue);
    }

    public static Tween.TweenMode TweenMode(this EntityData data, string key, Tween.TweenMode defaultValue)
        => GetTweenMode(data.Attr(key), defaultValue);

    // exposed via the api
    internal static Tween.TweenMode GetTweenMode(string mode, Tween.TweenMode defaultValue) {
        return mode switch {
            nameof(Tween.TweenMode.Persist) => Tween.TweenMode.Persist,
            nameof(Tween.TweenMode.Oneshot) => Tween.TweenMode.Oneshot,
            nameof(Tween.TweenMode.Looping) => Tween.TweenMode.Looping,
            nameof(Tween.TweenMode.YoyoOneshot) => Tween.TweenMode.YoyoOneshot,
            nameof(Tween.TweenMode.YoyoLooping) => Tween.TweenMode.YoyoLooping,
            _ => defaultValue
        };
    }

    private static readonly Dictionary<string, Ease.Easer?> Easers =
        typeof(Ease).GetFields(BindingFlags.Static | BindingFlags.Public)
        .Where(f => f.FieldType == typeof(Ease.Easer))
        .ToDictionary(f => f.Name, f => (Ease.Easer?)f.GetValue(null), StringComparer.OrdinalIgnoreCase);
}
