local enums = require("consts.celeste_enums")
local jautils = require("mods").requireFromPlugin("libraries.jautils")

local bloomColor = {
    name = "FrostHelper/BloomColorTrigger",
    placements = {
        name = "default",
        data = {
            color = "ffffff"
        }
    },
    fieldInformation = {
        color = {
            fieldType = "color",
            allowXNAColors = true,
        }
    },
}

jautils.addExtendedText(bloomColor, function (trigger)
    return trigger.color
end)

local rainbowBloom = {
    name = "FrostHelper/RainbowBloomTrigger",
    placements = {
        name = "default",
        data = {
            enable = true,
        }
    },
}

local bloomColorFade = {
    name = "FrostHelper/BloomColorFadeTrigger",
    placements = {
        name = "default",
        data = {
            bloomAddFrom = "ffffff",
            bloomAddTo = "ff00ff",
            positionMode = "NoEffect",
        }
    },
    fieldInformation = {
        bloomAddFrom = {
            fieldType = "color",
            allowXNAColors = true,
        },
        bloomAddTo = {
            fieldType = "color",
            allowXNAColors = true,
        },
        positionMode = {
            options = enums.trigger_position_modes,
            editable = false
        }
    }
}

jautils.addExtendedText(bloomColorFade, function (trigger)
    return string.format("%s -> %s", trigger.bloomAddFrom, trigger.bloomAddTo)
end)

local bloomColorPulse = {
    name = "FrostHelper/BloomColorPulseTrigger",
}

jautils.createPlacementsPreserveOrder(bloomColorPulse, "default", {
    { "width", 16 }, { "height", 16 },
    { "bloomAddFrom", "ffffff", "color" },
    { "bloomAddTo", "ff00ff", "color" },
    { "tweenMode", "YoyoOneshot", jautils.tweenModes },
    { "easing", "Linear", jautils.easings },
    { "duration", 0.4 },
})

jautils.addExtendedText(bloomColorPulse, function (trigger)
    return string.format("%s -> %s", trigger.bloomAddFrom, trigger.bloomAddTo)
end)

return {
    bloomColor,
    rainbowBloom,
    bloomColorFade,
    bloomColorPulse
}