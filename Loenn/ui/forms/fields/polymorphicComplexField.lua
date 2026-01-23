---@class PolymorphicComplexFieldInfoEntry : FieldInformationEntry
---@field name string The name of this class type, used as a prefix, needs to be unique among all possible classes in this polymorphic field.
---@field default any Default value of the underlying field, used if none is present.
---@field defaultValue string Default text used when setting an entry to this class. Should be of format 'prefix:default'.
---@field info FieldInformationEntry Field information used for the underlying field.


local uiElements = require("ui.elements")
local contextMenu = require("ui.context_menu")
--local configs = require("configs")
local grid = require("ui.widgets.grid")
local languageRegistry = require("language_registry")
local forms = require("ui.forms.form")
local utils = require("utils")
local uiUtils = require("ui.utils")
local iconUtils = require("ui.utils.icons")

local complexField = {}

complexField.fieldType = "FrostHelper.polymorphicComplexField"

complexField._MT = {}
complexField._MT.__index = {}

local invalidStyle = {
    normalBorder = { 0.65, 0.2, 0.2, 0.9, 2.0 },
    focusedBorder = { 0.9, 0.2, 0.2, 1.0, 2.0 }
}

function complexField._MT.__index:splitValue(value)
    local sep = self.separator

    return value:split(sep)()
end

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
    value = value or ""

    local values = self:splitValue(value)
    self.currentValue = value

    if not dontUpdateFields then
        for index, value in ipairs(self.uiForms) do
            local text = values[index] or ""
            if value.field then
                value.field:setText(text)
            else
                value:setValue(text)
            end
        end
    end


    self.field:setText(self.currentValue)
end

function complexField._MT.__index:getValue()
    return self.currentValue
end

function complexField._MT.__index:fieldValid()
    if not self._validate then
        return true
    end

    return self._validate()
end

local function shouldShowMenu(element, x, y, button)
    local menuButton = 1-- configs.editor.contextMenuButton
    local actionButton = 1--configs.editor.toolActionButton

    if button == menuButton or button == actionButton then
        return true
    end

    return false
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
        if old ~= new then
            formField:setValue(new or "")
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

local fieldTypeToStringToValue = {
    ["number"] = function(v, def)
        return tonumber(v) or def or 0
    end,
    ["integer"] = function(v, def)
        return tonumber(v) or def or 0
    end,
}

local fieldDropdown = require("ui.widgets.field_dropdown")
local dropdowns = require("ui.widgets.dropdown")

local function createGrid(formField)
    local language = languageRegistry.getLanguage()

    local valueSplit = formField:splitValue(formField.currentValue)
    local curValue = valueSplit[2] or ""

    for i, fieldData in ipairs(formField.types) do
        if valueSplit[1] == fieldData.name then
            local fieldType = fieldData.info.fieldType or "string"

            if fieldData.info and fieldTypeToStringToValue[fieldType] then
                curValue = fieldTypeToStringToValue[fieldType](curValue, fieldData.default)
            end

            local el = forms.getFieldElement(getLangKey(language, formField.langPrefix .. "." .. fieldData.name, fieldData.name), curValue, fieldData.info or {})
            local tooltipText = getLangKey(language, formField.langPrefix .. "." .. fieldData.name .. ".tooltip", nil)

            local origNotify = el.notifyFieldChanged
            el.notifyFieldChanged = function(...)
                local new = el.field and el.field:getText() or el:getValue()
                new = string.gsub(new or "", formField.separator, "") or ""

                local newValue = fieldData.name .. formField.separator .. new
                if newValue ~= formField.currentValue then
                    formField:setValue(newValue, true)

                    formField:notifyFieldChanged()
                end

                if origNotify then
                    origNotify(...)
                end
            end

            formField._validate = function()
                return el:fieldValid()
            end

            if fieldType == "FrostHelper.complexField" then
                return el:getGrid()
            end

            local gridContents = {}
            for _, elElement in ipairs(el.elements) do
                if tooltipText then
                    elElement.tooltipText = tooltipText
                    elElement.interactive = 1
                end

                table.insert(gridContents, elElement)
            end

            return grid.getGrid(gridContents, 2)
        end
    end

    return grid.getGrid({}, 2)
end

local function dropdownChanged(formField)
    return function(element, newText)
        if not formField._ignoredFirstChange then
            formField._ignoredFirstChange = true
            return
        end

        local foundOption = false

        local editable = formField.editable
        local searchable = formField.searchable
        local currentType = formField:splitValue(formField.currentValue)[1]

        for _, option in ipairs(formField.types) do
            if option.name == newText or option.displayName == newText then
                foundOption = option
            end
        end

        if foundOption and foundOption.name ~= currentType then
            --formField.currentValue = foundOption.defaultValue --value

            --local valid = formField:fieldValid()
            --local warningValid = formField:fieldWarning()
            --updateFieldStyle(formField, valid, warningValid)

            formField:setValue(foundOption.defaultValue, true)
            formField:notifyFieldChanged()

            formField.gridGroup.children = { createGrid(formField) }
            formField.gridGroup:reflow()
        end
    end
end

local function generateTypeDropdown(formField, options)
    local minWidth = options.minWidth or options.width or 160
    local maxWidth = options.maxWidth or options.width or 160
    local language = languageRegistry.getLanguage()

    local optionStrings = {}

    local selectedIndex = -1

    local currentType = formField:splitValue(formField.currentValue)[1]

    local types = formField.types

    for i, value in ipairs(types) do
        if not value.name then
            error("One of the types in a polymorphicComplexField does not have a 'name' field!")
        end

        value.displayName = getLangKey(language, formField.langPrefix .. "." .. value.name .. ".dropdown", value.name)

        table.insert(optionStrings, value.displayName)

        if value.name == currentType then
            selectedIndex = i
        end
    end

    local listOptions = {
        initialItem = selectedIndex,
    }

    local dropdown = dropdowns.fromList(dropdownChanged(formField), optionStrings, listOptions):with({
        minWidth = minWidth,
        maxWidth = maxWidth
    })

    listOptions.parentProxy = dropdown
    listOptions.spawnParent = dropdown

    return dropdown
end

function complexField.getElement(name, value, options)
    value = tostring(value)

    local language = languageRegistry.getLanguage()

    local formField = {}
    formField = setmetatable(formField, complexField._MT)
    formField.separator = options.separator or ","
    formField.types = options.types or {}
    formField.uiForms = {}
    formField.langPrefix = options.langPrefix or ""

    local valueSplit = formField:splitValue(value)

    local valueTransformer = options.valueTransformer or function(v)
        return v
    end

    local minWidth = options.minWidth or options.width or 160
    local maxWidth = options.maxWidth or options.width or 160
    formField.minValue = options.minValue or -math.huge
    formField.maxValue = options.maxValue or math.huge

    local label = uiElements.label(options.displayName or name)
    local field = uiElements.field(value, overFieldChanged(formField)):with({
        minWidth = minWidth,
        maxWidth = maxWidth
    })


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

                local typeDropdown = generateTypeDropdown(formField, options)
                formField.gridGroup = uiElements.group({ createGrid(formField) })

                contextMenu.showContextMenu(function()
                    return grid.getGrid({ typeDropdown, formField.gridGroup }, 1)
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

    createGrid(formField)
    local valid = formField:fieldValid()
    overUpdateFieldStyle(formField, valid)

    return formField
end

return complexField
