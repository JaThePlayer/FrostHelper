local jautils = require("mods").requireFromPlugin("libraries.jautils")
local lightBeamHelper = require("helpers.light_beam")

local coloredLightbeam = {
    name = "FrostHelper/ColoredLightbeam",
    depth = -9998,
    sprite = lightBeamHelper.getSprites,
    selection = lightBeamHelper.getSelection
}

jautils.createPlacementsPreserveOrder(coloredLightbeam, "normal", {
    { "width", 24 },
    { "height", 24 },
    { "rotation", 0 },
    { "color", "ccffff", "color" },
    { "flag", "" },
})

return coloredLightbeam