local jautils = require("mods").requireFromPlugin("libraries.jautils")

local snowballTrigger = {}

local directions = {
    "Left", "Right"
}

snowballTrigger.name = "FrostHelper/SnowballTrigger"

jautils.createPlacementsPreserveOrder(snowballTrigger, "normal", {
    { "spritePath", "snowball" },
    { "direction", "Right", directions },
    { "speed", 200.0 },
    { "resetTime", 0.8 },
    { "ySineWaveFrequency", 0.5 },
    { "drawOutline", true },
    { "replaceExisting", true },
}, true)

return snowballTrigger