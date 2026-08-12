---@module 'jautils'
local jautils = require("mods").requireFromPlugin("libraries.jautils")
local drawableSpriteStruct = require("structs.drawable_sprite")
local drawableTextStruct = require("structs.drawable_text")
local utils = require("utils")

local channels = {}

---comment
---@param name string
---@param placementData JaUtilsPlacementData
---@return EntityHandler<UnknownEntity>
local function addChannelType(name, placementData)
    ---@type EntityHandler<UnknownEntity>
    local channel = {
        name = name,
    }

    table.insert(placementData, 1, { "channelId", "" })

    jautils.createPlacementsPreserveOrder(channel, "default", placementData)

    function channel.sprite(room, entity)
        local baseSprite = drawableSpriteStruct.fromTexture("editor/FrostHelper/EntityRainbowifyController", entity)
        local textSprite = drawableTextStruct.fromText(entity.channelId or "", entity.x - 12, entity.y - 20, 24, 24, nil, 0.25, jautils.getColor("ffffff"))

        return {
            baseSprite,
            textSprite
        }
    end

    table.insert(channels, channel)
    return channel
end

addChannelType("FrostHelper/HsvRainbowChannel", {
    { "hue", "0.4 + $yoyo(($pos.len + $time * 50) % 280 / 280) * 0.4" },
    { "s", "0.4", jautils.fields.sessionExpression {} },
    { "v", "0.9", jautils.fields.sessionExpression {} }
})

---@type EntityHandler<UnknownEntity>
local channelAttacher = {
    name = "FrostHelper/RainbowChannelAttacher",
    texture = "editor/FrostHelper/RainbowChannelAttacher"
}

jautils.createPlacementsPreserveOrder(channelAttacher, "default", {
    { "channelId", "" },
    { "types", "", jautils.fields.typeList { } },
})

table.insert(channels, channelAttacher)

return channels
