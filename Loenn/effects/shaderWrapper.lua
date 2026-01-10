local celesteEnums = require("consts.celeste_enums")
local colorgrades = celesteEnums.color_grades
---@module 'jautils'
local jautils = require("mods").requireFromPlugin("libraries.jautils")

local function createWrapper(name, data, canProvideShader)
    local defaultData = canProvideShader
    and {
        --wrappedTag = "",
        { "wrappedTag", "" },
        { "shader", "" },
        { "shaderFlag", "" },
        { "parameters", "", jautils.effectParametersFieldData },
    }
    or {
        { "wrappedTag", "" },
    }

    for idx, value in ipairs(data) do
        --defaultData[key] = value
        table.insert(defaultData, value)
    end

    local handler = {
        name = name,
    }

    jautils.createPlacementsPreserveOrder(handler, "default", defaultData)

    handler.defaultData = handler.placements[1].data

    return handler
    --[[
        return {
            name = name,
            defaultData = defaultData,
            fieldInformation = fieldInfo,
        }
        ]]
end

return {
    createWrapper("FrostHelper/ShaderWrapper", {}, true),
    createWrapper("FrostHelper/ShaderWrapperColorList", {
        { "colors", "" }
    }, true),
    createWrapper("FrostHelper/ColorgradeWrapper", {
        { "colorgrade", "none", "editableDropdown", colorgrades }
    }, false)
}