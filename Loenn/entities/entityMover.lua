local utils = require("utils")
local jautils = require("mods").requireFromPlugin("libraries.jautils")

local entityMover = {}

local outlineColor = { 255, 255, 255, 255 }
local fillColor = { 63, 63, 63, 1/6 }
local arrowColor = { 255, 255, 255, 255 }

entityMover.nodeLineRenderType = "line"
entityMover.nodeLimits = { 1, 1 }

entityMover.name = "FrostHelper/EntityMover"
entityMover.depth = -19999999

jautils.createPlacementsPreserveOrder(entityMover, "normal", {
    { "width", 16 },
    { "height", 16 },
    { "types", "" },
    { "blacklist", false },
    { "moveDuration", 1.0 },
    { "easing", "CubeInOut", jautils.easings },
    { "pauseTimeLength", 0.0 },
    { "startPauseTimeLength", 0.0 },
    { "onEndSFX", "" },
    { "mustCollide", true },
    { "relativeMovementMode", false },
})

local function getArrowSprites(entity)
    local firstNode = entity.nodes[1]
    local widthByTwo = entity.width / 2
    local heightByTwo = entity.height / 2

    local startX, startY = entity.x + widthByTwo, entity.y + heightByTwo
    local dirX, dirY = jautils.normalize((entity.x - firstNode.x), (entity.y - firstNode.y))
    local arrowLen = math.min(widthByTwo, heightByTwo)
    local endX, endY = startX - dirX * arrowLen, startY - dirY * arrowLen

    return jautils.getArrowSprites(startX, startY, endX, endY, arrowLen/4, jautils.degreeToRadians(45), 1)
end

function entityMover.sprite(room, entity)
    local sprites = {}

    jautils.addAll(sprites, jautils.getBorderedRectangleSprites(entity, fillColor, outlineColor))
    jautils.addAll(sprites, getArrowSprites(entity))

    return sprites
end

function entityMover.nodeSprite(room, entity, node, nodeIndex, viewport)
    local sprites = {}
    jautils.addAll(sprites, jautils.getBorderedRectangleSprites(utils.rectangle(node.x, node.y, entity.width, entity.height), fillColor, outlineColor))

    return sprites
end

function entityMover.selection(room, entity)
    local main = utils.rectangle(entity.x, entity.y, entity.width, entity.height)

    if entity.nodes then
        local node = entity.nodes[1]

        return main, { utils.rectangle(node.x, node.y, entity.width, entity.height) }
    end

    return main, { }
end

return entityMover