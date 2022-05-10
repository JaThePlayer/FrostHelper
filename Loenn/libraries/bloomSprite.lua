local drawableSprite = require("structs.drawable_sprite")
local gradientPath = "util/bloomgradient"

local bloomSprite = {}

function bloomSprite.getSprite(position, alpha, radius)
    local gradientSprite = drawableSprite.fromTexture(gradientPath, position)
    alpha = math.max(math.min(alpha / 2, 0.6), 0.33)
    local scale = radius * 2 * (1 / gradientSprite.meta.width)

    gradientSprite:setColor({alpha, alpha, alpha, alpha})
    gradientSprite:setScale(scale, scale)

    return gradientSprite
end

return bloomSprite