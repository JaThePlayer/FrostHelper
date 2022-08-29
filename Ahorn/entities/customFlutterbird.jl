module FrostHelperFlutterBird

using ..Ahorn, Maple

@mapdef Entity "FrostHelper/CustomFlutterBird" FHCustomFlutterbird(x::Integer, y::Integer, colors::String="89fbff,f0fc6c,f493ff,93baff", dontFlyAway::Bool=false,
                                                                   hopSfx::String="event:/game/general/birdbaby_hop", flyAwaySfx::String="event:/game/general/birdbaby_flyaway",
                                                                   tweetingSfx::String="event:/game/general/birdbaby_tweet_loop")

const placements = Ahorn.PlacementDict(
    "Custom Flutterbird (Frost Helper)" => Ahorn.EntityPlacement(
        FHCustomFlutterbird
    ),
)

const sprite = "scenery/flutterbird/idle00.png"

const colors = [
    (137, 251, 255, 255) ./ 255,
    (240, 252, 108, 255) ./ 255,
    (244, 147, 255, 255) ./ 255,
    (147, 186, 255, 255) ./ 255
]

function Ahorn.selection(entity::FHCustomFlutterbird)
    x, y = Ahorn.position(entity)

    return Ahorn.getSpriteRectangle(sprite, x, y, jx=0.5, jy=1.0)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::FHCustomFlutterbird, room::Maple.Room)
    rng = Ahorn.getSimpleEntityRng(entity)
    color = rand(rng, colors)

    Ahorn.drawSprite(ctx, sprite, 0, 0, jx=0.5, jy=1.0, tint=color)
end

end