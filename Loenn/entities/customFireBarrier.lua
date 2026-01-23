local utils = require("utils")
local drawableSpriteStruct = require("structs.drawable_sprite")
local drawableRectangle = require("structs.drawable_rectangle")
---@module 'jautils'
local jautils = require("mods").requireFromPlugin("libraries.jautils")

local particlePath = "particles/bubble"

local onlyModes = {
    "All",
    "OnlyTop",
    "OnlyBottom"
}

local customFireBarrier = {}

customFireBarrier.name = "FrostHelper/CustomFireBarrier"
customFireBarrier.depth = -8500

jautils.createPlacementsPreserveOrder(customFireBarrier, "normal", {
    { "width", 16 },
    { "height", 16 },
    { "surfaceColor", "ff8933", "color" },
    { "edgeColor", "f25e29", "color" },
    { "centerColor", "d01c01", "color" },
    { "waves", "2,0.25,4.0;1,0.05000000074505806,0.5", jautils.fields.list {
        elementSeparator = ";",
        elementDefault = "2,0.25,4.0",
        elementOptions = jautils.fields.complex {
            separator = ",",
            innerFields = {
                {
                    name = "FrostHelper.fields.customFireBarrier.amplitude",
                    default = 2,
                    info = jautils.fields.number { }
                },
                {
                    name = "FrostHelper.fields.customFireBarrier.waveNumber",
                    default = 0.25,
                    info = jautils.fields.number { }
                },
                {
                    name = "FrostHelper.fields.customFireBarrier.frequency",
                    default = 4.0,
                    info = jautils.fields.number { }
                },
                {
                    name = "FrostHelper.fields.customFireBarrier.phase",
                    default = 0,
                    info = jautils.fields.number { }
                },
            }
        },
    } },
    { "curveAmplitude", 1 },
    { "bubbleAmountMultiplier", "1|particles/bubble|danger/lava/bubble_a", jautils.fields.complex {
        separator = "|",
        innerFields = {
            {
                name = "FrostHelper.fields.customFireBarrier.bubble.amountMultiplier",
                default = 1,
                info = jautils.fields.number { }
            },
            {
                name = "FrostHelper.fields.customFireBarrier.bubble.path",
                default = "particles/bubble",
                info = jautils.fields.list {
                    elementSeparator = ";",
                }
            },
            {
                name = "FrostHelper.fields.customFireBarrier.bubble.surfaceAnimations",
                default = "danger/lava/bubble_a",
                info = jautils.fields.list {
                    elementSeparator = ";",
                }
            },
        }
    } },
    { "surfaces", "All", onlyModes },
    { "silentFlag", "" },
    { "fade", 16 },
    { "rainbow", 0, jautils.fields.flagEnum {
        innerFields = {
            {
                name = "FrostHelper.fields.customFireBarrier.rainbow.surface",
                value = 1,
            },
            {
                name = "FrostHelper.fields.customFireBarrier.rainbow.edge",
                value = 2,
            },
            {
                name = "FrostHelper.fields.customFireBarrier.rainbow.bubble",
                value = 4,
            },
        }
    }},
    { "silent", false },
    { "isIce", false },
    { "ignoreCoreMode", false },
    { "canCollide", true },
    { "hasSolid", true },
})

jautils.addPlacement(customFireBarrier, "ice", {
    { "isIce", true },
    { "surfaceColor", "a6fff4" },
    { "edgeColor", "6cd6eb" },
    { "centerColor", "4ca8d6" },
    { "waves", "1,0.25,4.0;1,0.05,0.5" }
})

local function selectionFunc(room, entity)
    return utils.rectangle(entity.x, entity.y, entity.width, entity.height)
end

local defaultBubblePaths = { "particles/bubble" }

local function getBubbleConfig(entity)
    local str = entity.bubbleAmountMultiplier
    if not str then
        return 1, defaultBubblePaths
    end

    if type(str) == "number" then
        return str, defaultBubblePaths
    end

    str = tostring(str)
    local mult, path = str:match("^([^|]*)|?([^|]*)")
    if path == "" then
        path = nil
    end

    return tonumber(mult) or 1, path and path:split(";")() or defaultBubblePaths
end

local function spriteFunc(room, entity)
    local rectangle = utils.rectangle(entity.x, entity.y, entity.width, entity.height)

    local sprites = { drawableRectangle.fromRectangle("fill", rectangle, jautils.getColor(entity.centerColor or "d01c01")):getDrawableSprite() }
    for _, value in ipairs(drawableRectangle.fromRectangle("line", rectangle, entity.surfaceColor or "ff8933"):getDrawableSprite()) do
        table.insert(sprites, value)
    end

    local particleData = {
        color = jautils.getColor(entity.surfaceColor or "ff8933"),
        x = 0,
        y = 0,
    }

    local bubbleMult, bubbleTextures = getBubbleConfig(entity)

    utils.setSimpleCoordinateSeed(entity.x, entity.y)
    local bubbleAmt = entity.width * entity.height * 0.005 * (bubbleMult)
    if bubbleAmt >= 1 then
        for i = 0, bubbleAmt, 1 do
            particleData.x, particleData.y = entity.x + math.random(3, math.max(3,entity.width - 7)), entity.y + math.random(3, math.max(3, entity.height - 7))
            local texture = bubbleTextures[math.random(0, #bubbleTextures)]
            local particle = drawableSpriteStruct.fromTexture(texture, particleData)

            table.insert(sprites, particle)
        end
    end


    return sprites
end

customFireBarrier.selection = selectionFunc
customFireBarrier.sprite = spriteFunc

local rainbowFireBarrier = {}

rainbowFireBarrier.name = "FrostHelper/RainbowFireBarrier"
rainbowFireBarrier.depth = -8500

--[[
These are deprecated now
jautils.createPlacementsPreserveOrder(rainbowFireBarrier, "default", {
    { "width", 16 },
    { "height", 16 },
    { "centerColor", "d01c01", "color" },
    { "smallWaveAmplitude", 2 },
    { "bigWaveAmplitude", 1 },
    { "curveAmplitude", 1 },
    { "bubbleAmountMultiplier", 1 },
    { "surfaces", "All", onlyModes },
    { "silentFlag", "" },
    { "silent", false },
    { "isIce", false },
    { "ignoreCoreMode", false },
    { "canCollide", true },
})
]]

rainbowFireBarrier.selection = selectionFunc
rainbowFireBarrier.sprite = spriteFunc


return {
    customFireBarrier,
    rainbowFireBarrier
}