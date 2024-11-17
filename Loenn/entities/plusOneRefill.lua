local jautils = require("mods").requireFromPlugin("libraries.jautils")
local drawableSpriteStruct = require("structs.drawable_sprite")
local utils = require("utils")

local plusOneRefill = {}
plusOneRefill.name = "FrostHelper/PlusOneRefill"
plusOneRefill.depth = -100

jautils.createPlacementsPreserveOrder(plusOneRefill, "normal", {
    { "directory", "objects/FrostHelper/plusOneRefill"},
   --[[   
    { "directory", "objects/FrostHelper/plusOneRefill", "FrostHelper.texturePath", {
        baseFolder = "objects",
        pattern = "^(objects/.*)/outline$",
        filter = function(dir) return
                (not not drawableSpriteStruct.fromTexture(dir .. "/idle00", {}))
            and (not not drawableSpriteStruct.fromTexture(dir .. "/flash00", {}))
        end,
        captureConverter = function(dir)
            print(dir)
            return dir
        end,
        displayConverter = function(dir)
            return utils.humanizeVariableName(string.match(dir, "^.*/(.*)$") or dir)
        end,
        vanillaSprites = { "objects/spring/white" },
        langDir = "customSpring",
    }},
    ]]
    { "particleColor", "ffffff", "color" },
    { "dashCount", 1, "integer" },
    { "respawnTime", 2.5 },
    { "recoverStamina", true },
    { "oneUse", false },
    { "hitbox", "R,16,16,-8,-8", "FrostHelper.collider" },
})

function plusOneRefill.sprite(room, entity)
    return jautils.getCustomSprite(entity, "directory", "/idle00", "objects/FrostHelper/heldRefill/idle00")
end

return plusOneRefill