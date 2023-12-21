local csharpVector2 = require("#microsoft.xna.framework.vector2")

local celeste = require("#celeste")
local celesteMod = celeste.mod
local engine = require("#monocle.engine")
local fhLuaHelper = require("#FrostHelper.Helpers.LuaHelper")
local fhNotifHelper = require("#FrostHelper.Helpers.NotificationHelper")
local fhLuaBossType = require("#FrostHelper.Entities.LuaBadelineBoss")

local helpers = {}

function helpers.getBoss()
    local boss = fhLuaBossType.GetById(selfId)
    self = boss
    return boss
end

--- Pause code exection for duration seconds, or waits for a C# IEnumerator to finish.
function helpers.wait(duration)
    return coroutine.yield(duration)
end
--- Put debug message in the Celeste console, for debugging.
function helpers.log(message, tag)
    celesteMod.logger.log(celesteMod.LogLevel.Info, tag or "FrostHelper.CustomBoss", tostring(message))
end

-- Shows an in-game message that also get logged to the console, for debugging.
function helpers.notify(message)
    fhNotifHelper.Notify(message)
end

function helpers.shoot(...)
    local allShots = {...}

    for _, args in ipairs(allShots) do
        helpers.getBoss():Shoot(args)
    end
end

function helpers.shootAt(location, ...)
    local allShots = {...}

    for _, args in ipairs(allShots) do
        helpers.getBoss():ShootAt(location, args)
    end
end

function helpers.beam(...)
    local allBeams = {...}

    if #allBeams == 1 then
        helpers.wait(helpers.getBoss():Beam(allBeams[1]))
        return
    end

    for i, beamArgs in ipairs(allBeams) do
        if i == #allBeams then
            helpers.wait(helpers.getBoss():Beam(beamArgs))
        else
            helpers.startCoroutine(function ()
                helpers.wait(helpers.getBoss():Beam(beamArgs))
            end)
        end
    end
end

function helpers.beginCharge()
    helpers.getBoss():StartShootCharge()
end

function helpers.shatterSpinners(args)
    args = args or {}

    if args.types == nil then
        -- since the builtin spinner list has modded spinners which may or may not exist, we need to silence any notifications for missing types.
        -- if you provide your own type list though, the notifications are useful
        args.silenceTypeNotFoundNotifications = true
    end

    args.types = args.types or {
        "spinner", "FrostHelper/IceSpinner",
        "VivHelper/CustomSpinner",
        "BrokemiaHelper.CassetteSpinner", -- Brokemia Helper does not use the CustomEntity attribute, we have to use the C# type instead :/
        "ChronoHelper/ShatterSpinner",
        "ArphimigonHelper/ElementalCrystalSpinner"
    }

    if type(args.types) == "string" then
        args.types = { args.types }
    end

    helpers.getBoss():ShatterSpinners(args)
end

function helpers.startCoroutine(func)
    helpers.getBoss():StartCoroutine(func)
end

function helpers.vector2(x, y)
    local typ = type(x)

    if typ == "table" and not y then
        return csharpVector2(x[1], x[2])

    elseif typ == "userdata" and not y then
        return x

    else
        return csharpVector2(x, y)
    end
end

--- Set session flag.
-- @string flag Flag to set.
-- @bool value State of flag.
function helpers.setFlag(flag, value)
    engine.Scene.Session:SetFlag(flag, value)
end

--- Get session flag.
-- @string flag Flag to get.
-- @treturn bool The state of the flag.
function helpers.getFlag(flag)
    return engine.Scene.Session:GetFlag(flag)
end

--- Loads and returns the result of a Lua asset.
-- @string filename Filename to load. Filename should not have a extention.
function helpers.loadCelesteAsset(filename)
    local content = fhLuaHelper.ReadModAsset(filename)

    if not content then
        error(string.format("Failed to find celeste asset %s", filename))
    end

    local env = {}
    local mt = {
        __index = function (self, key)
            if helpers[key] then
                return helpers[key]
            end

            return _ENV[key]
        end
    }

    setmetatable(env, mt)

    local func, errorMsg = load(content, filename, nil, env)
    if errorMsg then
        error(string.format("Syntax error in celeste asset %s: %s", filename, errorMsg))
    end

    local success, result = pcall(func)

    if success then
        return result

    else
        error("Failed to require asset in Lua: " .. result)
    end
end

return helpers