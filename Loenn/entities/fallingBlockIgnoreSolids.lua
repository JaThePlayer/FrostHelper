local fakeTilesHelper = require("helpers.fake_tiles")
local jautils = require("mods").requireFromPlugin("libraries.jautils")

local fallingBlockIgnoreSolids = {
    name = "FrostHelper/FallingBlockIgnoreSolids",
    sprite = fakeTilesHelper.getEntitySpriteFunction("tiletype", false),
    depth = function (room, entity)
        return entity.behind and 5000 or 0
    end,
}

jautils.createPlacementsPreserveOrder(fallingBlockIgnoreSolids, "default", {
    { "width", 8 },
    { "height", 8 },
    { "tiletype", "3" },
    { "climbFall", true },
    { "behind", false },
    { "allowStaticMovers", true },
})

fallingBlockIgnoreSolids.fieldInformation = fakeTilesHelper.getFieldInformation("tiletype")

return fallingBlockIgnoreSolids