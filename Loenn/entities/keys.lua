local function setNodeLimits(handler)
    handler.nodeLimits = {0, 2}
    handler.nodeLineRenderType = "line"
end

local iceKey = {}
iceKey.name = "FrostHelper/KeyIce"
iceKey.placements =
{
    name = "normal",
    data = {
        onCarryFlag = "",
        persistent = false,
    }
}

iceKey.texture = "collectables/FrostHelper/keyice/idle00"

local temporaryKey = {}
temporaryKey.name = "FrostHelper/TemporaryKey"
temporaryKey.placements =
{
    name = "normal",
    data = {
        directory = "collectables/FrostHelper/keytemp",
        emitParticles = true,
    }
}

function temporaryKey.texture(room, entity)
    return (entity.directory or "collectables/FrostHelper/keytemp") .. "/idle00"
end

setNodeLimits(temporaryKey)
setNodeLimits(iceKey)

return {
    iceKey,
    temporaryKey
}