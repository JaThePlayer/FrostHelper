local utils = require("utils")
local drawableSpriteStruct = require("structs.drawable_sprite")
local drawableRectangle = require("structs.drawable_rectangle")
local jautils = require("mods").requireFromPlugin("libraries.jautils")

local particlePath = "particles/bubble"

local customFireBarrier = {}

customFireBarrier.name = "FrostHelper/CustomFireBarrier"
customFireBarrier.depth = -8500

jautils.createPlacementsPreserveOrder(customFireBarrier, "normal", {
    { "width", 16 },
    { "height", 16 },
    { "isIce", false },
    { "surfaceColor", "ff8933", "color" },
    { "edgeColor", "f25e29", "color" },
    { "centerColor", "d01c01", "color" },
    { "silent", false },
})

jautils.addPlacement(customFireBarrier, "ice", {
    { "isIce", true },
    { "surfaceColor", "a6fff4" },
    { "edgeColor", "6cd6eb" },
    { "centerColor", "4ca8d6" },
})

function customFireBarrier.selection(room, entity)
    return utils.rectangle(entity.x, entity.y, entity.width, entity.height)
end

function customFireBarrier.sprite(room, entity)
    local rectangle = utils.rectangle(entity.x, entity.y, entity.width, entity.height)

    local sprites = { drawableRectangle.fromRectangle("fill", rectangle, entity.centerColor or "d01c01"):getDrawableSprite() }
    for _, value in ipairs(drawableRectangle.fromRectangle("line", rectangle, entity.surfaceColor or "ff8933"):getDrawableSprite()) do
        table.insert(sprites, value)
    end

    local particleData = {
        color = jautils.getColor(entity.surfaceColor or "ff8933"),
        x = 0,
        y = 0,
    }

    utils.setSimpleCoordinateSeed(entity.x, entity.y)
    for i = 0, (entity.width * entity.height * 0.005), 1 do
        particleData.x, particleData.y = entity.x + math.random(3, entity.width - 7), entity.y + math.random(3, entity.height - 7)
        local particle = drawableSpriteStruct.fromTexture(particlePath, particleData)

        table.insert(sprites, particle)
    end

    return sprites
end

return customFireBarrier