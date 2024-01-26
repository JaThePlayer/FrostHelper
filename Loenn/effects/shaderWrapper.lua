local celesteEnums = require("consts.celeste_enums")
local colorgrades = celesteEnums.color_grades

local function createWrapper(name, data, fieldInfo, canProvideShader)
    local defaultData = canProvideShader
    and {
        wrappedTag = "",
        shader = "",
        shaderFlag = "",
    }
    or {
        wrappedTag = "",
    }

    for key, value in pairs(data) do
        defaultData[key] = value
    end

    return {
        name = name,
        defaultData = defaultData,
        fieldInformation = fieldInfo,
    }
end

return {
    createWrapper("FrostHelper/ShaderWrapper", {}, {}, true),
    createWrapper("FrostHelper/ShaderWrapperColorList", {
        colors = ""
    }, {}, true),
    createWrapper("FrostHelper/ColorgradeWrapper", {
        colorgrade = "none",
    }, {
        colorgrade = {
            options = colorgrades,
            editable = true,
        }
    }, false)
}