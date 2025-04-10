local fakeTilesHelper = require("helpers.fake_tiles")
local jautils = require("mods").requireFromPlugin("libraries.jautils")

local fallingBlockIgnoreSolids = {
    name = "FrostHelper/DashBlockDestroyAttached",
    sprite = fakeTilesHelper.getEntitySpriteFunction("tiletype", false),
    depth = function (room, entity)
        return entity.behind and 5000 or 0
    end,
}

local defaultBreakSounds = {
    "event:/game/general/wall_break_stone",
    "event:/game/general/wall_break_wood",
    "event:/game/general/wall_break_ice",
    "event:/game/general/wall_break_dirt",
    "",
}

jautils.createPlacementsPreserveOrder(fallingBlockIgnoreSolids, "default", {
    { "width", 8 },
    { "height", 8 },
    { "tiletype", "3", "tiletype", { layer = "tilesFg" } },
    { "breakSfx", "", "editableDropdown", defaultBreakSounds },
    { "blendin", true },
    { "canDash", true },
    { "permanent", false }
})

return fallingBlockIgnoreSolids