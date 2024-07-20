local jautils = require("mods").requireFromPlugin("libraries.jautils")
local arbitraryShapeEntity = require("mods").requireFromPlugin("libraries.arbitraryShapeEntity")

local lightning = {
    name = "FrostHelper/ArbitraryShapeLightning",
    nodeLimits = { 2, 999 },
    depth = -math.huge + 6
}

jautils.createPlacementsPreserveOrder(lightning, "default", {
    { "windingOrder", "Auto", jautils.windingOrders },
    { "fill", true },
})

lightning.sprite = arbitraryShapeEntity.getSpriteFunc("ffffff", "fcf579", "fcf57919", "ff0000")
lightning.nodeSprite = arbitraryShapeEntity.nodeSprite
lightning.selection = arbitraryShapeEntity.selection

return lightning