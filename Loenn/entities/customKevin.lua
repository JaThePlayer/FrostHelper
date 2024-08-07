local utils = require("utils")
local drawableSpriteStruct = require("structs.drawable_sprite")

local jautils = require("mods").requireFromPlugin("libraries.jautils")

local axesEnum = {
    "Both",
    "Horizontal",
    "Vertical"
}

local customKevin = {}

customKevin.name = "FrostHelper/SlowCrushBlock"
customKevin.depth = -9000

jautils.createPlacementsPreserveOrder(customKevin, "horizontal", {
    { "width", 16 },
    { "height", 16 },
    { "directory", "objects/FrostHelper/slowcrushblock/" },
    { "crushSpeed", 120.0 },
    { "returnSpeed", 60.0 },
    { "returnAcceleration", 160.0 },
    { "crushAcceleration", 250.0 },
    { "axes", "Horizontal", axesEnum },
    { "fillColor", "62222b", "color" },
    { "chillout", false },
    { "reskinFace", false },
})

jautils.addPlacement(customKevin, "vertical", {
    { "axes", "Vertical"}
})

jautils.addPlacement(customKevin, "both", {
    { "axes", "Both" }
})

local axesToBlockIndex = {
    none = "0",
    horizontal = "1",
    vertical = "2",
    both = "3",
}

function customKevin.sprite(room, entity)
    local fillColor = jautils.getColor(entity.fillColor or "62222b")
    local giant = entity.height >= 48 and entity.width >= 48 and entity.chillout

    local sprites = { jautils.getFilledRectangleSprite({x=entity.x + 2, y=entity.y + 2, width = entity.width - 4, height = entity.height - 4}, fillColor) }
    for _, value in ipairs(jautils.getCustomBlockSprites(entity, "directory", "block0" .. axesToBlockIndex[string.lower(entity.axes or "none")], "objects/FrostHelper/slowcrushblock/block00")) do
        table.insert(sprites, value)
    end


    local facePath = giant and "objects/crushblock/giant_block00" or "objects/crushblock/idle_face"
    if entity.reskinFace then
        local dir = entity.directory
        if string.sub(dir, -1) ~= "/" then
            dir = dir + "/"
        end

        local faceTextureName = giant and "giant_block00" or "idle_face"
        facePath = dir .. faceTextureName
    end

    table.insert(sprites, drawableSpriteStruct.fromTexture(facePath, entity):setPosition(entity.x + (entity.width / 2), entity.y + (entity.height / 2)))
    return sprites
end

function customKevin.selection(room, entity)
    return utils.rectangle(entity.x, entity.y, entity.width, entity.height)
end

return customKevin