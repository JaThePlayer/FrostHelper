local jautils = require("mods").requireFromPlugin("libraries.jautils")

local timer = {
    name = "FrostHelper/Timer",
}

jautils.createPlacementsPreserveOrder(timer, "default", {
    { "width", 16 },
    { "height", 16 },
    { "flag", "" },
    { "time", 1.0 },
    { "textColor", "ffffff", "color" },
    { "iconColor", "ffffff", "color" },
    { "icon", "frostHelper/time" },
})

return timer