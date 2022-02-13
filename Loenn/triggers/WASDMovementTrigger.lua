local jautils = require("mods").requireFromPlugin("libraries.jautils")

local trigger = {
    name = "FrostHelper/WASDMovementTrigger",
}

jautils.createPlacementsPreserveOrder(trigger, "default", {
    { "hitboxWidth", 2, "integer" },
    { "texture", "util/pixel" },
    { "speed", 80.0 },
})

return trigger