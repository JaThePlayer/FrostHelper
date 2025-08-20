local stringField = require("ui.forms.fields.string")
local utils = require("utils")
local mods = require("mods")
local languageRegistry = require("language_registry")
local jautils = mods.requireFromPlugin("libraries.jautils")

local integerField = {}

integerField.fieldType = "FrostHelper.texturePath"

-- Fine tuned search for exactly one mod folder
local function findModFolderFiletype(modFolderName, filenames, startFolder, filetype)
    local path = utils.convertToUnixPath(utils.joinpath(
        string.format(mods.specificModContent, modFolderName),
        startFolder
    ))

    if filetype then
        return utils.getFilenames(path, true, filenames, function(filename)
            return utils.fileExtension(filename) == filetype
        end, false)

    else
        return utils.getFilenames(path, true, filenames, nil, false)
    end
end

-- Finds files relative to the root of every loaded mod
-- This is more performant than using the common mount point when looking for files recursively
local function findModFiletype(startFolder, filetype, folderNames)
    -- Fall back to all loaded mods
    folderNames = folderNames and table.flip(folderNames) or mods.loadedMods

    local filenames = {}

    for modFolderName, _ in pairs(folderNames) do
        findModFolderFiletype(modFolderName, filenames, startFolder, filetype)
    end

    return filenames
end

local allSpritesByBaseDir = {}
local cache = {}

local function createEntry(options, language, displayNameLangDir, nameNoExt, added, pattern, captureConverter, displayConverter, baseFolder)
    local d, s = string.match(nameNoExt, pattern)
    if not d then return end

    if options.filter and (not options.filter(d, s)) then
        return
    end

    local k = captureConverter(d, s)

    if added[k] then return end
    added[k] = true

    local associatedMods = jautils.associatedModsFromSprite(nameNoExt)

    local display = displayNameLangDir[k]
    local isTranslated = false

    if display._exists then
        display = tostring(display)
        isTranslated = true
    else
        local converted = displayConverter(d, s)
        if converted then
            display = converted
        else
            display = k
        end
    end

    if associatedMods[1] then
        display = display .. " " .. mods.formatAssociatedMods(language, associatedMods)
    end

    return { display, k, isTranslated }
end

