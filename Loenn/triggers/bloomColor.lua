local enums = require("consts.celeste_enums")
local jautils = require("mods").requireFromPlugin("libraries.jautils")

return {
    {
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
        }
    },
    {
        name = "FrostHelper/RainbowBloomTrigger",
        placements = {
            name = "default",
            data = {
                enable = true,
            }
        },
    },
    {
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
    },
    {
        name = "FrostHelper/BloomColorPulseTrigger",
        placements = {
            name = "default",
            data = {
                bloomAddFrom = "ffffff",
                bloomAddTo = "ff00ff",
                duration = 0.4,
                easing = "Linear",
                tweenMode = "Oneshot",
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
            easing = {
                options = jautils.easings,
                editable = false
            },
            tweenMode = {
                options = jautils.tweenModes,
                editable = false,
            }
        }
    }
}