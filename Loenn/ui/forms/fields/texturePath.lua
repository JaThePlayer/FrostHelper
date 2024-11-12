local state = require("loaded_state")
local stringField = require("ui.forms.fields.string")
local uiElements = require("ui.elements")
local utils = require("utils")
local languageRegistry = require("language_registry")
local loadedState = require("loaded_state")
local utils = require("utils")
local mods = require("mods")
local tasks = require("utils.tasks")
local languageRegistry = require("language_registry")
local jautils = mods.requireFromPlugin("libraries.jautils")

local integerField = {}

integerField.fieldType = "FrostHelper.texturePath"

-- Fine tuned search for exactly one mod folder
local function findModFolderFiletype(modFolderName, filenames, startFolder, fileType)
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

local allSprites = nil
local allSpritesByBaseDir = {}
local cache = {}

local function findSprites(pattern, baseFolder, captureConverter)
    if cache[pattern] then
        return cache[pattern]
    end

    local res = {}
    local added = {}

    local dir = "Graphics/Atlases/Gameplay/" .. baseFolder
    local filenames = allSpritesByBaseDir[dir] or findModFiletype(dir, "png")
    allSpritesByBaseDir[dir] = filenames

    for i, name in ipairs(filenames) do
        local idx = name:find("/")
        local nameNoExt, ext = utils.splitExtension(name:sub(idx + 1))
        if ext == "png" then
            local d, s = string.match(nameNoExt, pattern, #"Graphics/Atlases/Gameplay/" + 1)
            if d then
                local k = captureConverter(d, s)

                if not added[k] then
                    added[k] = true
                    local associatedMods = jautils.associatedModsFromSprite(string.sub(nameNoExt, #"Graphics/Atlases/Gameplay/" + 1))

                    local display = associatedMods[1] 
                        and (k .. " " .. mods.formatAssociatedMods(languageRegistry.getLanguage(), associatedMods))
                        or k

                    table.insert(res, { display, k })
                end
            end
        end
    end

    table.sort(res, function(a, b) return a[1]:lower() < b[1]:lower() end)
    cache[pattern] = res
    return res
end


local function valueTransformer(v)
    return v
end

local function displayTransformer(v)
    return v
end

function integerField.getElement(name, value, options)
    -- Add extra options and pass it onto string field
    local language = languageRegistry.getLanguage()

   -- options.valueTransformer = valueTransformer
  -- options.displayTransformer = displayTransformer

    options.options = findSprites(options.pattern, options.baseFolder or "", options.captureConverter or function(x) return x end)
    options.editable = true

    local formField = stringField.getElement(name, value, options)

    return formField
end

return integerField