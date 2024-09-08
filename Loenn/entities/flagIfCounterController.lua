local jautils = require("mods").requireFromPlugin("libraries.jautils")
local drawableSpriteStruct = require("structs.drawable_sprite")
local drawableTextStruct = require("structs.drawable_text")

local controller = {
    name = "FrostHelper/FlagIfCounterController",
    depth = 8990,
}

function controller.sprite(room, entity)
    local baseSprite = drawableSpriteStruct.fromTexture("editor/FrostHelper/FlagToCounterController", entity)
    local textSprite = drawableTextStruct.fromText(entity.flagToSet or "", entity.x - 12, entity.y - 9, 24, 24, nil, 0.25, jautils.getColor("ffffff"))
    local textSpriteIf = drawableTextStruct.fromText("if", entity.x - 12, entity.y - 7, 24, 24, nil, 0.25, jautils.getColor("aaaaaa"))
    local textSpriteCond = drawableTextStruct.fromText(jautils.counterConditionToString(entity.counter, entity.operation, entity.target), entity.x - 12, entity.y - 5, 24, 24, nil, 0.25, jautils.getColor("ffffff"))

    return {
        baseSprite,
        textSprite,
        textSpriteIf,
        textSpriteCond,
    }
end

jautils.createPlacementsPreserveOrder(controller, "default", {
    { "flagToSet", "" },
    { "counter", "", "sessionCounter" },
    { "target", "0", "sessionCounter" },
    { "operation", "Equal", jautils.counterOperations },
})

return controller