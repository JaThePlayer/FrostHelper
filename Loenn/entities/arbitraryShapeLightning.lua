local jautils = require("mods").requireFromPlugin("libraries.jautils")
local arbitraryShapeEntity = require("mods").requireFromPlugin("libraries.arbitraryShapeEntity")

local lightning = {
    name = "FrostHelper/ArbitraryShapeLightning",
    nodeLimits = { 2, 999 },
    depth = -math.huge + 6
}

jautils.createPlacementsPreserveOrder(lightning, "default", {
    { "windingOrder", "Auto", jautils.windingOrders },
    { "edgeBolts", "fcf579,1;8cf7e2,1", "list", {
        elementSeparator = ";",
        elementDefault = "fcf579,1",
        elementOptions = {
            fieldType = "FrostHelper.complexField",
            separator = ",",
            innerFields = {
                {
                    name = "FrostHelper.fields.lightning.boltColor",
                    default = "fcf579",
                    info = {
                        fieldType = "color",
                        allowXNAColors = true,
                        useAlpha = true,
                    }
                },
                {
                    name = "FrostHelper.fields.lightning.boltThickness",
                    default = 1,
                    info = {
                        fieldType = "number",
                    }
                },
            }
        },
    } },
    { "depth", -1000100, "depth"},
    { "fillColor", "18110919", "color" },
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