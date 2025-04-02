local uiElements = require("ui.elements")
local contextMenu = require("ui.context_menu")
--local configs = require("configs")
local grid = require("ui.widgets.grid")
local languageRegistry = require("language_registry")
local uiUtils = require("ui.utils")
local iconUtils = require("ui.utils.icons")
local bit = require("bit")

local complexField = {}

complexField.fieldType = "FrostHelper.flagEnum"

complexField._MT = {}
complexField._MT.__index = {}

local invalidStyle = {
    normalBorder = { 0.65, 0.2, 0.2, 0.9, 2.0 },
    focusedBorder = { 0.9, 0.2, 0.2, 1.0, 2.0 }
}

local function listToString(values, sep)
    local addSep = false
    local s = ""
    for i, value in ipairs(values) do
        if addSep then
            s = s .. sep
        end
        addSep = true
        s = s .. tostring(value)
    end

    return s
end

function complexField._MT.__index:valuesToString(values)
    local sep = self.separator

    return listToString(values, sep)
end


function complexField._MT.__index:setValue(value, dontUpdateFields)
    value = tonumber(value) or 0
    if value ~= tonumber(self.currentValue) then
        self.currentValue = tostring(value)
        self.field:setText(self.currentValue)
    end

    if not dontUpdateFields then
        for i, checkbox in ipairs(self.checkboxes) do
            local text = bit.band(value, self.innerFields[i].value) ~= 0
            checkbox:setValue(text)
        end
    end
end

function complexField._MT.__index:getValue()
    return tonumber(self.currentValue) or 0
end

function complexField._MT.__index:fieldValid()
    local num = tonumber(self.field:getText())
    if num == nil then
        return false
    end
    return num >= 0 and num <= self.maxValue
end

local function shouldShowMenu(element, x, y, button)
    local menuButton = 1-- configs.editor.contextMenuButton
    local actionButton = 1--configs.editor.toolActionButton

    if button == menuButton or button == actionButton then
        return true
    end

    return false
end


local function fieldChanged(formField, col)
    return function(element, new, old)
        new = not not new

        local newValue = tonumber(formField.currentValue) or 0
        if newValue > formField.maxValue then
            newValue = bit.band(newValue, formField.maxValue)
        end

        newValue = bit.band(newValue, bit.bnot(formField.innerFields[col].value))
        newValue = bit.bor(newValue, ((new and 1 or 0) * formField.innerFields[col].value))

        if newValue ~= formField.currentValue then
            formField:setValue(newValue, true)

            formField:notifyFieldChanged()
        end
    end
end

local function overUpdateFieldStyle(formField, valid)
    local validVisuals = formField.overValidVisuals
    if validVisuals ~= valid then
        if not valid then
            formField.field.style = invalidStyle
        else
            formField.field.style = nil
        end
        formField.overValidVisuals = valid
        formField.field:repaint()
    end
end

local function overFieldChanged(formField)
    return function(element, new, old)
        if tonumber(new) then
            if old ~= new then
                formField:setValue(new or 0)
            end
        end


        local valid = formField:fieldValid()
        overUpdateFieldStyle(formField, valid)
        formField:notifyFieldChanged()
    end
end

local function getLangKey(language, key, default)
    local currKey = language
    for _, nextKey in ipairs(key:split(".")()) do
        currKey = currKey[nextKey]
    end

    if currKey._exists then
        return tostring(currKey)
    end

    return default
end

function complexField.getElement(name, value, options)
    value = tonumber(value) or 0

    local language = languageRegistry.getLanguage()

    local formField = {}
    formField = setmetatable(formField, complexField._MT)
    formField.innerFields = options.innerFields or {}
    formField.checkboxes = {}

    local valueTransformer = options.valueTransformer or function(v)
        return v
    end

    local minWidth = options.minWidth or options.width or 160
    local maxWidth = options.maxWidth or options.width or 160

    local label = uiElements.label(options.displayName or name)
    local field = uiElements.field(tostring(value), overFieldChanged(formField)):with({
        minWidth = minWidth,
        maxWidth = maxWidth
    })

    local gridContents = {}

    formField.maxValue = 0
    for i, fieldData in ipairs(formField.innerFields) do
        formField.maxValue = bit.bor(formField.maxValue, fieldData.value)
        local curValue = bit.band(value, fieldData.value) ~= 0

        local checkbox = uiElements.checkbox(getLangKey(language, fieldData.name, fieldData.name), curValue, fieldChanged(formField, i))
        checkbox.interactive = 1
        checkbox.tooltipText = getLangKey(language, fieldData.name .. ".tooltip", nil)

        table.insert(gridContents, checkbox)
        table.insert(formField.checkboxes, checkbox)
    end

    if field.height == -1 then
        field:layout()
    end

    local iconMaxSize = field.height - field.style.padding
    local parentHeight = field.height
    local folderIcon, iconSize = iconUtils.getIcon("list", iconMaxSize)

    if folderIcon then
        local centerOffset = math.floor((parentHeight - iconSize) / 2) + 1
        local folderImage = uiElements.image(folderIcon):with(uiUtils.rightbound(-1)):with(uiUtils.at(0, centerOffset))

        folderImage.interactive = 1
        folderImage:hook({
            onClick = function(orig, self)
                orig(self)

                contextMenu.showContextMenu(function()
                    return grid.getGrid(gridContents, 1)
                end, {
                    shouldShowMenu = shouldShowMenu,
                    mode = "focused"
                })
            end
        })

        field:addChild(folderImage)
    end

    if options.tooltipText then
        label.interactive = 1
        label.tooltipText = options.tooltipText
    end

    label.centerVertically = true

    formField.label = label
    formField.field = field
    formField.name = name
    formField.currentValue = value
    formField.valueTransformer = valueTransformer
    formField.validVisuals = { true, true, true }
    formField.overValidVisuals = true
    formField.width = 2
    formField.elements = {
        label, field
    }

    local valid = formField:fieldValid()
    overUpdateFieldStyle(formField, valid)

    return formField
end

return complexField
