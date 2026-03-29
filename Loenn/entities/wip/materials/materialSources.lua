---@module 'jautils'
local jautils = require("mods").requireFromPlugin("libraries.jautils")

if not jautils.devMode then
    return
end

local sources = {}

---@param name string
---@param placementData JaUtilsPlacementData<UnknownEntity>
---@return EntityHandler<UnknownEntity>
local function createSource(name, placementData)
    ---@type EntityHandler<UnknownEntity>
    local handler = {
        name = name,
        depth = 0,
        texture = "editor/FrostHelper/RainbowTilesetController",
    }

    table.insert(placementData, 1, { "name", "", jautils.fields.materialName {} })

    jautils.createPlacementsPreserveOrder(handler, "default", placementData)

    return handler
end


local solidColorSource = createSource("FrostHelper/Materials/SolidColor", {
    { "color", "ffffff", jautils.fields.color { } }
})
table.insert(sources, solidColorSource)

local gradientSource = createSource("FrostHelper/Materials/Gradient", {
    { "gradient", "ffffff,000000,50;000000,ffffff,50", jautils.fields.gradient { } },
    { "direction", "Horizontal", jautils.fields.gradientDirection {} },
    { "gradientWidth",  320, jautils.fields.positiveInteger {} },
    { "gradientHeight", 180, jautils.fields.positiveInteger {} },
})
table.insert(sources, gradientSource)

local blendSource = createSource("FrostHelper/Materials/Blend", {
    { "toBlend", "", jautils.fields.list {
        elementSeparator = ",",
        elementOptions = jautils.fields.materialName {}
    }},
})
table.insert(sources, blendSource)

return sources