module TemporaryKeyFrostHelper

using ..Ahorn, Maple

@mapdef Entity "FrostHelper/TemporaryKey" TemporaryKey(x::Integer, y::Integer, directory::String="collectables/FrostHelper/keytemp")

const placements = Ahorn.PlacementDict(
    "Temporary Key (Frost Helper)" => Ahorn.EntityPlacement(
        TemporaryKey
    )
)

sprite = "collectables/FrostHelper/keytemp/idle00.png"

function Ahorn.selection(entity::TemporaryKey)
    x, y = Ahorn.position(entity)
	return Ahorn.getSpriteRectangle("collectables/FrostHelper/keytemp/idle00.png", x, y)
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::TemporaryKey, room::Maple.Room) = Ahorn.drawSprite(ctx, sprite, 0, 0)

end