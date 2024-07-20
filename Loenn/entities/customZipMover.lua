local utils = require("utils")
local drawableLine = require("structs.drawable_line")
local drawableRectangle = require("structs.drawable_rectangle")

local jautils = require("mods").requireFromPlugin("libraries.jautils")

local zipper = {}

zipper.nodeLineRenderType = "line"

zipper.name = "FrostHelper/CustomZipMover"
zipper.depth = -9999
zipper.nodeLimits = { 1, 1 }

jautils.createPlacementsPreserveOrder(zipper, "custom_zip_mover", {
    { "width", 16 },
    { "height", 16 },
    { "lineColor", "663931", "color" },
    { "lineLightColor", "9b6157", "color" },
    { "coldLineColor", "006bb3", "color" },
    { "coldLineLightColor", "0099ff", "color" },
    { "tint", "ffffff", "color" },
    { "directory", "objects/zipmover" },
    { "speedMultiplier", 1.0 },
    { "coldSpeedMultiplier", 1 / 4 },
    { "bloomAlpha", 1.0 },
    { "bloomRadius", 6.0 },
    { "isCore", false },
    { "showLine", true },
    { "fillMiddle", true },
})

jautils.addPlacement(zipper, "slow", {
    { "lineColor", "006bb3" },
    { "lineLightColor", "0099ff" },
    { "speedMultiplier", 0.5 },
    { "coldSpeedMultiplier", 0.5 / 4 },
    { "directory", "objects/FrostHelper/customZipMover/redcog/cold" },
})

jautils.addPlacement(zipper, "fast", {
    { "lineColor", "e62e00" },
    { "lineLightColor", "ff5c33" },
    { "speedMultiplier", 2.0 },
    { "coldSpeedMultiplier", 2 / 4 },
    { "directory", "objects/FrostHelper/customZipMover/redcog" },
})

local function addNodeSprites(sprites, entity, centerX, centerY, centerNodeX, centerNodeY)
    local nodeCogSprite = jautils.getCustomSprite(entity, "directory", "/cog", "objects/zipmover/cog")

    nodeCogSprite:setPosition(centerNodeX, centerNodeY)
    nodeCogSprite:setJustification(0.5, 0.5)

    local points = {centerX, centerY, centerNodeX, centerNodeY}
    local leftLine = drawableLine.fromPoints(points, entity.lineColor, 1)
    local rightLine = drawableLine.fromPoints(points, entity.lineColor, 1)

    leftLine:setOffset(0, 4.5)
    rightLine:setOffset(0, -4.5)

    leftLine.depth = 5000
    rightLine.depth = 5000

    for _, sprite in ipairs(leftLine:getDrawableSprite()) do
        table.insert(sprites, sprite)
    end

    for _, sprite in ipairs(rightLine:getDrawableSprite()) do
        table.insert(sprites, sprite)
    end

    table.insert(sprites, nodeCogSprite)
end

local function addBlockSprites(sprites, entity, x, y, width, height, alpha)
    alpha = alpha or 1
    local tint = {1, 1, 1, alpha}
    local rectangle = drawableRectangle.fromRectangle("fill", x + 2, y + 2, width - 4, height - 4, {0, 0, 0, alpha})

    local frameSprites = jautils.getCustomBlockSprites(entity, "directory", "/block", "objects/zipmover/block", nil, nil, tint, {x=x, y=y})

    local lightsSprite = jautils.getCustomSprite(entity, "directory", "/light00", "objects/zipmover/light01")

    lightsSprite:setPosition(x + math.floor(width / 2), y)
    lightsSprite:setJustification(0.5, 0.0)
    if alpha ~= 1 then
        lightsSprite:setColor(tint)
    end


    table.insert(sprites, rectangle:getDrawableSprite())

    for _, sprite in ipairs(frameSprites) do
        --sprite:addPosition()
        table.insert(sprites, sprite)
    end

    table.insert(sprites, lightsSprite)
end

function zipper.sprite(room, entity)
    local sprites = {}

    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 8, entity.height or 8
    local halfWidth, halfHeight = math.floor(entity.width / 2), math.floor(entity.height / 2)

    local nodes = entity.nodes or {{x = 0, y = 0}}
    local nodeX, nodeY = nodes[1].x, nodes[1].y

    local centerX, centerY = x + halfWidth, y + halfHeight
    local centerNodeX, centerNodeY = nodeX + halfWidth, nodeY + halfHeight

    addNodeSprites(sprites, entity, centerX, centerY, centerNodeX, centerNodeY)
    addBlockSprites(sprites, entity, x, y, width, height)
    addBlockSprites(sprites, entity, nodeX, nodeY, width, height, .3)

    return sprites
end

function zipper.nodeSprite(room, entity)
    -- Disable node sprite, we already draw it in the main sprite function
end

function zipper.selection(room, entity)
    local main = utils.rectangle(entity.x, entity.y, entity.width, entity.height)

    if entity.nodes then
        local node = entity.nodes[1]
        local widthByTwo = entity.width / 2
        local heightByTwo = entity.height / 2

        return main, { utils.rectangle(node.x + widthByTwo - 6, node.y + heightByTwo - 6, 12, 12) }
    end

    return main, { }
end

return zipper