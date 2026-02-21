---@module 'jautils'
local jautils = require("mods").requireFromPlugin("libraries.jautils")
local arbitraryShapeEntity = require("mods").requireFromPlugin("libraries.arbitraryShapeEntity")

local lightning = {
    name = "FrostHelper/ArbitraryShapeLightning",
    nodeLimits = { 2, 999 }
}

jautils.createPlacementsPreserveOrder(lightning, "default", {
    { "windingOrder", "Auto", jautils.windingOrders },
    { "edgeBolts", "fcf579,1;8cf7e2,1", jautils.fields.lightningConfig {
        defaultBoltColor = "fcf579",
        defaultBoltThickness = 1,
    }},
    { "depth", -1000100, "depth"},
    { "fillColor", "18110919", "color" },
    { "group", "" },
    { "affectedByLightningBoxes", false },
    { "fill", true },
})

lightning.sprite = arbitraryShapeEntity.getSpriteFunc("ffffff", "fcf579", "fcf57919", "ff0000")
lightning.nodeSprite = arbitraryShapeEntity.nodeSprite
lightning.selection = arbitraryShapeEntity.selection
function lightning.depth(room, entity)
    return entity.depth or -1000100
end

return lightning