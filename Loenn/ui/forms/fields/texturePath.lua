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

local function findSprites(options)
    local pattern = options.pattern
    local baseFolder = options.baseFolder or ""
    local captureConverter = options.captureConverter or function(x) return x end
    local displayConverter = options.displayConverter or function(...) return string.match(captureConverter(...), baseFolder .. "/(.*)") end
    local langDir = options.langDir or "_default"
    local vanillaSprites = options.vanillaSprites or {}

    if cache[pattern] then
        --return cache[pattern]
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

    local dir = "Graphics/Atlases/Gameplay/" .. baseFolder
    local filenames = allSpritesByBaseDir[dir] or findModFiletype(dir, "png")
    allSpritesByBaseDir[dir] = filenames

    for i, name in ipairs(filenames) do
        local idx = name:find("/")
        local nameNoExt, ext = utils.splitExtension(name:sub(idx + 1))
        if ext == "png" then
            local entry = createEntry(options, language, displayNameLangDir, string.sub(nameNoExt, #"Graphics/Atlases/Gameplay/" + 1), added, pattern, captureConverter, displayConverter, baseFolder)
            if entry then
                table.insert(res, entry)
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

function integerField.getElement(name, value, options)
    options.options = findSprites(options)
    options.editable = true

    local formField = stringField.getElement(name, value, options)

    return formField
end

return integerField