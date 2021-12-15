local coreBerry = {}

coreBerry.name = "FrostHelper/CoreBerry"
coreBerry.depth = -100
coreBerry.placements = {
    name = "normal",
    data = {
        order = -1,
        checkpointID = -1,
        isIce = false,
    }
}

function coreBerry.texture(room, entity)
    return entity.isIce and "collectables/FrostHelper/CoreBerry/Cold/ColdBerry_Cold00" or "collectables/FrostHelper/CoreBerry/Hot/CoreBerry_Hot00"
end

return coreBerry