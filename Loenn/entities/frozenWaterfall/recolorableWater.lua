---@module "jautils"
local jautils = require("mods").requireFromPlugin("libraries.jautils")
local xnaColors = require("consts.xna_colors")

local water = {
    name = "FrostHelper/RecolorableWater",
    depth = 0,
}

jautils.createPlacementsPreserveOrder(water, "default", {
    { "color", "LightSkyBlue", "color" },
    { "triggerOnJump", true },
}, true)


local function getEntityColor(entity)
    local rawColor = entity.color or "LightSkyBlue"
    local color = jautils.getColor(rawColor) or xnaColors.LightSkyBlue

    return color
end

water.fillColor = function(room, entity)
    local color = getEntityColor(entity)

    return {color[1] * 0.3, color[2] * 0.3, color[3] * 0.3, 0.6}
end

water.borderColor = function(room, entity)
    local color = getEntityColor(entity)

    return {color[1] * 0.8, color[2] * 0.8, color[3] * 0.8, 0.8}
end

return water