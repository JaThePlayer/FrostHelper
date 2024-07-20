local jautils = require("mods").requireFromPlugin("libraries.jautils")

local shaderTrigger = {
    name = "FrostHelper/ScreenwideShaderTrigger",
    category = "visual",
    placements = {
        name = "default",
        data = {
            effects = "",
            alwaysOn = true,
            flag = "",
        }
    }
}

jautils.addExtendedText(shaderTrigger, function (trigger)
    return trigger.effects
end)

return shaderTrigger