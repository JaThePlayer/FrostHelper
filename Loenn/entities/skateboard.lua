local jautils = require("mods").requireFromPlugin("libraries.jautils")

local fallbackSprite = "objects/FrostHelper/skateboard"

local skateboard = {}

local directionsEnum = {
    "Left", "Right"
}

skateboard.name = "FrostHelper/Skateboard"

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