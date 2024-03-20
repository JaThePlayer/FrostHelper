local jautils = require("mods").requireFromPlugin("libraries.jautils")

local flagInvert = {
    name = "FrostHelper/FlagInvertTrigger",
    placements = {
        name = "default",
        data = {
            flag = "",
        }
    },
}

jautils.addExtendedText(flagInvert, function (trigger)
    return trigger.flag
end)

return flagInvert