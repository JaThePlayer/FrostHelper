local jautils = require("mods").requireFromPlugin("libraries.jautils", "FrostHelper")
local blendStates = require("mods").requireFromPlugin("libraries.blendStates", "FrostHelper")

local blendStateTrigger = {
    name = "FrostHelper/StylegroundBlendModeTrigger",
}

jautils.createPlacementsPreserveOrder(blendStateTrigger, "default", {
    { "tag", "", "FrostHelper.stylegroundTag" },
    { "colorWriteChannels", "All", "editableDropdown", blendStates.colorWriteChannels },

    { "alphaBlendFunction", "Add", blendStates.blendFunctions },
    { "colorBlendFunction", "Add", blendStates.blendFunctions },

    { "colorSourceBlend", "One", blendStates.blends },
    { "colorDestinationBlend", "InverseSourceAlpha", blendStates.blends },
    { "alphaSourceBlend", "One", blendStates.blends },
    { "alphaDestinationBlend", "InverseSourceAlpha", blendStates.blends },

    { "blendFactor", "ffffff", "color" },
}, true)

jautils.addExtendedText(blendStateTrigger, function(trigger)
    return trigger.tag
end)

return blendStateTrigger