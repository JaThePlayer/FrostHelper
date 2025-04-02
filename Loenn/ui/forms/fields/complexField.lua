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

complexField.fieldType = "FrostHelper.complexField"

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
            --print(index, text, value.field)
            if value.field then
                value.field:setText(text)
               -- value.field.index = 0-- #value.field:getText()
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
    for _, value in ipairs(self.uiForms) do
        if not value:fieldValid() then
            return false
        end
    end

    return true
end

function complexField._MT.__index:validateIdx(v)
    --return type(v) == "number" and v >= self.minValue and v <= self.maxValue
    return true
end

function complexField._MT.__index:fieldsValid()
    local t = {}
    local values = self:splitValue(self.currentValue)

    for i = 1, #self.innerFields, 1 do
        table.insert(t, self:validateIdx(values[i]))
    end

    return t
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
        new = string.gsub(new or "", formField.separator, "") or ""

        local values = formField:splitValue(formField.currentValue)

        values[col] = new

        local newValue = formField:valuesToString(values)
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

function complexField.getElement(name, value, options)
    value = tostring(value)

    local language = languageRegistry.getLanguage()

    local formField = {}
    formField = setmetatable(formField, complexField._MT)
    formField.separator = options.separator or ","
    formField.innerFields = options.innerFields or {}
    formField.uiForms = {}

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

    local gridContents = {}


    for i, fieldData in ipairs(formField.innerFields) do
        local curValue = valueSplit[i]
        if not curValue or curValue == "" then
            curValue = fieldData.default or ""
        end

        if fieldData.info and fieldTypeToStringToValue[fieldData.info.fieldType or "string"] then
            curValue = fieldTypeToStringToValue[fieldData.info.fieldType or "string"](curValue, fieldData.default)
        end

        local el = forms.getFieldElement(getLangKey(language, fieldData.name, fieldData.name), curValue, fieldData.info or {})
        local tooltipText = getLangKey(language, fieldData.name .. ".tooltip", nil)
        local onChanged = fieldChanged(formField, i)

        el.notifyFieldChanged = function()
            onChanged(el, el.field and el.field:getText() or el:getValue())
        end

        for _, elElement in ipairs(el.elements) do
            if tooltipText then
                elElement.tooltipText = tooltipText
                elElement.interactive = 1
            end


            table.insert(gridContents, elElement)
        end

        table.insert(formField.uiForms, el)
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

                --openFileDialog(textfield, options)
                contextMenu.showContextMenu(function()
                    return grid.getGrid(gridContents, 2)
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
