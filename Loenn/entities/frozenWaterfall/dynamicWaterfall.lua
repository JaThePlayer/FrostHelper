---@module "jautils"
local jautils = require("mods").requireFromPlugin("libraries.jautils")

local waterfallHelper = require("helpers.waterfalls")

local waterfall = {}

waterfall.name = "FrostHelper/DynamicWaterfall"
waterfall.depth = -9999

jautils.createPlacementsPreserveOrder(waterfall, "default", {
    { "color", "LightSkyBlue", "color" },
    { "fallSpeed", 120 },
    { "depth", -9999, "depth" },
    { "drainSpeed", 480 },
    { "drainCondition", "" },
    { "shatterBathBombs", false },
    { "collideWithPlatforms", false },
    { "collideWithHoldables", false },
})

function waterfall.sprite(room, entity)
    local rawColor = entity.color or "LightSkyBlue"
    local color = jautils.getColor(rawColor)

    local fillColor = {color[1] * 0.3, color[2] * 0.3, color[3] * 0.3, 0.3}
    local borderColor = {color[1] * 0.8, color[2] * 0.8, color[3] * 0.8, 0.8}

    return waterfallHelper.getWaterfallSprites(room, entity, fillColor, borderColor)
end

waterfall.rectangle = waterfallHelper.getWaterfallRectangle

return waterfall