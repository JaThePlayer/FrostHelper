---@module "jautils"
local jautils = require("mods").requireFromPlugin("libraries.jautils")
---@module "arbitraryShapeEntity"
local arbitraryShapeEntity = require("mods").requireFromPlugin("libraries.arbitraryShapeEntity")

---@type EntityHandler<Entity>
local arbitraryBloom = {
    name = "FrostHelper/ArbitraryBloom",
    nodeLimits = { 2, 999 },
    depth = -math.huge + 6
}

---@type EntityHandler<Entity>
local arbitraryBloomBlocker = {
    name = "FrostHelper/BloomBlocker",
    nodeLimits = { 2, 999 },
    depth = -math.huge + 4
}

jautils.createPlacementsPreserveOrder(arbitraryBloom, "default", {
    { "alpha", 1.0 },
    { "windingOrder", "Auto", jautils.windingOrders },
})

jautils.createPlacementsPreserveOrder(arbitraryBloomBlocker, "default", {
    { "alpha", 1.0 },
    { "windingOrder", "Auto", jautils.windingOrders },
})

arbitraryBloom.sprite = arbitraryShapeEntity.getSpriteFunc("ffffff", "ffffff", "ffffff19")
arbitraryBloom.nodeSprite = arbitraryShapeEntity.nodeSprite
arbitraryBloom.selection = arbitraryShapeEntity.selection

arbitraryBloomBlocker.sprite = arbitraryShapeEntity.getSpriteFunc("ffffff", "ffffff30", "ffffff19")
arbitraryBloomBlocker.nodeSprite = arbitraryShapeEntity.nodeSprite
arbitraryBloomBlocker.selection = arbitraryShapeEntity.selection

return {
    arbitraryBloom,
    arbitraryBloomBlocker
}