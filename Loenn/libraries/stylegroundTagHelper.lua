local mapScanHelper = require("mods").requireFromPlugin("libraries.mapScanHelper")

local tagHelper = {}

---Finds all tags used in FG and BG stylegrounds in the given map
---@param map table
---@return table<string>
function tagHelper.findAllTags(map)
    return mapScanHelper.findAllStylegroundTagsInMap(map)
end

return tagHelper