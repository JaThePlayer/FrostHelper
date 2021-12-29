local attachGroupHelper = require("mods").requireFromPlugin("libraries.attachGroupHelper")
local state = require("loaded_state")
local stringField = require("ui.forms.fields.string")
local uiElements = require("ui.elements")
local utils = require("utils")
local languageRegistry = require("language_registry")

local integerField = {}

integerField.fieldType = "FrostHelper.attachGroup"

local function buttonPressed(formField)
    return function (element)
        formField.field.text = tostring(attachGroupHelper.findNewGroup(state.getSelectedRoom()))
        formField.field.index = #formField.field.text
    end
end

local function fieldCallback(self, value, prev)
    local text = self.text or ""
    local font = self.label.style.font
    local button = self.button

    -- should just be button.width, but that isn't correct initially :(
    local offset = -font:getWidth(button.text) - (2 * button.style.padding)

    self.button.x = -font:getWidth(text) + self.minWidth + offset
end

function integerField.getElement(name, value, options)
    -- Add extra options and pass it onto string field
    local language = languageRegistry.getLanguage()
    local minimumValue = options.minimumValue or -math.huge
    local maximumValue = options.maximumValue or math.huge

    options.valueTransformer = tonumber
    options.displayTransformer = tostring
    options.validator = function(v)
        local number = tonumber(v)

        return utils.isInteger(number) and number >= minimumValue and number <= maximumValue
    end

    local formField = stringField.getElement(name, value, options)

    local button = uiElements.button(tostring(language.ui.fh.attachGroup.name), buttonPressed(formField))

    button.style.padding *= 0.36
    button.style.spacing = 0
    button.tooltipText = tostring(language.ui.fh.attachGroup.tooltip)
    formField.field:addChild(button)
    formField.field.button = button

    local orig = formField.field.cb
    formField.field.cb = function (...)
        orig(...)
        fieldCallback(...)
    end

    formField.field.cb(formField.field, formField.field.text, "")

    return formField
end

return integerField