local jautils = require("mods").requireFromPlugin("libraries.jautils")
local drawableSpriteStruct = require("structs.drawable_sprite")
local drawableTextStruct = require("structs.drawable_text")

local controller = {
    name = "FrostHelper/FlagCounterController",
    depth = 8990,
}

function controller.sprite(room, entity)
    local baseSprite = drawableSpriteStruct.fromTexture("editor/FrostHelper/FlagToCounterController", entity)
    local textSprite = drawableTextStruct.fromText(entity.counter or "", entity.x - 12, entity.y - 9, 24, 24, nil, 0.25, jautils.getColor("ffffff"))

    return {
        baseSprite,
        textSprite
    }
end

jautils.createPlacementsPreserveOrder(controller, "default", {
    { "counter", "", "sessionCounter" },
    { "flags", "", "list", {
        elementDefault = "",
        elementOptions = {
            fieldType = "FrostHelper.complexField",
                separator = ";",
                innerFields = {
                    {
                        name = "FrostHelper.fields.flagCounterController.flag",
                    },
                    {
                        name = "FrostHelper.fields.flagCounterController.value",
                        default = 1,
                    },
                }
        },
    }},
})

return controller