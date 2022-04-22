local jautils = require("mods").requireFromPlugin("libraries.jautils")
local utils = require("utils")
local drawableSprite = require("structs.drawable_sprite")
local frostSettings = require("mods").requireFromPlugin("libraries.settings")

local particlePath = "objects/dreamblock/particles"
local particleColors = { jautils.getColor("FFEF11"), jautils.getColor("FF00D0"), jautils.getColor("08a310"),
                         jautils.getColor("5fcde4"), jautils.getColor("7fb25e"), jautils.getColor("E0564C"),
                         jautils.getColor("5b6ee1"), jautils.getColor("CC3B3B"), jautils.getColor("7daa64"), }

local particleSizes = { 1, 4, 6}

local dreamBlock = {}

dreamBlock.name = "FrostHelper/CustomDreamBlock"
dreamBlock.depth = -9000

dreamBlock.nodeLineRenderType = "line"
jautils.createPlacementsPreserveOrder(dreamBlock, "custom_dream_block", {
    { "width", 16 },
    { "height", 16 },
    { "oneUse", false },
    { "below", false },
    { "speed", 240.0 },
    { "conserveSpeed", false },
    { "activeBackColor", "000000", "color" },
    { "disabledBackColor", "1f2e2d", "color" },
    { "activeLineColor", "ffffff", "color" },
    { "disabledLineColor", "6a8480", "color" },
    { "allowRedirects", false },
    { "allowSameDirectionDash", false },
    { "sameDirectionSpeedMultiplier", 2.0 },
    { "old", false },
    { "moveEase", "SineInOut", jautils.easings },
    { "moveSpeedMult", 1.0 },
})

local function addParticles(sprites, entity)
    utils.setSimpleCoordinateSeed(entity.x, entity.y)
    local baseParticle = drawableSprite.fromTexture(particlePath, entity)
    for i = 1, entity.width / 8 * (entity.height / 8) * 0.7, 1 do
        local particle = drawableSprite.fromMeta(baseParticle.meta, entity)
        local particleIndex = math.random(0,2)
        local particleSize = particleSizes[particleIndex + 1]
        particle:useRelativeQuad(particleIndex * 6, 0, particleSize, particleSize)
        particle:setJustification(0.0, 0.0)
        particle:setColor(particleColors[math.random(1, 9)])
        particle:setPosition(entity.x + math.random(0, entity.width - particleSize - 3), entity.y + math.random(0, entity.height - particleSize - 3))

        table.insert(sprites, particle)
    end
end

function dreamBlock.sprite(room, entity)
    local rectangle = utils.rectangle(entity.x, entity.y, entity.width, entity.height)

    local sprites = jautils.getBorderedRectangleSprites(rectangle, entity.activeBackColor or "000000", entity.activeLineColor or "ffffff")

    if frostSettings.fancyDreamBlocks() then
        addParticles(sprites, entity)
    end

    return sprites
end


function dreamBlock.depth(room, entity)
    return entity.below and 5000 or -11000
end

dreamBlock.nodeLimits = { 0, 1 }

function dreamBlock.selection(room, entity)
    local main = utils.rectangle(entity.x, entity.y, entity.width, entity.height)
    local nodes = {}

    if entity.nodes then
        for i, node in ipairs(entity.nodes) do
            nodes[i] = utils.rectangle(node.x, node.y, entity.width, entity.height)
        end
    end

    return main, nodes
end

return dreamBlock