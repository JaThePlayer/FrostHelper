local jautils = require("mods").requireFromPlugin("libraries.jautils")
local drawableSpriteStruct = require("structs.drawable_sprite")
local utils = require("utils")

local dustEdgeColor = {1.0, 0.0, 0.0}

local builtinDirectories = {
    "danger/dustcreature",
    "frostHelper/whiteDust",
}

local dust = {
    name = "FrostHelper/DustSprite",
    depth = -50,
}

jautils.createPlacementsPreserveOrder(dust, "default", {
    { "edgeColors", "f25a10,ff0000,f21067", "colorList" },
    { "directory", "danger/dustcreature", "editableDropdown", builtinDirectories },
    { "tint", "ffffff", "color" },
    { "eyeColor", "ff0000", "color" },
    { "attachGroup", -1, "FrostHelper.attachGroup" },
    { "attachToSolid", false },
    { "rainbow", false },
    { "rainbowEyes", false },
})

function dust.sprite(room, entity)
    local baseTexture = string.format("%s/base00", entity.directory or "danger/dustcreature")
    local baseOutlineTexture = "dust_creature_outlines/base00"

    local rainbow = entity.rainbow or false

    local baseSprite = drawableSpriteStruct.fromTexture(baseTexture, entity)
    local baseOutlineSprite = drawableSpriteStruct.fromInternalTexture(baseOutlineTexture, entity)

    baseOutlineSprite:setColor(jautils.getColors(entity.edgeColors or "f25a10,ff0000,f21067")[1] or dustEdgeColor)
    baseOutlineSprite.depth = -49
    baseSprite:setColor(rainbow and jautils.getRainbowHue(room, entity.x, entity.y) or (entity.tint or "ffffff"))

    return {
        baseOutlineSprite,
        baseSprite
    }
end

function dust.selection(room, entity)
    local baseSprite = drawableSpriteStruct.fromTexture("danger/dustcreature/base00", entity)

    return baseSprite:getRectangle()
end

return dust