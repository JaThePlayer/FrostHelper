local drawableNinePatch = require("structs.drawable_nine_patch")
local drawableSprite = require("structs.drawable_sprite")
local enums = require("consts.celeste_enums")
local utils = require("utils")
local drawableSpriteStruct = require("structs.drawable_sprite")
local jautils = require("mods").requireFromPlugin("libraries.jautils")

local switchGate = {}

local textures = {
    "block", "mirror", "temple", "stars"
}
local textureOptions = {}

for _, texture in ipairs(textures) do
    textureOptions[utils.titleCase(texture)] = texture
end

switchGate.name = "FrostHelper/RainbowSwitchGate"
switchGate.depth = -9000
switchGate.nodeLimits = {1, 1}
switchGate.nodeLineRenderType = "line"
switchGate.warnBelowSize = {16, 16}
switchGate.fieldInformation = {
    sprite = {
        options = textureOptions
    }
}

local frameTexture = "objects/switchgate/%s"

jautils.createPlacementsPreserveOrder(switchGate, "default", {
    { "sprite", "block", "FrostHelper.texturePath", {
        baseFolder = "objects/switchgate",
        pattern = "^objects/switchgate/(.*)$",
        captureConverter = function(dir)
            return dir
        end,
        displayConverter = function(dir)
            return utils.humanizeVariableName(string.match(dir, "^.*/(.*)/$") or dir)
        end,
        vanillaSprites = { "objects/switchgate/icon00" },
        langDir = "rainbowSwitchGate",
    }},
    { "persistent", false }
}, true)

local ninePatchOptions = {
    mode = "fill",
    borderMode = "repeat",
    fillMode = "repeat"
}


local middleTexture = "objects/switchgate/icon00"

function switchGate.sprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 24, entity.height or 24

    local blockSprite = entity.sprite or "block"
    local frame = string.format(frameTexture, blockSprite)

    local ninePatch = drawableNinePatch.fromTexture(frame, ninePatchOptions, x, y, width, height)
    local middleSprite = drawableSprite.fromTexture(middleTexture, entity)
    local sprites = ninePatch:getDrawableSprite()
    --jautils.rainbowifyAll(room, sprites)

    middleSprite:addPosition(math.floor(width / 2), math.floor(height / 2))
    table.insert(sprites, middleSprite)

    return sprites
end

function switchGate.selection(room, entity)
    local nodes = entity.nodes or {}
    local x, y = entity.x or 0, entity.y or 0
    local nodeX, nodeY = nodes[1].x or x, nodes[1].y or y
    local width, height = entity.width or 24, entity.height or 24

    return utils.rectangle(x, y, width, height), {utils.rectangle(nodeX, nodeY, width, height)}
end

return switchGate