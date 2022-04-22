local utils = require("utils")
local jautils = require("mods").requireFromPlugin("libraries.jautils")

local explodeIndicatorStartAngle, explodeIndicatorEndAngle = -jautils.degreeToRadians(6), jautils.degreeToRadians(186)

local explodeDirectionsEnum = {
    "Left", "Right", "Both", "None"
}

local builtinSprites = {
    "objects/puffer/",
    "objects/FrostHelper/spikyPuffer/"
}

local directionalPuffer = {
    name = "FrostHelper/DirectionalPuffer",
    depth = 0,
}

jautils.createPlacementsPreserveOrder(directionalPuffer, "none", {
    { "explodeDirection", "None", explodeDirectionsEnum },
    { "directory", "objects/puffer/", "editableDropdown", builtinSprites },
    { "color", "ffffff", "color" },
    { "dashRecovery", 1, "integer" },
    { "static", false },
    { "right", false },
    { "respawnTime", 2.5 },
    { "noRespawn", false },
    { "killOnJump", false },
    { "killOnLaunch", false },
})

jautils.addPlacement(directionalPuffer, "right", {
    { "right", true },
    { "explodeDirection", "Right" },
})

jautils.addPlacement(directionalPuffer, "left", {
    { "right", false },
    { "explodeDirection", "Left" },
})

function directionalPuffer.sprite(room, entity)
    local sprites = jautils.getOutlinedSpriteFromPath(entity, (entity.directory or "objects/puffer/") .. "idle00", entity.color, nil, entity.right and 1 or -1)

    local explodeDir = entity.explodeDirection
    if explodeDir and explodeDir ~= "None" then
        local startIndex = explodeDir == "Left" and 14 or 0
        local endIndex = explodeDir == "Right" and 14 or 28

        for i = startIndex, endIndex, 1 do
            local angle = jautils.map(i / 28, 0., 1., explodeIndicatorStartAngle, explodeIndicatorEndAngle)
            local angleVecX, angleVecY = jautils.angleToVector(angle, 1.)
            local offsetX, offsetY = angleVecX * 32, angleVecY * 32

            offsetY += 3 -- align the lines more correctly

            if i == 0 or i == 28 then
                -- draw lines at the edges
                local offset2X, offset2Y = angleVecX * 22, angleVecY * 22
                table.insert(sprites, jautils.getLineSprite(entity.x + offsetX, entity.y + offsetY,
                                                            entity.x + offset2X, entity.y + offsetY))
            else
                table.insert(sprites, jautils.getPixelSprite(entity.x + offsetX, entity.y + offsetY))
            end

        end
    end

    return sprites
end

function directionalPuffer.selection(room, entity)
    return utils.rectangle(entity.x - 9, entity.y - 5, 16, 12)
end

return directionalPuffer