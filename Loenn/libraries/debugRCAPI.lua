-- provides friendly wrappers over Frost Helper's DebugRC API
local utils = require("utils")
local jautils = require("mods").requireFromPlugin("libraries.jautils")
local frostSettings = require("mods").requireFromPlugin("libraries.settings")

local hasRequest, request = false, nil
if jautils.inLonn then
    hasRequest, request = utils.tryrequire("lib.luajit-request.luajit-request")
end

local port = 32270

local debugRC = {
    available = hasRequest
}

function debugRC.getUrl(endpoint)
    return string.format("http://localhost:%s/frostHelper/%s", port, endpoint)
end

local _entityTypesToNamesUrl = debugRC.getUrl("csharpTypesToEntityID")


local _entityTypesToNamesCache = {}
local _entityTypesToNamesKeyIndexedCache = {}

local function splitList(list)
    local t = {}
    if string.find(list, ",") then
        for w in string.gmatch(list, "([^,]+)") do
            table.insert(t, w)
        end
    else
        table.insert(t, list)
    end

    return t
end

---cached
---@param entityTypes string
---@return table
function debugRC.entityTypesToNames(entityTypes)
    local typ = type(entityTypes)
    if typ == "string" then
        local cached = _entityTypesToNamesCache[entityTypes]
        if cached then
            return cached
        end

        local response =
            frostSettings.useDebugRC() and request.send(_entityTypesToNamesUrl, {
                headers = {
                    types = entityTypes,
                },
            })

        local names = response and response.body or entityTypes

        local t = splitList(names)

        _entityTypesToNamesCache[entityTypes] = t

        return t
    end
end

function debugRC.entityTypesToNamesKeyIndexed(entityTypes)
    local cached = _entityTypesToNamesKeyIndexedCache[entityTypes]
    if cached then
        return cached
    end

    local t = {}
    local types = debugRC.entityTypesToNames(entityTypes)
    for _, value in ipairs(types) do
        t[value] = true
    end

    _entityTypesToNamesKeyIndexedCache[entityTypes] = t
    return t
end

return debugRC