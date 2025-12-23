local jautils = require("mods").requireFromPlugin("libraries.jautils")
local drawableSpriteStruct = require("structs.drawable_sprite")
local drawableTextStruct = require("structs.drawable_text")

local exprToCounter = {
    name = "FrostHelper/ExpressionToCounterController",
    depth = 8990,
}

local exprToSlider = {
    name = "FrostHelper/ExpressionToSliderController",
    depth = 8990,
}

function exprToCounter.sprite(room, entity)
    local baseSprite = drawableSpriteStruct.fromTexture("editor/FrostHelper/ExprToCounter", entity)
    local textSprite = drawableTextStruct.fromText(entity.counter or "", entity.x - 12, entity.y - 9, 24, 24, nil, 0.25, jautils.getColor("ffffff"))
    local textSpriteIf = drawableTextStruct.fromText("=", entity.x - 12, entity.y - 7, 24, 24, nil, 0.25, jautils.getColor("aaaaaa"))
    local textSpriteCond = drawableTextStruct.fromText(entity.expression, entity.x - 12, entity.y - 5, 24, 24, nil, 0.25, jautils.getColor("ffffff"))

    return {
        baseSprite,
        textSprite,
        textSpriteIf,
        textSpriteCond,
    }
end

function exprToSlider.sprite(room, entity)
    local baseSprite = drawableSpriteStruct.fromTexture("editor/FrostHelper/ExprToSlider", entity)
    local textSprite = drawableTextStruct.fromText(entity.slider or "", entity.x - 12, entity.y - 9, 24, 24, nil, 0.25, jautils.getColor("ffffff"))
    local textSpriteIf = drawableTextStruct.fromText("=", entity.x - 12, entity.y - 7, 24, 24, nil, 0.25, jautils.getColor("aaaaaa"))
    local textSpriteCond = drawableTextStruct.fromText(entity.expression, entity.x - 12, entity.y - 5, 24, 24, nil, 0.25, jautils.getColor("ffffff"))

    return {
        baseSprite,
        textSprite,
        textSpriteIf,
        textSpriteCond,
    }
end


jautils.createPlacementsPreserveOrder(exprToCounter, "default", {
    { "counter", "" },
    { "expression", "", "FrostHelper.condition" },
})

jautils.createPlacementsPreserveOrder(exprToSlider, "default", {
    { "slider", "" },
    { "expression", "", "FrostHelper.condition" },
})

return {
    exprToCounter,
    exprToSlider
}