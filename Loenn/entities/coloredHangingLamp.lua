local jautils = require("mods").requireFromPlugin("libraries.jautils")
local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local hangingLamp = {}

hangingLamp.name = "FrostHelper/ColoredHangingLamp"
hangingLamp.depth = 2000

jautils.createPlacementsPreserveOrder(hangingLamp, "default", {
    { "height", 16, "integer" },
    { "sfx", "event:/game/02_old_site/lantern_hit" },
    { "sprite", "objects/hanginglamp" },
    { "spriteColor", "ffffff", "color" },
    { "spriteOutlineColor", "000000", "color" },
    { "color", "ffffff", "color" },
    { "alpha", 1 },
    { "startFade", 24, "integer" },
    { "endFade", 48, "integer" },
    { "bloomAlpha", 1 },
    { "bloomRadius", 48 },
}, false)

-- Manual offsets and justifications of the sprites
function hangingLamp.sprite(room, entity)
    local sprites = {}
    local height = math.max(entity.height or 0, 16)
    local pos = {
        x = entity.x,
        y = entity.y,
        color = entity.spriteColor or "ffffff",
    }

    local path = entity.sprite or "objects/hanginglamp"

    local topSprite = drawableSprite.fromTexture(path, pos)

    topSprite:setJustification(0, 0)
    topSprite:useRelativeQuad(0, 0, 8, 8)

    table.insert(sprites, topSprite)

    for i = 0, height - 16, 8 do
        local middleSprite = drawableSprite.fromTexture(path, pos)

        middleSprite:setJustification(0, 0)
        middleSprite:addPosition(0, i)
        middleSprite:useRelativeQuad(0, 8, 8, 8)

        table.insert(sprites, middleSprite)
    end

    local bottomSprite = drawableSprite.fromTexture(path, pos)

    bottomSprite:setJustification(0, 0)
    bottomSprite:addPosition(0, height - 8)
    bottomSprite:useRelativeQuad(0, 16, 8, 8)

    table.insert(sprites, bottomSprite)

    return sprites
end

function hangingLamp.selection(room, entity)
    return utils.rectangle(entity.x, entity.y, 8, math.max(entity.height, 16))
end

return {
    hangingLamp,
    jautils.createLegacyHandler(hangingLamp, "coloredlights/hanginglamp")
}