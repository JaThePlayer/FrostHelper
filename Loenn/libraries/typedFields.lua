local fields = {}

local mods = require("mods")
local utils = require("utils")

-- Vanilla fields

---@class NumberFieldData
---@field minimumValue number?
---@field maximumValue number?
---@field options { [string]: number } | ({ [1]: string, [2]: number }[]) | nil Dropdown options for this field.

---Creates a number field.
---@param data NumberFieldData
---@return FieldInformationEntry
function fields.number(data)
    return {
        fieldType = "number",
        minimumValue = data.minimumValue,
        maximumValue = data.maximumValue,
        options = data.options
    }
end

---Creates a number field with minimumValue defaulting to 0.
---@param data NumberFieldData
---@return FieldInformationEntry
function fields.nonNegativeNumber(data)
    return {
        fieldType = "number",
        minimumValue = data.minimumValue or 0,
        maximumValue = data.maximumValue,
        options = data.options
    }
end

---Creates an integer field.
---@param data NumberFieldData
---@return FieldInformationEntry
function fields.integer(data)
    return {
        fieldType = "integer",
        minimumValue = data.minimumValue,
        maximumValue = data.maximumValue,
        options = data.options
    }
end

---Creates an integer field with minimumValue defaulting to 0.
---@param data NumberFieldData
---@return FieldInformationEntry
function fields.nonNegativeInteger(data)
    return {
        fieldType = "integer",
        minimumValue = data.minimumValue or 0,
        maximumValue = data.maximumValue,
        options = data.options
    }
end

---Creates an integer field with minimumValue defaulting to 1.
---@param data NumberFieldData
---@return FieldInformationEntry
function fields.positiveInteger(data)
    return {
        fieldType = "integer",
        minimumValue = data.minimumValue or 1,
        maximumValue = data.maximumValue,
        options = data.options
    }
end

---@class ColorFieldData
---@field allowXNAColors boolean? Whether XNA color names are allowed. JaUtils defaults to true, though Loenn defaults to false.
---@field useAlpha boolean? Whether alpha values are accepted. JaUtils defaults to true, though Loenn defaults to false.

---Creates a color field.
---@param data ColorFieldData
---@return FieldInformationEntry
function fields.color(data)
    if data.useAlpha == nil then
        data.useAlpha = true
    end

    if data.allowXNAColors == nil then
        data.allowXNAColors = true
    end

    return {
        fieldType = "color",
        allowXNAColors = data.allowXNAColors,
        useAlpha = data.useAlpha,
    }
end

---@class ListFieldData
---@field elementSeparator string The separator to use between elements.
---@field elementDefault string? The default text value for a newly created element.
---@field elementOptions FieldInformationEntry? The field information for the underlying field.
---@field minimumElements integer? Determines the minimum number of elements allowed for the list to be valid. Defaults to 0.
---@field maximumElements integer? Determines the maximum number of elements allowed for the list to be valid. Defaults to positive infinity.

---Creates a list field.
---@param data ListFieldData
---@return FieldInformationEntry
function fields.list(data)
    return {
        fieldType = "list",
        elementSeparator = data.elementSeparator,
        elementDefault = data.elementDefault,
        elementOptions = data.elementOptions,
        minimumElements = data.minimumElements,
        maximumElements = data.maximumElements,
    }
end

-- Frost Helper fields

---@class ComplexFieldData
---@field separator string The separator to use between elements.
---@field innerFields ComplexFieldDataInner[] The field information for inner elements, in order of their occurence.

---@class ComplexFieldDataInner
---@field name string The lang key of the field, displayed in UI.
---@field default any The default value of the field.
---@field info FieldInformationEntry? The field information for the underlying field.

---Creates a polymorphic complex field
---@param data ComplexFieldData
---@return FieldInformationEntry
function fields.complex(data)
    return {
        fieldType = "FrostHelper.complexField",
        separator = data.separator or error("Missing separator for complexField"),
        innerFields = data.innerFields or error("Missing innerFields for complexField"),
    }
end

---@class PolymorphicComplexFieldData
---@field separator string
---@field langPrefix string
---@field types PolymorphicComplexFieldInfoEntry[]

