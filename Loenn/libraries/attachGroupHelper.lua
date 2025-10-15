local attachGroupHelper = {}

function attachGroupHelper.findAllGroups(room)
    local ids = {}

    for _, target in ipairs(room.entities) do
        if (target.attachGroup) then
            ids[target.attachGroup] = true
        end
    end

    return ids
end

function attachGroupHelper.findAllGroupsAsList(room)
    if not room then
        return {}
    end

    local ids = attachGroupHelper.findAllGroups(room)
    local list = {}
    for key, value in pairs(ids) do
        table.insert(list, key)
    end

    return list
end

function attachGroupHelper.findNewGroup(room)
    local ids = attachGroupHelper.findAllGroups(room)

    for id = 0, math.huge do
        if not ids[id] then
            return id
        end
    end

    return -1
end

return attachGroupHelper