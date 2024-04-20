local mapScanHelper = require("mods").requireFromPlugin("libraries.mapScanHelper")
local stringField = require("ui.forms.fields.string")
local loadedState = require("loaded_state")

local integerField = {}

integerField.fieldType = "FrostHelper.cloudTag"

function integerField.getElement(name, value, options)
    if not value then
        value = ""
    end
    -- Add extra options and pass it onto the string field
    options.displayTransformer = tostring
    options.validator = function(v)
        return true
    end

    local room = loadedState.getSelectedRoom()
    if room then
        options.options = mapScanHelper.findAllCloudTagsInRoom(room)
    end

    local formField = stringField.getElement(name, value, options)

    return formField
end

return integerField