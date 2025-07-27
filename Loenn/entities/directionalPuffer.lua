local utils = require("utils")
---@module "jautils"
local jautils = require("mods").requireFromPlugin("libraries.jautils")

---@class Entity
---@field recovery string?

local explodeIndicatorStartAngle, explodeIndicatorEndAngle = -jautils.degreeToRadians(6), jautils.degreeToRadians(186)

local explodeDirectionsEnum = {
    "Left", "Right", "Both", "None"
}

---@class DirectionalPuffer : Entity
---@field explodeDirection? string
---@field directory? string
---@field color? string
---@field right? boolean
---@field explosionRangeIndicatorColor? string

local builtinSprites = {
    "objects/puffer/",
    "objects/FrostHelper/spikyPuffer/"
}

---@type EntityHandler<DirectionalPuffer>
local directionalPuffer = {
    name = "FrostHelper/DirectionalPuffer",
    depth = 0,
}

jautils.createPlacementsPreserveOrder(directionalPuffer, "none", {
    { "explodeDirection", "None", explodeDirectionsEnum },
    { "directory", "objects/puffer/", "editableDropdown", builtinSprites },
    { "color", "ffffff", "color" },
    { "eyeColor", "000000", "color" },
    { "explosionRangeIndicatorColor", "ffffff", "color" },
    -- Legacy option, replaced with 'recovery'
    { "dashRecovery", 1, "integer", nil, {
        hideIf = function (entity) return entity.recovery ~= nil end },
        doNotAddToPlacement = true,
    },
    { "recovery", "10000;10000;10001", "statRecovery", nil, { hideIfMissing = true } },
    { "respawnTime", 2.5 },
    { "static", false },
    { "right", false },
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

jautils.addPlacement(directionalPuffer, "spiky", {
    { "killOnJump", true },
    { "directory", "objects/FrostHelper/spikyPuffer/" },
    { "explodeDirection", "Both" },
    { "eyeColor", "c6845e" },
})

function directionalPuffer.flip(room, entity, horizontal, vertical)
    if vertical then
        return false
    end

    entity.right = not entity.right
    if entity.explodeDirection == "Left" then
        entity.explodeDirection = "Right"
        entity.right = true
    elseif entity.explodeDirection == "Right" then
        entity.explodeDirection = "Left"
        entity.right = false
    end

    return true
end

function directionalPuffer.sprite(room, entity)
    local sprites = jautils.getOutlinedSpriteFromPath(entity, (entity.directory or "objects/puffer/") .. "idle00", entity.color, nil, entity.right and 1 or -1)

    local explodeDir = entity.explodeDirection
    if explodeDir and explodeDir ~= "None" then
        local startIndex = explodeDir == "Left" and 14 or 0
        local endIndex = explodeDir == "Right" and 14 or 28

        for i = startIndex, endIndex, 1 do
            local indicatorColor = jautils.getColor(entity.explosionRangeIndicatorColor) or jautils.colorWhite
            local angle = jautils.map(i / 28, 0., 1., explodeIndicatorStartAngle, explodeIndicatorEndAngle)
            local angleVecX, angleVecY = jautils.angleToVector(angle, 1.)
            local offsetX, offsetY = angleVecX * 32, angleVecY * 32

            offsetY += 3 -- align the lines more correctly

            if i == 0 or i == 28 then
                -- draw lines at the edges
                local offset2X = angleVecX * 22
                table.insert(sprites, jautils.getLineSprite(entity.x + offsetX, entity.y + offsetY,
                                                            entity.x + offset2X, entity.y + offsetY,
                                                            indicatorColor))
            else
                table.insert(sprites, jautils.getPixelSprite(entity.x + offsetX, entity.y + offsetY, indicatorColor))
            end

        end
    end

    return sprites
end

function directionalPuffer.selection(room, entity)
    return utils.rectangle(entity.x - 9, entity.y - 5, 16, 12)
end

return directionalPuffer