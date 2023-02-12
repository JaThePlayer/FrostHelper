local activationModes = {
    "All", -- all triggers get activated at once
    "Cycle", -- each time this activator gets triggered, the next trigger gets activated, wrapping over to the first one once all other ones have been triggered.
    "Random"
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
                loopTime = 1.0
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
            },
        }
    )
}