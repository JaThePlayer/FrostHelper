local drawableSpriteStruct = require("structs.drawable_sprite")
---@module "jautils"
local jautils = require("mods").requireFromPlugin("libraries.jautils")
local utils = require("utils")

local springDepth = -8501
local springTexture = "objects/spring/00"

---@class CustomSpring : Entity
---@field outlineColor string?
---@field renderOutline boolean

local rotations = {
    [0] = "FrostHelper/SpringFloor",
    [1] = "FrostHelper/SpringLeft",
    [2] = "FrostHelper/SpringCeiling",
    [3] = "FrostHelper/SpringRight",
}

local function getSprite(entity)
    local sprite = drawableSpriteStruct.fromTexture((entity.directory .. "00") or springTexture, entity)

    if not sprite then
        sprite = drawableSpriteStruct.fromTexture(springTexture, entity)
    end

    return sprite
end

local dashAndStaminaRecoveryOptions = {
    fieldType = "integer",
    options = {
        ["10000 (Refill)"] = 10000,
        ["10001 (No Change)"] = 10001,
    },
    maximumValue = 10001,
}

local function createSpringHandler(name, spriteRotation, speedAsVector)
    ---@type EntityHandler<CustomSpring>
    local handler = {
        name = name,

        depth = springDepth,
        sprite = function (room, entity)
            local sprite = getSprite(entity)

            sprite:setJustification(0.5, 1.0)
            sprite.rotation = spriteRotation
            local renderOutline = entity.renderOutline == nil and true or entity.renderOutline
            if renderOutline then
                local sprites = jautils.getBorder(sprite, entity.outlineColor)
                table.insert(sprites, sprite)
                return sprites
            end

            return sprite
        end,
        selection = function(room, entity)
            local sprite = drawableSpriteStruct.fromTexture("objects/spring/00", entity)
            sprite:setJustification(0.5, 1.0)
            sprite.rotation = spriteRotation
            return sprite:getRectangle()
        end,
        rotate = jautils.getNameRotationHandler(rotations),
        flip = jautils.getNameFlipHandler(rotations),
        --ignoredFields = { "_name", "_id", "version" },
    }

    jautils.createPlacementsPreserveOrder(handler, "normal", {
        { "color", "ffffff", "color" },
        { "directory", "objects/spring/", "FrostHelper.texturePath", {
            baseFolder = "objects",
            pattern = "^(objects/.*/)00$",
            filter = function(dir) return not not drawableSpriteStruct.fromTexture(dir .. "white", {}) end,
            captureConverter = function(dir)
                return dir
            end,
            displayConverter = function(dir)
                return utils.humanizeVariableName(string.match(dir, "^.*/(.*)/$") or dir)
            end,
            vanillaSprites = { "objects/spring/00", "objects/FrostHelper/whiteSpring/00" },
            langDir = "customSpring",
            fallback = {
                "objects/spring/",
                "objects/FrostHelper/whiteSpring/",
            }
        }},
        { "speedMult", speedAsVector and "1.0" or 1.0 },
        { "attachGroup", -1, "FrostHelper.attachGroup" },
        -- legacy options, replaced with 'recovery'
        { "dashRecovery", 10000, dashAndStaminaRecoveryOptions, nil, {
            hideIf = function (entity) return entity.recovery ~= nil end },
            doNotAddToPlacement = true,
        },
        { "staminaRecovery", 10000, dashAndStaminaRecoveryOptions, nil, {
            hideIf = function (entity) return entity.recovery ~= nil end },
            doNotAddToPlacement = true,
        },
        { "jumpRecovery", 10001, dashAndStaminaRecoveryOptions, nil, {
            hideIf = function (entity) return entity.recovery ~= nil end },
            doNotAddToPlacement = true,
        },
        { "recovery", "10000;10000;10001", "statRecovery", nil, { hideIfMissing = true } },
        { "sfx", "event:/game/general/spring" },
        { "version", 3, "integer", nil, { hidden = true } },
        { "oneUse", false },
        { "playerCanUse", true },
        { "renderOutline", true },
        -- { "alwaysActivate", false }, --todo - puffers, holdables (if thats even possible???)
    })

    return handler
end

local springUp = createSpringHandler("FrostHelper/SpringFloor", 0, false)
local springRight = createSpringHandler("FrostHelper/SpringRight", -math.pi / 2, true)
local springCeiling = createSpringHandler("FrostHelper/SpringCeiling", math.pi, false)
local springLeft = createSpringHandler("FrostHelper/SpringLeft", math.pi / 2, true)

return {
    springUp,
    springRight,
    springLeft,
    springCeiling
}