local attachGroupHelper = {}

function attachGroupHelper.findNewGroup(room)
    local ids = {}

    for _, target in ipairs(room.entities) do
        if (target.attachGroup) then
            ids[target.attachGroup] = true
        end
    end

    for id = 0, math.huge do
        if not ids[id] then
            return id
        end
    end

    return -1
end

return attachGroupHelper