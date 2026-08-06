---@module 'jautils'
local jautils = require("mods").requireFromPlugin("libraries.jautils")
local drawableSpriteStruct = require("structs.drawable_sprite")
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

    channel.texture = "editor/FrostHelper/EntityRainbowifyController"

    table.insert(channels, channel)
    return channel
end

--    return Calc.HsvToColor((float) (0.4 + Calc.YoYo((position.Length() + this.Scene.TimeActive * 50f) % 280 / 280) * 0.4), 0.4f, 0.9f);

addChannelType("FrostHelper/HsvRainbowChannel", {
    { "hue", "0.4 + $yoyo(($pos.len + $time * 50) % 280 / 280) * 0.4" },
    { "s", "0.4", jautils.fields.sessionExpression {} },
    { "v", "0.9", jautils.fields.sessionExpression {} }
})

return channels