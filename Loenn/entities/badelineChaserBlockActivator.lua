local nineSliceSolidEntity = require("mods").requireFromPlugin("libraries.nineSliceSolidEntity")

local solidTexturePath = "objects/FrostHelper/badelineChaserBlock/activator"
local nonsolidTexturePath = "objects/FrostHelper/badelineChaserBlock/activatorfield"
local nonsolidEmblemTexturePath = "objects/FrostHelper/badelineChaserBlock/emblemfield"
local solidEmblemTexturePath = "objects/FrostHelper/badelineChaserBlock/emblemsolid"

local badelineChaserBlockActivator = nineSliceSolidEntity.createHandler("FrostHelper/BadelineChaserBlockActivator", {
    {
        name = "badeline_chaser_block_activator",
        data = {
            width = 16,
            height = 16,
            solid = false,
        },
    },
    {
        name = "badeline_chaser_block_activator_reversed",
        data = {
            width = 16,
            height = 16,
            solid = true,
        },
    },
}, false, function (entity)
    return entity.solid and solidTexturePath or nonsolidTexturePath
end, function (entity)
    return entity.solid and solidEmblemTexturePath or nonsolidEmblemTexturePath
end)

return badelineChaserBlockActivator