using NLua;
using System.IO;

namespace FrostHelper.Helpers;

public static class LuaHelper {
    internal const string LoggingTag = "FrostHelper.LuaHelper";

    private static Lazy<LuaTable> LuaApi = new(() => (Everest.LuaLoader.Run(LuaApiCode, "FrostHelper.LuaHelper")[0] as LuaTable)!);

    // Used in LuaBoss/env
    public static string? ReadModAsset(string filename) {
        if (Everest.Content.TryGet(filename, out var asset)) {
            return ReadModAsset(asset);
        }

        return null;
    }

    public static string ReadModAsset(ModAsset asset) {
        using var reader = new StreamReader(asset.Stream);

        return reader.ReadToEnd();
    }

    public static object[] RunLua(string filename, LuaTable? env, LuaTable? args = null, string helperFunc = "getLuaData") {
        var content = Everest.Content.TryGet(filename, out var asset) ? ReadModAsset(asset) : null;
        if (content is null) {
            NotificationHelper.Notify($"Couldn't find asset {filename}\nTry restarting the game.");

            return new object[0];
        }

        try {
            if (args is { }) {
                return (LuaApi.Value[helperFunc] as LuaFunction)!.Call(content, filename, env!, args);
            } else {
                return (LuaApi.Value[helperFunc] as LuaFunction)!.Call(content, filename, env!);
            }
        } catch (Exception ex) {
            Logger.Log(LogLevel.Error, LoggingTag, ex.ToString());

            return new object[0];
        }
    }

    public static IEnumerator LuaFuncToIEnumerator(this LuaFunction f) {
        var luaRoutine = (LuaCoroutine) ((LuaApi.Value["funcToLuaCoroutine"] as LuaFunction)!.Call(f)[0]);

        return LuaCoroutineToIEnumerator(luaRoutine);
    }

    public static LuaTable DictionaryToLuaTable(Dictionary<object, object> dict) {
        LuaTable luaTable = (Everest.LuaLoader.Context.DoString("return {}", "chunk").FirstOrDefault() as LuaTable)!;

        foreach (var keyValuePair in dict) {
            luaTable[keyValuePair.Key] = keyValuePair.Value;
        }
        return luaTable;
    }

    private static bool SafeMoveNext(this LuaCoroutine enumerator) {
        bool result;
        try {
            result = enumerator.MoveNext();
        } catch (Exception e) {
            NotificationHelper.Notify("Failed to resume lua coroutine. Check log.txt.");
            Logger.LogDetailed(e, null);
            result = false;
        }

        return result;
    }

    public static float GetOrDefault(this LuaTable? table, object key, float def) {
        if (table is null)
            return def;

        if (table[key] is { } obj) {
            return obj switch {
                double d => (float)d,
                long l => l,
                _ => def,
            };
        }

        return def;
    }

    public static float? GetFloatOrNull(this LuaTable? table, object key) {
        if (table is null)
            return null;

        if (table[key] is { } obj) {
            return obj switch {
                double d => (float) d,
                long l => l,
                _ => null,
            };
        }

        return null;
    }
    
    public static int? GetIntOrNull(this LuaTable? table, object key) {
        if (table is null)
            return null;

        if (table[key] is { } obj) {
            return obj switch {
                double d => (int) d,
                long l => (int)l,
                _ => null,
            };
        }

        return null;
    }

    public static T GetOrDefault<T>(this LuaTable? table, object key, T def) {
        if (table is null)
            return def;

        if (table[key] is { } obj) {
            return obj switch {
                T t => t,
                _ => def,
            };
        }
        return def;
    }

    public static IEnumerator LuaCoroutineToIEnumerator(LuaCoroutine routine) {
        while (routine != null && routine.SafeMoveNext()) {
            if (routine.Current is double or long) {
                yield return Convert.ToSingle(routine.Current);
            } else {
                yield return routine.Current;
            }
        }
    }

    private const string LuaApiCode = """
        local h = {}

        local celesteMod = require("#celeste.mod")
        local lrexception = require("#FrostHelper.Helpers.LuaRuntimeException")

        local function threadProxyResume(self, ...)
            if coroutine.status(self.value) == "dead" then
                return false, nil
            end

            local success, ret = coroutine.resume(self.value, ...)

            -- if something crashed, success is false and ret is a string with the error message, but LuaCoroutine looks for ret being an Exception...
            if not success then
                return success, lrexception(ret)
            end

            return success, ret
        end

        function h.funcToLuaCoroutine(f)
            if not f then
                return false
            end

            return celesteMod.LuaCoroutine({value = coroutine.create(f), resume = threadProxyResume})
        end

        local function prepareBoss(env, func)
            local success, ai = pcall(func)

            if success then
                ai = ai or env.ai
                if not ai then
                    celesteMod.logger.log(celesteMod.logLevel.error, "FrostHelper.LuaHelper", "Lua Boss didn't return an ai function!")
                end

                local onEnd = env.onEnd
                local onHit = env.onHit

                return ai, onEnd, onHit

            else
                celesteMod.logger.log(celesteMod.logLevel.error, "FrostHelper.LuaHelper", "Failed to load boss in Lua: " .. tostring(ai))

                return success
            end
        end

        function h.getLuaEnv(data, args)
            local env = data or {}
            args = args or {}

            local mt = {
                __index = function (self, key)
                    if args[key] then
                        return args[key]
                    end

                    return _G[key]
                end
            }

            setmetatable(env, mt)

            return env
        end

        function h.getLuaData(content, filename, data, args, preparationFunc)
            preparationFunc = preparationFunc or function(env, func) 
                local success, ret = pcall(func)
                if success then
                    return ret
                end

                celesteMod.logger.log(celesteMod.logLevel.error, "FrostHelper.LuaHelper", "Failed to load lua: " .. ret)
                return false
            end

            local env = h.getLuaEnv(data, args)

            if content then
                local func = load(content, filename, nil, env)

                return preparationFunc(env, func)
            end
        end

        function h.getBossData(content, filename, data, args)
            return h.getLuaData(content, filename, data, args, prepareBoss)
        end

        return h
        """;
}

public class LuaRuntimeException : Exception {
    public LuaRuntimeException(string msg) : base(msg) {

    }

    public LuaRuntimeException() {

    }
}