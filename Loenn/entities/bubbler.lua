local jautils = require("mods").requireFromPlugin("libraries.jautils")

local bubbler = {
    name = "FrostHelper/Bubbler",
    depth = 0,
    texture = "objects/FrostHelper/bubble00"
}

jautils.createPlacementsPreserveOrder(bubbler, "normal", {
    { "visible", true },
    { "color", "ffffff", "color"}
})

return bubbler