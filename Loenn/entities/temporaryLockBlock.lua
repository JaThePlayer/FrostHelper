local drawableSpriteStruct = require("structs.drawable_sprite")

local temporaryLockBlock = {}

temporaryLockBlock.name = "FrostHelper/TemporaryKeyDoor"
temporaryLockBlock.depth = 8990

local sprites = {
    moon = "objects/door/moonDoor11",
    temple_b = "objects/door/lockdoorTempleB00",
    temple_a = "objects/door/lockdoorTempleA00",
    wood = "objects/door/lockdoor00",
}

temporaryLockBlock.placements = {}
for key, _ in pairs(sprites) do
    table.insert(temporaryLockBlock.placements, {
        name = key,
        data = {
            sprite = key,
            unlock_sfx = "",
            stepMusicProgress = false,
        }
    })
end

function temporaryLockBlock.sprite(room, entity)
    local path = sprites[entity.sprite] or sprites.wood
    return drawableSpriteStruct.fromTexture(path, entity)
end

return temporaryLockBlock