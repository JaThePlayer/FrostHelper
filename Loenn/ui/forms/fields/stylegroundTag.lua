local tagHelper = require("mods").requireFromPlugin("libraries.stylegroundTagHelper")
local stringField = require("ui.forms.fields.string")
local loadedState = require("loaded_state")

local integerField = {}

integerField.fieldType = "FrostHelper.stylegroundTag"

function integerField.getElement(name, value, options)
    -- Add extra options and pass it onto the string field
    options.displayTransformer = tostring
    options.validator = function(v)
        return true
    end
    options.options = tagHelper.findAllTags(loadedState.map)

    local formField = stringField.getElement(name, value, options)

    return formField
end

return integerField