module FrostHelperEntityBatcherController

using ..Ahorn, Maple

@mapdef Entity "FrostHelper/EntityBatcher" EntityBatcher(x::Integer, y::Integer, types::String="", effect::String="", depth::Integer=0, parameters::String="", dynamicDepthBatchSplitField::String="", flag::String="", flagInverted::Bool=false)

const placements = Ahorn.PlacementDict(
	"Entity Batcher (Frost Helper + Shader Helper)" => Ahorn.EntityPlacement(
		EntityBatcher
	)
)

const sprite = "editor/FrostHelper/RainbowTilesetController"

function Ahorn.selection(entity::EntityBatcher)
	x, y = Ahorn.position(entity)

	return Ahorn.getSpriteRectangle(sprite, x, y)
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::EntityBatcher, room::Maple.Room)
	x, y = Ahorn.position(entity)

	Ahorn.drawSprite(ctx, sprite, x, y)
end

end