local function findSprites(options, scan)
    local pattern = options.pattern
    local baseFolder = options.baseFolder or ""
    local captureConverter = options.captureConverter or function(x) return x end
    local displayConverter = options.displayConverter or function(...) return string.match(captureConverter(...), baseFolder .. "/(.*)") end
    local langDir = options.langDir or "_default"
    local vanillaSprites = options.vanillaSprites or {}

    if not scan and cache[pattern] then
        return cache[pattern]
    end

    local language = languageRegistry.getLanguage()
    local displayNameLangDir = language.FrostHelper.paths[langDir]

    local res = {}
    local added = {}
    for _, v in ipairs(vanillaSprites) do
        local entry = createEntry(options, language, displayNameLangDir, v, added, pattern, captureConverter, displayConverter, baseFolder)
        if entry then
            table.insert(res, entry)
        end
    end

    if scan then
        local atlas = (options.atlas or "Graphics/Atlases/Gameplay/")
        local targetExt = (options.extension or "png")
        local dir = atlas .. baseFolder
        local filenames = allSpritesByBaseDir[dir] or findModFiletype(dir, targetExt)
        allSpritesByBaseDir[dir] = filenames

        for i, name in ipairs(filenames) do
            local idx = name:find("/")
            local nameNoExt, ext = utils.splitExtension(name:sub(idx + 1))
            print(i, name, idx, nameNoExt, ext)
            if ext == targetExt then
                local entry = createEntry(options, language, displayNameLangDir, string.sub(nameNoExt, #atlas + 1), added, pattern, captureConverter, displayConverter, baseFolder)
                if entry then
                    table.insert(res, entry)
                end
            end
        end
    end

    table.sort(res, function(a, b)
        -- Show manually translated entries first - decide if this is a good idea...
        --[[
            if a[3] and not b[3] then
                return true
            end
            if not a[3] and b[3] then
                return false
            end
        ]]

        return a[1]:lower() < b[1]:lower()
    end)

    cache[pattern] = res
    return res
end

local uiElements = require("ui.elements")
local contextMenu = require("ui.context_menu")
local uiUtils = require("ui.utils")
local iconUtils = require("ui.utils.icons")
local grid = require("ui.widgets.grid")
local fieldDropdown = require("ui.widgets.field_dropdown")
local dropdowns = require("ui.widgets.dropdown")

local function shouldShowMenu(element, x, y, button)
    local menuButton = 1-- configs.editor.contextMenuButton
    local actionButton = 1--configs.editor.toolActionButton

    if button == menuButton or button == actionButton then
        return true
    end

    return false
end

local warningStyle = {
    normalBorder = {0.65, 0.5, 0.2, 0.9, 2.0},
    focusedBorder = {0.9, 0.67, 0.2, 1.0, 2.0}
}

local invalidStyle = {
    normalBorder = {0.65, 0.2, 0.2, 0.9, 2.0},
    focusedBorder = {0.9, 0.2, 0.2, 1.0, 2.0}
}

local function updateFieldStyle(formField, valid, warningValid)
    -- Make sure the textbox visual style matches the input validity
    local validVisuals = formField.validVisuals
    local warnVisuals = formField.warnVisuals
    local needsChange = validVisuals ~= valid or warnVisuals ~= warningValid

    if needsChange then
        if not valid then
            formField.field.style = invalidStyle

        elseif not warningValid then
            formField.field.style = warningStyle

        else
            formField.field.style = nil
        end

        formField.validVisuals = valid
        formField.warnVisuals = warningValid

        formField.field:repaint()
    end
end

local function dropdownChanged(formField, optionsFlattened)
    return function(element, newText)
        if not formField._ignoredFirstChange then
            formField._ignoredFirstChange = true
            return
        end

        local old = formField.currentValue
        local value
        local foundOption = false

        local editable = formField.editable
        local searchable = formField.searchable

        for _, option in ipairs(optionsFlattened) do
            if option[1] == newText then
                foundOption = true
                value = option[2]
            end
        end

        if foundOption and value ~= old then
            -- Manually handle for text field, dropdown handles itself
            -- Don't update if field is purely a dropdown
            if editable or searchable then
                formField.field:setText(newText)
                formField.field.index = #newText
            end

            formField.currentValue = value

            local valid = formField:fieldValid()
            local warningValid = formField:fieldWarning()

            updateFieldStyle(formField, valid, warningValid)
            formField:notifyFieldChanged()
        end
    end
end

local function generateDropdown(formField, options, scan)
    local minWidth = options.minWidth or options.width or 160
    local maxWidth = options.maxWidth or options.width or 160

    local sprites = findSprites(options, scan) -- list: { display, k, isTranslated }

    local optionStrings = {}

    local selectedIndex = -1

    local currentText = formField.field.text

    for i, value in ipairs(sprites) do
        table.insert(optionStrings, value[1])

        if (value[1] == currentText) or (value[2] == currentText) then
            selectedIndex = i
        end
    end

    local listOptions = {
        initialItem = selectedIndex,
    }

    local dropdown = dropdowns.fromList(dropdownChanged(formField, sprites), optionStrings, listOptions):with({
        minWidth = minWidth,
        maxWidth = maxWidth
    })

    return dropdown
end

function integerField.getElement(name, value, options)
    options.editable = true

    local formField = stringField.getElement(name, value, options)
    local field = formField.field

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
            onClick = function(orig, self, x, y, button)
                formField._ignoredFirstChange = false

                orig(self)

                local function buttonPressed(formField)
                    return function (element)
                        local dropdown = generateDropdown(formField, options, true)
                        formField._dropdownGroup.children = {dropdown}
                        formField._dropdownGroup:reflow()
                    end
                end

                local language = languageRegistry.getLanguage()
                local button = uiElements.button(tostring(language.ui.fh.fields.scan), buttonPressed(formField))
                button.tooltipText = tostring(language.ui.fh.fields.scan.tooltip)

                local dropdown = generateDropdown(formField, options, false)

                formField._dropdown = dropdown
                formField._dropdownGroup = uiElements.group({dropdown})
                formField._dropdownGroup.style.spacing = 0

                contextMenu.showContextMenu(function()
                    return grid.getGrid({ button, formField._dropdownGroup }, 1)
                end, {
                    shouldShowMenu = shouldShowMenu,
                    mode = "focused"
                })
            end
        })

        field:addChild(folderImage)
    end

    return formField
end

return integerField