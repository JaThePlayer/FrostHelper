---@module 'jautils'
local jautils = require("mods").requireFromPlugin("libraries.jautils")

local fallbackSprite = "objects/FrostHelper/skateboard"

---@class Skateboard : Entity
---@field direction "Left"|"Right"?

---@type EntityHandler
local skateboard = {
    name = "FrostHelper/Skateboard",
    nodeVisibility = "selected",
    fieldInformation = {
        x = {
            options = {
                yo = 4,
                x = 2,
            }
        }
    }
}

local directionsEnum = {
    "Left", "Right"
}

jautils.createPlacementsPreserveOrder(skateboard, "normal", {
    { "direction", "Right", directionsEnum },
    { "sprite", fallbackSprite },
    { "speed", 90.0 },
    { "hitbox", "R,20,6,-10,-7", "FrostHelper.collider" },
    { "keepMoving", false }
})

function skateboard.sprite(room, entity)
    local sprite = jautils.getCustomSprite(entity, "sprite", "", fallbackSprite)
    sprite.y = sprite.y + 4
    sprite.scaleX = entity.direction == "Right" and -1 or 1
    return sprite
end

return skateboard