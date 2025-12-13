---@module "jautils"
local jautils = require("mods").requireFromPlugin("libraries.jautils")

if not jautils.devMode then
    return
end

local drawableSpriteStruct = require("structs.drawable_sprite")

local cloud = {
    name = "FrostHelper/Raincloud",
    depth = 0,
}

jautils.createPlacementsPreserveOrder(cloud, "default", {
    { "color", "LightSkyBlue", "color" },
    { "fragile", false },
    { "small", false },
})

local normalScale = 1.0
local smallScale = 29 / 35

local function getTexture(entity)
    local fragile = entity.fragile

    if fragile then
        return "objects/clouds/fragile00"

    else
        return "objects/clouds/cloud00"
    end
end

function cloud.sprite(room, entity)
    local texture = getTexture(entity)
    local sprite = drawableSpriteStruct.fromTexture(texture, entity)
    local small = entity.small
    local scale = small and smallScale or normalScale

    sprite:setScale(scale, 1.0)

    return sprite
end

return cloud