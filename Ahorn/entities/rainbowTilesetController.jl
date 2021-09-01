module FrostHelperRainbowTilesetController

using ..Ahorn, Maple

@mapdef Entity "FrostHelper/RainbowTilesetController" RainbowTilesetController(x::Integer, y::Integer, tilesets::String="3", bg::Bool=false)

const placements = Ahorn.PlacementDict(
	"Rainbow Tileset Controller (Frost Helper)" => Ahorn.EntityPlacement(
		RainbowTilesetController
	)
)

const sprite = "editor/FrostHelper/RainbowTilesetController"

function Ahorn.selection(entity::RainbowTilesetController)
	x, y = Ahorn.position(entity)

	return Ahorn.getSpriteRectangle(sprite, x, y)
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::RainbowTilesetController, room::Maple.Room)
	x, y = Ahorn.position(entity)

	Ahorn.drawSprite(ctx, sprite, x, y)
end

end