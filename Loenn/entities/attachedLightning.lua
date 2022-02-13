local jautils = require("mods").requireFromPlugin("libraries.jautils")

local attachedLightning = {
    name = "FrostHelper/AttachedLightning",
    depth = -1000100,
    fillColor = {0.55, 0.97, 0.96, 0.4},
    borderColor = {0.99, 0.96, 0.47, 1.0},
    nodeLineRenderType = "line",
    nodeLimits = {0, 1},
}

jautils.createPlacementsPreserveOrder(attachedLightning, "default", {
    { "width", 8 },
    { "height", 8 },
    { "perLevel", false },
    { "moveTime", 5.0 },
    { "attachGroup", 0, "FrostHelper.attachGroup" },
})

return attachedLightning