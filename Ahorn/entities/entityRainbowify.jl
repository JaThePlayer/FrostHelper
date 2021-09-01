module FrostHelperEntityRainbowifyController

using ..Ahorn, Maple

@mapdef Entity "FrostHelper/EntityRainbowifyController" EntityRainbowifyController(x::Integer, y::Integer, types::String="")

const placements = Ahorn.PlacementDict(
	"Entity Rainbowifier (Frost Helper)" => Ahorn.EntityPlacement(
		EntityRainbowifyController
	)
)

const sprite = "editor/FrostHelper/EntityRainbowifyController"

function Ahorn.selection(entity::EntityRainbowifyController)
	x, y = Ahorn.position(entity)

	return Ahorn.getSpriteRectangle(sprite, x, y)
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::EntityRainbowifyController, room::Maple.Room)
	x, y = Ahorn.position(entity)

	Ahorn.drawSprite(ctx, sprite, x, y)
end

end