local utils = require("utils")
local jautils = require("mods").requireFromPlugin("libraries.jautils")
local drawableSpriteStruct = require("structs.drawable_sprite")

local customRisingLava = {}
customRisingLava.name = "FrostHelper/CustomRisingLava"

jautils.createPlacementsPreserveOrder(customRisingLava, "normal", {
    { "intro", false },
    { "speed", -30 },
    { "coldColor1", "33ffe7", "color" },
    { "hotColor1", "ff8933", "color" },
    { "coldColor2", "4ca2eb", "color" },
    { "hotColor2", "f25e29", "color" },
    { "coldColor3", "0151d0", "color" },
    { "hotColor3", "d01c01", "color" },
    { "reverseCoreMode", false },
    { "doRubberbanding", true },
})

function customRisingLava.sprite(room, entity)
    local rcm = entity.reverseCoreMode
    local topColor, fillColor = rcm and jautils.getColor(entity.coldColor1) or jautils.getColor(entity.hotColor1), rcm and jautils.getColor(entity.coldColor2) or jautils.getColor(entity.hotColor2)
    local bubbleColor = rcm and jautils.getColor(entity.coldColor3) or jautils.getColor(entity.hotColor3)

    local top = drawableSpriteStruct.fromTexture("editor/FrostHelper/CustomRisingLavaTop", entity)
    top:setColor(topColor)

    local fill = drawableSpriteStruct.fromTexture("editor/FrostHelper/CustomRisingLavaFill", entity)
    fill:setColor(fillColor)

    local bubbles = drawableSpriteStruct.fromTexture("editor/FrostHelper/CustomRisingLavaBubbles", entity)
    bubbles:setColor(topColor)

    return {
        top, fill, bubbles, drawableSpriteStruct.fromTexture("editor/FrostHelper/CustomRisingLavaOutline", entity)
    }
end

function customRisingLava.selection(room, entity)
    return utils.rectangle(entity.x - 12, entity.y - 12, 24, 24)
end

return customRisingLava