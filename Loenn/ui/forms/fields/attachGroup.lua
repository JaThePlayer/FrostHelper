local attachGroupHelper = require("mods").requireFromPlugin("libraries.attachGroupHelper")
local state = require("loaded_state")
local stringField = require("ui.forms.fields.string")
local uiElements = require("ui.elements")
local utils = require("utils")
local languageRegistry = require("language_registry")
local loadedState = require("loaded_state")

local integerField = {}

integerField.fieldType = "FrostHelper.attachGroup"

local function buttonPressed(formField)
    return function (element)
        formField.field.text = tostring(attachGroupHelper.findNewGroup(state.getSelectedRoom()))
        formField.field.index = #formField.field.text

        formField:notifyFieldChanged()
    end
end

local function fieldCallback(self, value, prev)
    local text = self.text or ""
    local font = self.label.style.font
    local button = self.button

    -- should just be button.width, but that isn't correct initially :(
    local offset = -font:getWidth(button.text) - (2 * button.style.padding)

    self.button.x = -font:getWidth(text) + self.minWidth + offset - 40
end

local function valueTransformer(v)
    if v then
        return tonumber(v)
    end

    return -1
end

local function displayTransformer(v)
    if v then
        return tostring(v)
    end

    return "-1"
end

function integerField.getElement(name, value, options)
    -- Add extra options and pass it onto string field
    local language = languageRegistry.getLanguage()
    local minimumValue = options.minimumValue or -math.huge
    local maximumValue = options.maximumValue or math.huge

    options.valueTransformer = valueTransformer
    options.displayTransformer = displayTransformer
    options.validator = function(v)
        if not v then
            v = -1
        end

        local number = tonumber(v)

        return utils.isInteger(number) and number >= minimumValue and number <= maximumValue
    end
    options.options = attachGroupHelper.findAllGroupsAsList(loadedState.getSelectedRoom())

    local formField = stringField.getElement(name, value, options)

    local button = uiElements.button(tostring(language.ui.fh.attachGroup.name), buttonPressed(formField))

    button.style.padding = 0
    button.style.spacing = 0
    button.tooltipText = tostring(language.ui.fh.attachGroup.tooltip)
    formField.field:addChild(button)
    formField.field.button = button

    local orig = formField.field.cb
    formField.field.cb = function (...)
        orig(...)
        fieldCallback(...)
    end

    --formField.formFieldChanged = fieldChangedCallback

    fieldCallback(formField.field, formField.field.text, "")
    --formField.field.cb(formField.field, formField.field.text, "")

    return formField
end

return integerField