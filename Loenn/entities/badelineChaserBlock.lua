local nineSliceSolidEntity = require("mods").requireFromPlugin("libraries.nineSliceSolidEntity")

local solidTexturePath = "objects/FrostHelper/badelineChaserBlock/solid"
local pressedTexturePath = "objects/FrostHelper/badelineChaserBlock/pressed"
local emblemTexturePath = "objects/FrostHelper/badelineChaserBlock/emblemsolid"
local pressedEmblemTexturePath = "objects/FrostHelper/badelineChaserBlock/emblempressed"

local badelineChaserBlock = nineSliceSolidEntity.createHandler("FrostHelper/BadelineChaserBlock", {
    {
        name = "badeline_chaser_block",
        data = {
            width = 16,
            height = 16,
            reversed = false,
        },
    },
    {
        name = "badeline_chaser_block_reversed",
        data = {
            width = 16,
            height = 16,
            reversed = true,
        },
    },
}, false, function (entity)
    return entity.reversed and pressedTexturePath or solidTexturePath
end, function (entity)
    return entity.reversed and pressedEmblemTexturePath or emblemTexturePath
end)

return badelineChaserBlock