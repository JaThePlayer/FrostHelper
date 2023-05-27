local tagHelper = {}

local function _findAllTagsScan(into, styles)
    for _, style in ipairs(styles) do
        -- tags can be split by ','
        local tagStr = style.tag
        if tagStr then
            local tags = style.tag:split(',')()
            for _, tag in ipairs(tags) do
                into[tag] = true
            end
        end
    end
end

---Finds all tags used in FG and BG stylegrounds in the given map
---@param map table
---@return table<string>
function tagHelper.findAllTags(map)
    local ret = {}
    _findAllTagsScan(ret, map.stylesFg)
    _findAllTagsScan(ret, map.stylesBg)

    -- convert from a table<string, bool>, to a list<string>
    local list = {}
    for tag, _ in pairs(ret) do
        table.insert(list, tag)
    end

    return list
end

return tagHelper