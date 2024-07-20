local jautils = require("mods").requireFromPlugin("libraries.jautils")
local drawableSpriteStruct = require("structs.drawable_sprite")
local utils = require("utils")

local texture = "scenery/flutterbird/idle00"
local flutterbird = {}

flutterbird.name = "FrostHelper/CustomFlutterBird"
flutterbird.depth = -9999

jautils.createPlacementsPreserveOrder(flutterbird, "normal", {
    { "colors", "89fbff,f0fc6c,f493ff,93baff", "colorList" },
    { "directory", "scenery/flutterbird/" },
    { "hopSfx", "event:/game/general/birdbaby_hop" },
    { "flyAwaySfx", "event:/game/general/birdbaby_flyaway" },
    { "tweetingSfx", "event:/game/general/birdbaby_tweet_loop" },
    { "dontFlyAway", false },
})

function flutterbird.sprite(room, entity)
    utils.setSimpleCoordinateSeed(entity.x, entity.y)
    local colors = jautils.getColors(entity.colors)
    local colorIndex = math.random(1, #colors)

    local flutterbirdSprite = drawableSpriteStruct.fromTexture(texture, entity)

    flutterbirdSprite:setJustification(0.5, 1.0)
    flutterbirdSprite:setColor(colors[colorIndex])

    return flutterbirdSprite
end

return flutterbird