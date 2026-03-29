---@module 'jautils'
local jautils = require("mods").requireFromPlugin("libraries.jautils")
local drawableSpriteStruct = require("structs.drawable_sprite")
local utils = require("utils")

---@type EntityHandler<UnknownEntity>
local santaRefill = {}
santaRefill.name = "FrostHelper/SantaRefill"
santaRefill.depth = -100

jautils.createPlacementsPreserveOrder(santaRefill, "normal", {
    { "directory", "objects/FrostHelper/santaRefill", jautils.fields.texturePath {
        baseFolder = "objects",
        pattern = "^(objects/.*)/outline$",
        filter = function(dir) return
                (not not drawableSpriteStruct.fromTexture(dir .. "/idle00", {}))
            and (not not drawableSpriteStruct.fromTexture(dir .. "/flash00", {}))
        end,
        captureConverter = function(dir)
            return dir
        end,
        displayConverter = function(dir)
            return utils.humanizeVariableName(string.match(dir, "^.*/(.*)$") or dir)
        end,
        vanillaSprites = { "objects/FrostHelper/santaRefill/outline" },
        langDir = "santaRefill",
    }},
    { "respawnTime", 2.5 },
    { "hitbox", "R,16,16,-8,-8", "FrostHelper.collider" },
    { "light", "ffffff;1;16;40", jautils.fields.vertexLight {
        defaultColor = "ffffff",
        defaultAlpha = 1,
        defaultStartFade = 16,
        defaultEndFade = 40,
    }},
    { "bloom", "0.8;16", jautils.fields.bloomPoint {
        defaultAlpha = 0.8,
        defaultRadius = 16,
    }},
    { "oneUse", false },
})

function santaRefill.sprite(room, entity)
    return jautils.getCustomSprite(entity, "directory", "/idle00", "objects/FrostHelper/santaRefill/idle00")
end

return santaRefill