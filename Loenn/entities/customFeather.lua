local jautils = require("mods").requireFromPlugin("libraries.jautils")
local utils = require("utils")
local fallback = "objects/flyFeather/idle00"
local feather = {}

feather.name = "FrostHelper/CustomFeather"
feather.depth = 0

jautils.createPlacementsPreserveOrder(feather, "normal", {
    { "shielded", false },
    { "singleUse", false },
    { "flyColor", "ffd65c", "color" },
    { "spriteColor", "ffffff", "color" },
    { "flyTime", 2.0 },
    { "respawnTime", 3.0 },
    { "maxSpeed", 190.0 },
    { "lowSpeed", 140.0 },
    { "neutralSpeed", 91.0 },
    { "spritePath", "objects/flyFeather/" },
})

function feather.draw(room, entity)
    local featherSprite = jautils.getCustomSprite(entity, "spritePath", "idle00", fallback, "spriteColor")

    if entity.shielded then
        love.graphics.circle("line", entity.x, entity.y, 12)
    end

    featherSprite:draw()
end

function feather.selection(room, entity)
    return utils.rectangle(entity.x - 12, entity.y - 12, 24, 24)
end

return feather