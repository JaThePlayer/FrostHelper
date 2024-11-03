local utils = require("utils")
local drawableSpriteStruct = require("structs.drawable_sprite")
local drawableRectangle = require("structs.drawable_rectangle")
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

jautils.addPlacement(customFireBarrier, "ice", {
    { "isIce", true },
    { "surfaceColor", "a6fff4" },
    { "edgeColor", "6cd6eb" },
    { "centerColor", "4ca8d6" },
})

local function selectionFunc(room, entity)
    return utils.rectangle(entity.x, entity.y, entity.width, entity.height)
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

    utils.setSimpleCoordinateSeed(entity.x, entity.y)
    local bubbleAmt = entity.width * entity.height * 0.005 * (entity.bubbleAmountMultiplier or 1)
    if bubbleAmt >= 1 then
        for i = 0, bubbleAmt, 1 do
            particleData.x, particleData.y = entity.x + math.random(3, math.max(3,entity.width - 7)), entity.y + math.random(3, math.max(3, entity.height - 7))
            local particle = drawableSpriteStruct.fromTexture(particlePath, particleData)
    
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

rainbowFireBarrier.selection = selectionFunc
rainbowFireBarrier.sprite = spriteFunc

return {
    customFireBarrier,
    rainbowFireBarrier
}