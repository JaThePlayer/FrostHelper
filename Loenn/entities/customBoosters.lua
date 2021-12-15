local jautils = require("mods").requireFromPlugin("libraries.jautils")
local utils = require("utils")

local fallback = "objects/FrostHelper/dashIncrementBooster/booster00"

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

local dashIncrementBooster = createCustomBoosterHandler("FrostHelper/IncrementBooster",
{
    { "respawnTime", 1.0 },
    { "boostTime", 0.25 },
    { "dashCap", -1 },
    { "dashes", 1 },
    { "particleColor", "93bd40", "color" },
    { "directory", "objects/FrostHelper/dashIncrementBooster/" },
    { "reappearSfx", "event:/game/04_cliffside/greenbooster_reappear" },
    { "enterSfx", "event:/game/04_cliffside/greenbooster_enter" },
    { "boostSfx", "event:/game/04_cliffside/greenbooster_dash" },
    { "releaseSfx", "event:/game/04_cliffside/greenbooster_end" },
    { "red", false },
    { "refillBeforeIncrementing", false},
    { "preserveSpeed", false },
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
    { "directory", "objects/FrostHelper/grayBooster/" },
    { "reappearSfx", "event:/game/04_cliffside/greenbooster_reappear" },
    { "enterSfx", "event:/game/04_cliffside/greenbooster_enter" },
    { "boostSfx", "event:/game/04_cliffside/greenbooster_dash" },
    { "releaseSfx", "event:/game/04_cliffside/greenbooster_end" },
    { "red", false },
    { "dashes", -1 },
    { "preserveSpeed", false },
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
    { "directory", "objects/FrostHelper/blueBooster/" },
    { "reappearSfx", "event:/game/04_cliffside/greenbooster_reappear" },
    { "enterSfx", "event:/game/04_cliffside/greenbooster_enter" },
    { "boostSfx", "event:/game/04_cliffside/greenbooster_dash" },
    { "releaseSfx", "event:/game/04_cliffside/greenbooster_end" },
    { "red", false },
    { "preserveSpeed", false },
})

local yellowBooster = createCustomBoosterHandler("FrostHelper/YellowBooster", {
    { "respawnTime", 1.0 },
    { "boostTime", 0.3 },
    { "particleColor", "Yellow", "color" },
    { "flashTint", "Red", "color" },
    { "directory", "objects/FrostHelper/yellowBooster/" },
    { "reappearSfx", "event:/game/04_cliffside/greenbooster_reappear" },
    { "enterSfx", "event:/game/04_cliffside/greenbooster_enter" },
    { "boostSfx", "event:/game/04_cliffside/greenbooster_dash" },
    { "releaseSfx", "event:/game/04_cliffside/greenbooster_end" },
    { "dashes", -1 },
})

return {
    dashIncrementBooster,
    grayBooster,
    blueBooster,
    yellowBooster
}