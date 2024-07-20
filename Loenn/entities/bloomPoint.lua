local utils = require("utils")
local drawableSprite = require("structs.drawable_sprite")
local bloomSprite = require("mods").requireFromPlugin("libraries.bloomSprite")

local bloomPoint = {
    name = "FrostHelper/BloomPoint",
    placements = {
        name = "default",
        data = {
            alpha = 1.0,
            radius = 16.0,
        }
    },
}

function bloomPoint.sprite(room, entity)
    return bloomSprite.getSprite(entity, entity.alpha, entity.radius)
end

function bloomPoint.selection(room, entity)
    local radius = entity.radius
    local halfRadius = radius / 2


    local main = utils.rectangle(entity.x - halfRadius, entity.y - halfRadius, radius, radius)

    return main
end



return bloomPoint