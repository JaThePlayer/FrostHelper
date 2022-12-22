local jautils = require("mods").requireFromPlugin("libraries.jautils")

local temporaryFlag = {
    name = "FrostHelper/TemporaryFlagTrigger",
    placements = {
        name = "default",
        data = {
            flag = ""
        }
    }
}

jautils.addExtendedText(temporaryFlag, function (trigger)
    return trigger.flag
end)

return temporaryFlag