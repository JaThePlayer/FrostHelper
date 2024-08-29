local jautils = require("mods").requireFromPlugin("libraries.jautils")

local activationModes = {
    "All", -- all triggers get activated at once
    "Cycle", -- each time this activator gets triggered, the next trigger gets activated, wrapping over to the first one once all other ones have been triggered.
    "Random"
}

local cassetteSwapActivatorOptions = {
    ["-1: Any"] = -1,
    ["0: Blue"] = 0,
    ["1: Rose"] = 1,
    ["2: Bright Sun"] = 2,
    ["3: Malachite"] = 3,
}

local cassetteSwapActivatorOptionsInv = {
    [-1] = "Any",
    [0] = "Blue",
    [1] = "Rose",
    [2] = "Bright Sun",
    [3] = "Malachite",
}

local counterOperations = {
    "Equal",
    "NotEqual",
    "GreaterThan",
    "LessThan",
}

local counterOperationToMathExpr = {
    ["Equal"] = "==",
    ["NotEqual"] = "!=",
    ["GreaterThan"] = ">",
    ["LessThan"] = "<",
}

local function makeActivator(name, placement, extTextCallback, extra)
    local h = {
        name = name,
        nodeLimits = {1, -1},
        nodeLineRenderType = "fan",
    }

    extra = extra or {}

    table.insert(placement, 1, { "delay", "0" })
    table.insert(placement, 1, { "activationMode", "All", activationModes })
    if extra.disableOnce ~= true then
        table.insert(placement, { "once", false })
    end

    jautils.createPlacementsPreserveOrder(h, "default", placement, true)
    jautils.addExtendedText(h, extTextCallback)

    return h
end

local counterSwitchActivator = {
    name = "FrostHelper/SwitchOnCounterActivator",
    nodeLimits = {1, -1},
    nodeLineRenderType = "line",
}

jautils.createPlacementsPreserveOrder(counterSwitchActivator, "default", {
    { "delay", "0" },
    { "counter", "", "sessionCounter" },
    { "cases", "", "list" },
    { "once", false },
}, true)
jautils.addExtendedText(counterSwitchActivator, function (trigger) return trigger.counter or "" end)

return {
    makeActivator("FrostHelper/OnPlayerEnterActivator", {
    }),
    makeActivator("FrostHelper/OnPlayerOnGroundActivator", {
        { "onlyWhenJustLanded", true }
    }),
    makeActivator("FrostHelper/OnPlayerDashingActivator", {
        { "onlyWhenJustDashed", true },
        { "hasToBeInside", false },
    }),
    makeActivator("FrostHelper/OnSpawnActivator", {
    }),
    makeActivator("FrostHelper/OnFlagActivator", {
        { "flag", "" },
        { "targetState", true },
        { "mustChange", false },
        { "triggerOnRoomBegin", true },
        { "activateAfterDeath", false },
    }, function (trigger)
        if trigger.targetState then
            return trigger.flag
        else
            return "!" .. trigger.flag
        end
    end),
    makeActivator("FrostHelper/IfActivator", {
        { "condition", "" },
    },function (trigger)
        return trigger.condition
    end),
    makeActivator("FrostHelper/DelayActivator", {
    },function (trigger)
        return string.format("%.3f", trigger.delay)
    end),
    makeActivator("FrostHelper/LoopActivator",
        {
            { "loopTime", 1.0 },
            { "activateAfterDeath", false },
            { "requireActivation", false },
        },
        function (trigger)
            return string.format("%.3f", trigger.loopTime or 0)
        end,
        {
            disableOnce = true,
        }
    ),
    makeActivator("FrostHelper/OnEntityEnterActivator",
        {
            { "types", "", "typesList" },
            { "cache", true },
            { "activateAfterDeath", false },
        }
    ),
    makeActivator("FrostHelper/OnDeathActivator",
    {
    }),
    makeActivator("FrostHelper/OnCassetteSwapActivator",
        {
            { "targetIndex", -1, "dropdown_int", cassetteSwapActivatorOptions },
            { "activateAfterDeath", false },
        },
        function (trigger)
            return cassetteSwapActivatorOptionsInv[trigger.targetIndex] or tostring(trigger.targetIndex)
        end
    ),
    makeActivator("FrostHelper/IfCounterActivator",
        {
            { "counter", "", "sessionCounter" },
            { "target", "0", "sessionCounter" },
            { "operation", "Equal", counterOperations },
        },
        function (trigger)
            return string.format("%s %s %s", trigger.counter, counterOperationToMathExpr[trigger.operation], trigger.target)
        end
    ),
    makeActivator("FrostHelper/OnCounterActivator",
        {
            { "counter", "", "sessionCounter" },
            { "target", "0", "sessionCounter" },
            { "operation", "Equal", counterOperations },
        },
        function (trigger)
            return string.format("%s %s %s", trigger.counter, counterOperationToMathExpr[trigger.operation], trigger.target)
        end
    ),
    --counterSwitchActivator,
}