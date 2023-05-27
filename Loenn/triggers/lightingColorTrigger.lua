local jautils = require("mods").requireFromPlugin("libraries.jautils")

local trigger = {
    name = "FrostHelper/LightningBaseColorTrigger",
}

jautils.createPlacementsPreserveOrder(trigger, "default", {
    { "color", "000000", "color" },
})

return trigger