local jautils = require("mods").requireFromPlugin("libraries.jautils")
local drawableSpriteStruct = require("structs.drawable_sprite")
local drawableLine = require("structs.drawable_line")
local utils = require("utils")

local builtinTexturePaths = {
    "objects/FrostHelper/growBlock/green",
    "objects/FrostHelper/growBlock/greenBig",
	"objects/FrostHelper/growBlock/greenWide",
	"objects/FrostHelper/growBlock/gray",
    "objects/FrostHelper/growBlock/grayBig",
	"objects/FrostHelper/growBlock/grayWide",
}

local growBlock = {
    name = "FrostHelper/GrowBlock",
    depth = -9000,
    nodeLimits = { 1, math.huge },
    nodeLineRenderType = "line",
    nodeVisibility = "never",
}

jautils.createPlacementsPreserveOrder(growBlock, "default", {
    { "flag", "" },
    { "texture", "objects/FrostHelper/growBlock/green", "editableDropdown",  builtinTexturePaths },
    { "blockGrowTime", 0.3 },
    { "vanishTime", 1.0 },
    { "giveLiftBoost", true },
	{ "tint", "ffffff", "color" },
    { "vanishOnFlagUnset", false },
    { "version", 1, "integer" }
    -- { "maxBlocks", 0 },
})

growBlock.ignoredFields = { "version" }

function growBlock.nodeSprite() end

local function getBaseTextureAndSizeForBlock(entity)
    local texturePath = entity.texture or builtinTexturePaths[1]
    local baseTexture = drawableSpriteStruct.fromTexture(texturePath, entity) or drawableSpriteStruct.fromTexture(builtinTexturePaths[1], entity)
    baseTexture:setJustification(0, 0)
	baseTexture:setColor(entity.tint or "ffffff")

    local textureMeta = baseTexture.meta

    return baseTexture, textureMeta.width, textureMeta.height
end

local previewTextureAlpha = 0.2
local nodeTextureAlpha = 0.4

function growBlock.sprite(room, entity)
    local baseTexture, textureWidth, textureHeight = getBaseTextureAndSizeForBlock(entity)
    local sprites = { jautils.copyTexture(baseTexture, entity.x, entity.y) }

    local x, y = entity.x, entity.y
    for _, node in ipairs(entity.nodes) do
        local nx, ny = node.x, node.y

        while not jautils.equalVec(x, y, nx, ny) do
            x = jautils.approach(x, nx, textureWidth)
            y = jautils.approach(y, ny, textureHeight)

            local sprite = jautils.copyTexture(baseTexture, math.floor(x), math.floor(y))

            local alpha = jautils.equalVec(x, y, nx, ny) and nodeTextureAlpha or previewTextureAlpha

            sprite:setColor(jautils.multColor(sprite.color, alpha))

            table.insert(sprites, sprite)
        end

        x, y = nx, ny
    end

    return sprites
end

function growBlock.selection(room, entity)
    local baseTexture, width, height = getBaseTextureAndSizeForBlock(entity)

    local main = utils.rectangle(entity.x, entity.y, width, height)

    if entity.nodes then
        local nodeSelections = {}
        for _,node in ipairs(entity.nodes) do
            table.insert(nodeSelections, utils.rectangle(node.x, node.y, width, height))
        end
        return main, nodeSelections
    end

    return main, { }
end


return growBlock