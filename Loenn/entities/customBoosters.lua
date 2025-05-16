local jautils = require("mods").requireFromPlugin("libraries.jautils")
local utils = require("utils")
local drawableSpriteStruct = require("structs.drawable_sprite")

local fallback = "objects/FrostHelper/dashIncrementBooster/booster00"

local dashesOptions = {
    fieldType = "integer",
    options = {
        ["-1 (Refill)"] = -1,
        ["-2 (No Change)"] = -2,
    },
    minimumValue = -2
}

local dashOutModes = {
    "Default", "EvenAtZeroDashes", "Never"
}

local function getSpriteForBooster(room, entity)
    return jautils.getCustomSprite(entity, "directory", "/booster00", fallback)
end

local function getSelectionForBooster(room, entity)
    return utils.rectangle(entity.x - 8, entity.y - 8, 16, 16)
end

local function createCustomBoosterHandler(name, mainPlacement, secondPlacement)
    local handler = {
        name = name,
        depth = -8500,
        sprite = getSpriteForBooster,
        selection = getSelectionForBooster,
    }

    jautils.createPlacementsPreserveOrder(handler, "normal", mainPlacement)
    if secondPlacement then
        jautils.addPlacement(handler, "red", secondPlacement)
    end

    return handler
end

local vanillaBoosterDirectories = {
    "objects/booster/booster00",
    "objects/FrostHelper/dashIncrementBooster/booster00",
    "objects/FrostHelper/grayBooster/booster00",
    "objects/FrostHelper/blueBooster/booster00",
    "objects/FrostHelper/yellowBooster/booster00",
}

local function getDirectoryFieldInfo(vanillaSprites) return {
    baseFolder = "objects",
    pattern = "^(objects/.*/)booster00$",
    filter = function(dir) return
            (not not drawableSpriteStruct.fromTexture(dir .. "booster00", {}))
        and (not not drawableSpriteStruct.fromTexture(dir .. "outline", {}))
    end,
    captureConverter = function(dir)
        return dir
    end,
    displayConverter = function(dir)
        return utils.humanizeVariableName(string.match(dir, "^.*/(.*)/$") or dir)
    end,
    vanillaSprites = vanillaSprites or {},
    langDir = "customBooster",
} end

local dashIncrementBooster = createCustomBoosterHandler("FrostHelper/IncrementBooster",
{
    { "respawnTime", 1.0 },
    { "boostTime", 0.25 },
    { "dashCap", -1 },
    { "dashes", 1 },
    { "particleColor", "93bd40", "color" },
    { "outlineColor", "000000", "color" },
    { "directory", "objects/FrostHelper/dashIncrementBooster/", "FrostHelper.texturePath", getDirectoryFieldInfo(vanillaBoosterDirectories) },
    { "reappearSfx", "event:/game/04_cliffside/greenbooster_reappear" },
    { "enterSfx", "event:/game/04_cliffside/greenbooster_enter" },
    { "boostSfx", "event:/game/04_cliffside/greenbooster_dash" },
    { "releaseSfx", "event:/game/04_cliffside/greenbooster_end" },
    { "redBoostDashOutMode", "Default", dashOutModes },
    { "hitbox", "C,10,0,2", "FrostHelper.collider" },
    { "red", false },
    { "refillBeforeIncrementing", false},
    { "preserveSpeed", false },
    { "staminaRecovery", true },
},
{
    { "red", true },
    { "particleColor", "c268d1" },
    { "directory", "objects/FrostHelper/dashIncrementBoosterRed/" },
    { "reappearSfx", "event:/game/05_mirror_temple/redbooster_reappear" },
    { "enterSfx", "event:/game/05_mirror_temple/redbooster_enter" },
    { "boostSfx", "event:/game/05_mirror_temple/redbooster_dash" },
    { "releaseSfx", "event:/game/05_mirror_temple/redbooster_end" },
    { "dashes", 2 },
})

local grayBooster = createCustomBoosterHandler("FrostHelper/GrayBooster", {
    { "respawnTime", 1.0 },
    { "boostTime", 0.0 },
    { "particleColor", "Gray", "color" },
    { "outlineColor", "000000", "color" },
    { "directory", "objects/FrostHelper/grayBooster/", "FrostHelper.texturePath", getDirectoryFieldInfo(vanillaBoosterDirectories) },
    { "reappearSfx", "event:/game/04_cliffside/greenbooster_reappear" },
    { "enterSfx", "event:/game/04_cliffside/greenbooster_enter" },
    { "boostSfx", "event:/game/04_cliffside/greenbooster_dash" },
    { "releaseSfx", "event:/game/04_cliffside/greenbooster_end" },
    { "redBoostDashOutMode", "Default", dashOutModes },
    { "hitbox", "C,10,0,2", "FrostHelper.collider" },
    { "red", false },
    { "dashes", -1, dashesOptions },
    { "preserveSpeed", false },
    { "staminaRecovery", true },
},
{
    { "red", true },
    { "directory", "objects/FrostHelper/grayBoosterRed/" },
    { "reappearSfx", "event:/game/05_mirror_temple/redbooster_reappear" },
    { "enterSfx", "event:/game/05_mirror_temple/redbooster_enter" },
    { "boostSfx", "event:/game/05_mirror_temple/redbooster_dash" },
    { "releaseSfx", "event:/game/05_mirror_temple/redbooster_end" },
})

local blueBooster = createCustomBoosterHandler("FrostHelper/BlueBooster", {
    { "respawnTime", 1.0 },
    { "boostTime", 0.25 },
    { "particleColor", "87cefa", "color" },
    { "outlineColor", "000000", "color" },
    { "directory", "objects/FrostHelper/blueBooster/", "FrostHelper.texturePath", getDirectoryFieldInfo(vanillaBoosterDirectories) },
    { "reappearSfx", "event:/game/04_cliffside/greenbooster_reappear" },
    { "enterSfx", "event:/game/04_cliffside/greenbooster_enter" },
    { "boostSfx", "event:/game/04_cliffside/greenbooster_dash" },
    { "releaseSfx", "event:/game/04_cliffside/greenbooster_end" },
    { "redBoostDashOutMode", "Default", dashOutModes },
    { "hitbox", "C,10,0,2", "FrostHelper.collider" },
    { "red", false },
    { "preserveSpeed", false },
    { "staminaRecovery", true },
})

local yellowBooster = createCustomBoosterHandler("FrostHelper/YellowBooster", {
    { "respawnTime", 1.0 },
    { "boostTime", 0.3 },
    { "particleColor", "Yellow", "color" },
    { "outlineColor", "000000", "color" },
    { "flashTint", "Red", "color" },
    { "directory", "objects/FrostHelper/yellowBooster/", "FrostHelper.texturePath", getDirectoryFieldInfo(vanillaBoosterDirectories) },
    { "reappearSfx", "event:/game/04_cliffside/greenbooster_reappear" },
    { "enterSfx", "event:/game/04_cliffside/greenbooster_enter" },
    { "boostSfx", "event:/game/04_cliffside/greenbooster_dash" },
    { "releaseSfx", "event:/game/04_cliffside/greenbooster_end" },
    { "hitbox", "C,10,0,2", "FrostHelper.collider" },
    -- { "redBoostDashOutMode", "Default", dashOutModes },
    { "dashes", -1, dashesOptions },
    { "preserveSpeed", false },
    { "staminaRecovery", true },
})

return {
    dashIncrementBooster,
    grayBooster,
    blueBooster,
    yellowBooster
}