module PufferIndicatorFrostHelper

using ..Ahorn, Maple

@mapdef Entity "FrostHelper/PufferIndicator" PufferIndicator(x::Integer, y::Integer, spritePath::String="objects/FrostHelper/pufferIndicator", color::String="ffffff", outlineColor::String="000000")

const placements = Ahorn.PlacementDict(
	"Puffer Indicator (Frost Helper)" => Ahorn.EntityPlacement(
		PufferIndicator
	)
)

function Ahorn.selection(entity::PufferIndicator)
    x, y = Ahorn.position(entity)

	return Ahorn.getSpriteRectangle(get(entity.data, "spritePath", "objects/FrostHelper/pufferIndicator"), x, y)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::PufferIndicator, room::Maple.Room)
	texture = get(entity.data, "spritePath", "objects/FrostHelper/pufferIndicator")

	rawTint = Ahorn.argb32ToRGBATuple(parse(Int, lstrip(get(entity.data, "color", "ffffff"), [ '#' ]), base=16))[1:3] ./ 255
	realTint = (rawTint..., 1.0)
    Ahorn.drawSprite(ctx, texture, 0, 0, tint = realTint)
end

end