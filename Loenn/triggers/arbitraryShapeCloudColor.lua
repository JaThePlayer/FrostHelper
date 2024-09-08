local enums = require("consts.celeste_enums")
local jautils = require("mods").requireFromPlugin("libraries.jautils")

local function createHandler(name, fieldInfo, extendedText)
    local h = {
        name = name,
        category = "visual",
    }

    jautils.addExtendedText(h, extendedText or function (trigger)
        return trigger.cloudTag or ""
    end)

    fieldInfo =  jautils.union({
        { "width", 16 },
        { "height", 16 },
        { "cloudTag", "", "cloudTag" },
    }, fieldInfo)

    jautils.createPlacementsPreserveOrder(h, "default", fieldInfo)

    return h
end

return {
    createHandler("FrostHelper/ArbitraryShapeCloudEditColor", {
        { "color", "ffffff", "color" },
        { "duration", 0 },
        { "easing", "Linear", jautils.easings }
    }),
    createHandler("FrostHelper/ArbitraryShapeCloudEditRainbow", {
        { "enable", true }
    }, function (trigger) return string.format("%s (%s)", trigger.cloudTag or "", trigger.enable or false) end)
}