---Creates a polymorphic complex field
---@param data PolymorphicComplexFieldData
---@return FieldInformationEntry
function fields.polymorphicComplex(data)
    return {
        fieldType = "FrostHelper.polymorphicComplexField",
        separator = data.separator or error("Missing separator for polymorphicComplexField"),
        langPrefix = data.langPrefix or error("Missing langPrefix for polymorphicComplexField"),
        types = data.types or {}
    }
end

---@class FlagEnumFieldData
---@field innerFields FlagEnumFieldDataInner[] The field information for inner elements, in order of their occurence.

---@class FlagEnumFieldDataInner
---@field name string The lang key of the field, displayed in UI.
---@field value integer The int value of this enum field.

---Creates a polymorphic complex field
---@param data FlagEnumFieldData
---@return FieldInformationEntry
function fields.flagEnum(data)
    return {
        fieldType = "FrostHelper.flagEnum",
        innerFields = data.innerFields or error("Missing innerFields for flagEnum"),
    }
end

---Creates a Session Expression field.
---@param data {}
---@return FieldInformationEntry
function fields.sessionExpression(data)
    return {}
end

---@class TexturePathFieldData
---@field baseFolder string The base folder (relative to Graphics/Atlases/Gameplay) to search through.
---@field pattern string The Lua Pattern to use on the found asset paths to determine whether they should be considered.
---@field filter (fun(path: string): boolean)? Filter ran on each asset matching 'pattern'.
---@field captureConverter fun(...: string): string Function converting the path captured via the pattern into a value to be used as the field value if the user picks this path from the dropdown.
---@field displayConverter fun(...: string): string Converts the pattern's captures to a displayable name.
---@field langDir string The lang file prefix to check for translated path names.
---@field fallback string[]? If the current map editor does not support Path Fields, these paths will instead be used to populate the dropdown statically.
---@field vanillaSprites string[] Full path (including the baseFolder) of all vanilla sprites matching the pattern. Will be displayed as a fallback if the user does not wish to scan mod folders for all sprites.


local compat = mods.requireFromPlugin("libraries.compat")

---Creates a Texture Path field.
---@param data TexturePathFieldData
---@return FieldInformationEntry
function fields.texturePath(data)
    if not compat.inSnowberry then
        ---@type FieldInformationEntry
        local data = data
        data.fieldType = "FrostHelper.texturePath"
        return data
    end

    if data.fallback then
        return {
            options = data.fallback,
            editable = true,
        }
    end

    return {}
end

local spinnerDirectoryFieldData = fields.texturePath {
    baseFolder = "danger",
    pattern = "^(danger/.*)/fg(.-)%d+$",
    captureConverter = function(dir, subdir)
        local animationless = string.match(dir, "(.-)/%d%d$")
        if animationless then
            return animationless .. ">" .. subdir .. "!"
        end

        return dir .. ">" .. subdir
    end,
    displayConverter = function(dir, subdir)
        dir = string.match(dir, "(.-)/%d%d$") or dir

        local humanizedDir = utils.humanizeVariableName(string.match(dir, "^.*/(.*/hot)$") or string.match(dir, "^.*/(.*)$") or dir)
        if subdir and #subdir > 0 then
            return humanizedDir .. " (" .. utils.humanizeVariableName(subdir) .. ")"
        end

        return humanizedDir
    end,
    vanillaSprites = { "danger/crystal/fg_white00", "danger/crystal/fg_red00", "danger/crystal/fg_blue00", "danger/crystal/fg_purple00" },
    langDir = "customSpinner",
}

---Creates a field for a Custom Spinner's directory field.
---@param data {}
---@return FieldInformationEntry
function fields.spinnerDirectory(data)
    return spinnerDirectoryFieldData
end

--- Calc.ReadCSVIntWithTricks.
--- Read positive-integer CSV with some added tricks.
---  Use - to add inclusive range. Ex: 3-6 = 3,4,5,6.
---  Use * to add multiple values. Ex: 4*3 = 4,4,4.
---@param data {}
---@return FieldInformationEntry
function fields.csvWithTricks(data)
    return fields.list {
        elementSeparator = ',',
        -- TODO: validation
    }
end


return fields