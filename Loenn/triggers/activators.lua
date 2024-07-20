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

local function makeActivator(name, placement, extTextCallback, extra)
    local h = {
        name = name,
        nodeLimits = {1, -1},
        nodeLineRenderType = "fan",
        placements = {
            placement
        },
        _lonnExt_extendedText = extTextCallback,
        fieldInformation = {
            activationMode = {
                options = activationModes,
                editable = false,
            }
        }
    }

    extra = extra or {}

    if extra.disableOnce ~= true then
        h.placements[1].data.once = false
    end

    if extra.fieldInformation then
        for key, value in pairs(extra.fieldInformation) do
            h.fieldInformation[key] = value
        end
    end

    h.placements[1].data.delay = 0.0
    h.placements[1].data.activationMode = "All"

    return h
end

return {
    makeActivator("FrostHelper/OnPlayerEnterActivator", {
        name = "default",
        data = {
        }
    }),
    makeActivator("FrostHelper/OnPlayerOnGroundActivator", {
        name = "default",
        data = {
            onlyWhenJustLanded = true,
        }
    }),
    makeActivator("FrostHelper/OnPlayerDashingActivator", {
        name = "default",
        data = {
            onlyWhenJustDashed = true,
            hasToBeInside = false,
        }
    }),
    makeActivator("FrostHelper/OnSpawnActivator", {
        name = "default",
        data = {
        }
    }),
    makeActivator("FrostHelper/OnFlagActivator", {
        name = "default",
        data = {
            flag = "",
            targetState = true,
            mustChange = false,
            triggerOnRoomBegin = true,
            activateAfterDeath = false,
        }
    }, function (trigger)
        if trigger.targetState then
            return trigger.flag
        else
            return "!" .. trigger.flag
        end
    end),
    makeActivator("FrostHelper/IfActivator", {
        name = "default",
        data = {
            condition = "",
        }
    },function (trigger)
        return trigger.condition
    end),
    makeActivator("FrostHelper/DelayActivator", {
        name = "default",
        data = {
        }
    },function (trigger)
        return string.format("%.3f", trigger.delay)
    end),
    makeActivator("FrostHelper/LoopActivator",
        {
            name = "default",
            data = {
                requireActivation = false,
                loopTime = 1.0,
                activateAfterDeath = false,
            },
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
            name = "default",
            data = {
                cache = true,
                types = "",
                activateAfterDeath = false,
            },
        },
        nil,
        {
            fieldInformation = {
                types = jautils.typesListFieldInfo()
            }
        }
    ),
    makeActivator("FrostHelper/OnDeathActivator",
    {
        name = "default",
        data = {
        }
    }),
    makeActivator("FrostHelper/OnCassetteSwapActivator",
        {
            name = "default",
            data = {
                targetIndex = -1,
                activateAfterDeath = false,
            }
        },
        function (trigger)
            return cassetteSwapActivatorOptionsInv[trigger.targetIndex] or tostring(trigger.targetIndex)
        end,
        {
            fieldInformation = {
                targetIndex = {
                    fieldType = "integer",
                    options = cassetteSwapActivatorOptions,
                    editable = false
                }
            }
        }
    ),
}