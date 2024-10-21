local jautils = require("mods").requireFromPlugin("libraries.jautils")
local drawableSpriteStruct = require("structs.drawable_sprite")
local drawableTextStruct = require("structs.drawable_text")

local controller = {
    name = "FrostHelper/TimeToCounterController",
    depth = 8990,
}

function controller.sprite(room, entity)
    local baseSprite = drawableSpriteStruct.fromTexture("editor/FrostHelper/SessionTimeToCounterController", entity)
    local textSprite = drawableTextStruct.fromText(entity.counter or "", entity.x - 12.5, entity.y - 4, 24, 24, nil, 0.25, jautils.getColor("ffffff"))

    return {
        baseSprite,
        textSprite
    }
end

jautils.createPlacementsPreserveOrder(controller, "default", {
    { "counter", "", "sessionCounter" },
    { "unit", "Milliseconds", jautils.counterTimeUnits },
    { "timerKind", "Session", { "Session", "File" } },
})

return controller