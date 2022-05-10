local jautils = require("mods").requireFromPlugin("libraries.jautils")
local arbitraryShapeEntity = require("mods").requireFromPlugin("libraries.arbitraryShapeEntity")

local arbitraryBloom = {
    name = "FrostHelper/ArbitraryBloom",
    nodeLimits = { 2, 999 },
    depth = -math.huge + 6
}

jautils.createPlacementsPreserveOrder(arbitraryBloom, "default", {
    { "alpha", 1.0 },
})

arbitraryBloom.sprite = arbitraryShapeEntity.getSpriteFunc("ffffff", "ffffff", "ffffff19")
arbitraryBloom.nodeSprite = arbitraryShapeEntity.nodeSprite
arbitraryBloom.selection = arbitraryShapeEntity.selection

return arbitraryBloom