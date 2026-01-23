---@module 'jautils'
local jautils = require("mods").requireFromPlugin("libraries.jautils")
local arbitraryShapeEntity = require("mods").requireFromPlugin("libraries.arbitraryShapeEntity")

local lightning = {
    name = "FrostHelper/ArbitraryShapeLightning",
    nodeLimits = { 2, 999 }
}

jautils.createPlacementsPreserveOrder(lightning, "default", {
    { "windingOrder", "Auto", jautils.windingOrders },
    { "edgeBolts", "fcf579,1;8cf7e2,1", jautils.fields.list {
        elementSeparator = ";",
        elementDefault = "fcf579,1",
        elementOptions = jautils.fields.complex {
            separator = ",",
            innerFields = {
                {
                    name = "FrostHelper.fields.lightning.boltColor",
                    default = "fcf579",
                    info = jautils.fields.color { }
                },
                {
                    name = "FrostHelper.fields.lightning.boltThickness",
                    default = 1,
                    info = jautils.fields.nonNegativeNumber { }
                },
            }
        },
    } },
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