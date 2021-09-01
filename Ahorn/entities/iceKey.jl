module IceKey

using ..Ahorn, Maple

@mapdef Entity "FrostHelper/KeyIce" KeyIce(x::Integer, y::Integer)

const placements = Ahorn.PlacementDict(
    "Ice Key (Frost Helper)" => Ahorn.EntityPlacement(
        KeyIce
    )
)

sprite = "collectables/FrostHelper/keyice/idle00.png"

function Ahorn.selection(entity::KeyIce)
    x, y = Ahorn.position(entity)
	return Ahorn.getSpriteRectangle("collectables/FrostHelper/keyice/idle00.png", x, y)
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::KeyIce, room::Maple.Room) = Ahorn.drawSprite(ctx, sprite, 0, 0)

end