local stringField = require("ui.forms.fields.string")

local vanillaEasings = require("mods").requireFromPlugin("libraries.easings")
local easingDict = {}
for _, ease in ipairs(vanillaEasings) do
    easingDict[ease] = true
end

local easingField = {}

easingField.fieldType = "FrostHelper.easing"

function easingField.getElement(name, value, options)
    -- Add extra options and pass it onto the string field
    options.displayTransformer = tostring
    options.validator = function(v)
        if not v then
            return true
        end

        if easingDict[v] ~= nil then
            return easingDict[v]
        end

        -- Frost Helper allows using Lua functions for easings, we're in lua so we can check the syntax
        local code = string.format("return function(p)%s %s end", string.find(v, "return", 1, true) and "" or " return", v)

        local success, errorMsg = loadstring(code)
        if success then
            easingDict[v] = true
            return true
        else
            print(errorMsg)
            easingDict[v] = false
        end

        return false
    end
    options.options = vanillaEasings
    options.editable = true

    local formField = stringField.getElement(name, value, options)

    return formField
end

return easingField