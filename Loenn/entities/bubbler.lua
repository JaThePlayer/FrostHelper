local jautils = require("mods").requireFromPlugin("libraries.jautils")

local bubbler = {
    name = "FrostHelper/Bubbler",
    depth = 0,
    texture = "objects/FrostHelper/bubble00",
    nodeLimits = {2, 2},
    nodeLineRenderType = "line",
}

jautils.createPlacementsPreserveOrder(bubbler, "normal", {
    { "visible", true },
    { "color", "ffffff", "color"}
})

return bubbler