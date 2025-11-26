---@module "jautils"
local jautils = require("mods").requireFromPlugin("libraries.jautils")

local shaderTrigger = {
    name = "FrostHelper/ScreenwideShaderTrigger",
    category = "visual",
}

jautils.createPlacementsPreserveOrder(shaderTrigger, "default", {
    { "effects", "", "list" },
    { "flag", "", "FrostHelper.condition" },
    { "alwaysOn", true },
    { "parameters", "", jautils.effectParametersFieldData }
}, true)

jautils.addExtendedText(shaderTrigger, function (trigger)
    return trigger.effects
end)

return shaderTrigger