local blendStates = require("mods").requireFromPlugin("libraries.blendStates", "FrostHelper")
---@module "jautils"
local jautils = require("mods").requireFromPlugin("libraries.jautils", "FrostHelper")

return {
    name = "FrostHelper/Gradient",
    defaultData = {
        gradient = "ffffff,000000,50;000000,ffffff,50",
        direction = "Vertical",
        blendMode = "alphablend",
        loopX = false,
        loopY = false,
    },
    fieldInformation = {
        direction = jautils.fields.gradientDirection {},
        blendMode = {
            editable = false,
            options = blendStates.blendModes
        },
        gradient = jautils.fields.gradient {},
    },
}