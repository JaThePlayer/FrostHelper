local jautils = require("mods").requireFromPlugin("libraries.jautils")
local utils = require("utils")
local drawableSpriteStruct = require("structs.drawable_sprite")

local nineSliceSolidEntity = {}

local defaultNinePatchOptions = {
    mode = "fill",
    borderMode = "repeat",
    fillMode = "repeat"
}

function nineSliceSolidEntity.createHandler(name, placements, placementsUseJaUtils, blockPathGetter, emblemPathGetter, emblemJustificationX, emblemJustificationY)
    local handler = {
        name = name,
        depth = 8990,
        selection = function(room, entity)
            return utils.rectangle(entity.x, entity.y, entity.width, entity.height)
        end,
        sprite = function(room, entity)
            local sprites = jautils.getBlockSprites(entity, blockPathGetter(entity))--jautils.getCustomBlockSprites(entity, spritePropertyName, spritePostfix, spriteFallback, spriteTintPropertyName, ninePatchOptions)

            if emblemPathGetter then
                local emblem = drawableSpriteStruct.fromTexture(emblemPathGetter(entity), entity)
                emblem:setJustification(emblemJustificationX or 0.5, emblemJustificationY or 0.5)
                emblem:setPosition(entity.x + (entity.width / 2), entity.y + (entity.height / 2))
                table.insert(sprites, emblem)
            end

            return sprites
        end
    }

    if placementsUseJaUtils then
        jautils.createPlacementsPreserveOrder(handler, "normal", placements)
    else
        handler.placements = placements
    end

    return handler
end

return nineSliceSolidEntity