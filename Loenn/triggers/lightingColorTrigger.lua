local jautils = require("mods").requireFromPlugin("libraries.jautils")

local trigger = {
    name = "FrostHelper/LightingBaseColorTrigger",
    category = "visual",
}

local misnamedVersion = {
    name = "FrostHelper/LightningBaseColorTrigger",
    category = "visual",
}

jautils.createPlacementsPreserveOrder(trigger, "default", {
    { "color", "000000", "color" },
    -- doesn't quite work yet iirc
    -- { "persistent", false },
})

misnamedVersion.fieldInformation = trigger.fieldInformation

return {
    trigger,
    misnamedVersion,
}