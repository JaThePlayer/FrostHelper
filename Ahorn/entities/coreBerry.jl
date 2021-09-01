module FrostHelperCoreBerry

using ..Ahorn, Maple

@mapdef Entity "FrostHelper/CoreBerry" FTCoreBerry(x::Integer, y::Integer, order::Integer=-1, checkpointID::Integer=-1, isIce::Bool=false)

const placements = Ahorn.PlacementDict(
	"Core Berry (Frost Helper)" => Ahorn.EntityPlacement(
		FTCoreBerry
	)
)

function Ahorn.selection(entity::FTCoreBerry)
	x, y = Ahorn.position(entity)

	return Ahorn.getSpriteRectangle("collectables/strawberry/normal00", x, y)
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::FTCoreBerry, room::Maple.Room)
	x, y = Ahorn.position(entity)

	Ahorn.drawSprite(ctx, "collectables/strawberry/normal00", x, y)
end

end