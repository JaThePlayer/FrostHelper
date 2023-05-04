local utils = require("utils")
local mods = require("mods")
local jautils = mods.requireFromPlugin("libraries.jautils")
local fhDebugRC = mods.requireFromPlugin("libraries.debugRCAPI")
local entities = jautils.inLonn and require("entities") or nil

local entityMover = {}

local outlineColor = { 255, 255, 255, 1 }
local fillColor = { 63, 63, 63, 1/6 }

local nodeAlpha = .4
local nodeOutlineColor = { 255 * nodeAlpha, 255 * nodeAlpha, 255 * nodeAlpha, 1 * nodeAlpha }
local nodeFillColor = { 63 * nodeAlpha, 63 * nodeAlpha, 63 * nodeAlpha, 1/6 * nodeAlpha }
local arrowColor = { 255, 255, 255, 0.5 }

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

local function getAffectedPredicate(types, blacklist)
    return blacklist
    and function(entity)
        return entity._name ~= "FrostHelper/EntityMover" and not types[entity._name]
    end
    or function(entity)
        return entity._name ~= "FrostHelper/EntityMover" and types[entity._name]
    end
end

---tries to set the alpha of a drawable
local function setAlpha(drawable, alpha)
    if drawable.color then
        local c = drawable.color
        drawable.color = {c[1], c[2], c[3], (c[4] or 1) * alpha}
    elseif drawable.setColor then
        drawable:setColor({1, 1, 1, alpha})
    end

    return drawable
end

---offsets the position of a point to where it will move thanks to the entity mover
---mutates the first argument!
local function offsetPos(e, diffX, diffY, relative)
    if relative then
        e.x, e.y = e.x + diffX, e.y + diffY
    else
        e.x, e.y = diffX, diffY
    end
end

local function addSpritesOfMovedEntities(sprites, entity, room, viewport, node)
    local types = fhDebugRC.entityTypesToNamesKeyIndexed(entity.types)
    local affected = utils.filter(getAffectedPredicate(types, entity.blacklist), room.entities)
    local selfRect = utils.rectangle(entity.x, entity.y, entity.width, entity.height)

    local diffX, diffY
    if entity.relativeMovementMode then
        diffX, diffY = node.x - entity.x, node.y - entity.y
    else
        diffX, diffY = node.x + entity.width / 2, node.y + entity.height / 2
    end

    for _, e in ipairs(affected) do
        if entity == e then goto next end

        if entity.mustCollide then
            local selection = entities.getSelection(room, e, viewport)
            if not utils.aabbCheck(selection, selfRect) then
                goto next
            end
        end

        -- change the location of the entity to the destination
        local ox, oy = e.x, e.y
        local oldNodes
        local relative = entity.relativeMovementMode

        offsetPos(e, diffX, diffY, relative)

        if e.nodes then
            oldNodes = utils.deepcopy(e.nodes)

            for _, n in ipairs(e.nodes) do
                offsetPos(n, diffX, diffY, relative)
            end
        end

        local drawable = entities.getEntityDrawable(e._name, nil, room, e, viewport)

        -- reset the entity
        e.x, e.y = ox, oy
        if e.nodes then e.nodes = oldNodes end

        if drawable._type then
            -- if _type is present, this is definitely one of Cruor's types,
            -- let's assume it's the drawable we want
            table.insert(sprites, setAlpha(drawable, 0.3))
        else
            for _, draw in ipairs(drawable) do
                table.insert(sprites, setAlpha(draw, 0.3))
            end
        end

        ::next::
    end
end

local function getArrowSprites(entity)
    local firstNode = entity.nodes[1]
    local widthByTwo = entity.width / 2
    local heightByTwo = entity.height / 2

    local startX, startY = entity.x + widthByTwo, entity.y + heightByTwo
    local dirX, dirY = jautils.normalize((entity.x - firstNode.x), (entity.y - firstNode.y))
    local arrowLen = math.min(widthByTwo, heightByTwo)
    local endX, endY = firstNode.x + widthByTwo, firstNode.y + heightByTwo--startX - dirX * arrowLen, startY - dirY * arrowLen

    return jautils.getArrowSprites(startX, startY, endX, endY, arrowLen/4, jautils.degreeToRadians(45), 1, arrowColor)
end

function entityMover.sprite(room, entity, viewport)
    local sprites = {}

    local node = entity.nodes[1]

    jautils.addAll(sprites, jautils.getBorderedRectangleSprites(entity, fillColor, outlineColor))
    jautils.addAll(sprites, getArrowSprites(entity))

    -- node
    jautils.addAll(sprites, jautils.getBorderedRectangleSprites(utils.rectangle(node.x, node.y, entity.width, entity.height), nodeFillColor, nodeOutlineColor))

    if jautils.inLonn then
        addSpritesOfMovedEntities(sprites, entity, room, viewport, node)
    end

    return sprites
end

function entityMover.nodeSprite(room, entity, node, nodeIndex, viewport)
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