local jautils = require("mods").requireFromPlugin("libraries.jautils")

local flashlightColor = {}

flashlightColor.name = "FrostHelper/FlashlightColorTrigger"
flashlightColor.depth = 2000
flashlightColor.category = "visual"

flashlightColor.placements = {
    name = "default",
    data = {
        width = 16,
        height = 16,
        time = -1,
        color = "00000000",
        persistent = true,
    }
}

flashlightColor.fieldInformation = {
    color = {
        fieldType = "color",
        allowXNAColors = true,
        allowAlpha = true,
    }
}

return {
    flashlightColor,
    jautils.createLegacyHandler(flashlightColor, "coloredlights/flashlightColorTrigger")
}