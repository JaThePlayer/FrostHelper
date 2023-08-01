local jautils = require("mods").requireFromPlugin("libraries.jautils")
local arbitraryShapeEntity = require("mods").requireFromPlugin("libraries.arbitraryShapeEntity")

local defaultTextures = "decals/10-farewell/clouds/cloud_c,decals/10-farewell/clouds/cloud_cc,decals/10-farewell/clouds/cloud_cd,decals/10-farewell/clouds/cloud_ce"

local cacheOptions = {
    "Auto",
    "Never",
    "RenderTarget",
}

local cloud = {
    name = "FrostHelper/ArbitraryShapeCloud",
    nodeLimits = { 3, 999 },
    depth = function (room, entity)
        return entity.depth
    end
}

jautils.createPlacementsPreserveOrder(cloud, "default", {
    { "color", "ffffff", "color" },
    { "parallax", 0.0 },
    { "depth", -10500, "depth" },
    { "textures", defaultTextures },
    { "cache", "Auto", cacheOptions },
    { "blockBloom", false },
})

cloud.sprite = arbitraryShapeEntity.getSpriteFunc("00ff00", "fcf579", function (entity)
    return entity.color
end, "ff0000")

cloud.nodeSprite = arbitraryShapeEntity.nodeSprite
cloud.selection = arbitraryShapeEntity.selection

return cloud