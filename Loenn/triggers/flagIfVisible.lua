local jautils = require("mods").requireFromPlugin("libraries.jautils")

local flagIfVisible = {
    name = "FrostHelper/FlagIfVisibleTrigger",
    placements = {
        name = "default",
        data = {
            flag = "",
        }
    },
}

jautils.addExtendedText(flagIfVisible, function (trigger)
    return trigger.flag
end)

return flagIfVisible