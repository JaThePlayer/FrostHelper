local jautils = require("mods").requireFromPlugin("libraries.jautils")

local snowballTrigger = {}

local directions = {
    "Left", "Right", "Top", "Bottom"
}

snowballTrigger.name = "FrostHelper/SnowballTrigger"

jautils.createPlacementsPreserveOrder(snowballTrigger, "normal", {
    { "spritePath", "snowball" },
    { "direction", "Right", directions },
    { "speed", 200.0 },
    { "resetTime", 0.8 },
    { "ySineWaveFrequency", 0.5 },
    { "safeZoneSize", 64.0 },
    { "offset", 0.0 },
    { "drawOutline", true },
    { "replaceExisting", true },
    { "once", true },
}, true)

return snowballTrigger