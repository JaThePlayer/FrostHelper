local jautils = require("mods").requireFromPlugin("libraries.jautils")

---@type TriggerHandler
local flashlightColor = {
    name = "FrostHelper/FlashlightColorTrigger",
    depth = 2000,
    category = "visual"
}

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