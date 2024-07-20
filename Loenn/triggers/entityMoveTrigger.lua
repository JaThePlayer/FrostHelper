local jautils = require("mods").requireFromPlugin("libraries.jautils")

local flagInvert = {
    name = "FrostHelper/EntityMoveTrigger",
    nodeLimits = { 2, 2 },
    nodeLineRenderType = "line",
    nodeVisibility = "always"
}

jautils.createPlacementsPreserveOrder(flagInvert, "default", {
    { "types", "", "typesList" },
    { "blacklist", false },
    { "moveByX", 0.0 },
    { "moveByY", 0.0 },
    { "moveDuration", 1.0 },
    { "easing", "CubeInOut", jautils.easings },
    { "once", false },
}, true)


return flagInvert