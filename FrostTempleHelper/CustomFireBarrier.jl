module FrostHelperCustomFireBarrierModule

using ..Ahorn, Maple

@mapdef Entity "FrostHelper/CustomFireBarrier" FrostHelperCustomFireBarrierPlacement(x::Integer, y::Integer, width::Integer=16, height::Integer=16, surfaceColor::String="ffffff", edgeColor::String="ffffff", centerColor::String="ffffff", isIce::Bool=false)

const placements = Ahorn.PlacementDict(
	"Custom Fire Barrier (Frost Helper)" => Ahorn.EntityPlacement(
		FrostHelperCustomFireBarrierPlacement,
		"rectangle"
	)
)

Ahorn.minimumSize(entity::FrostHelperCustomFireBarrierPlacement) = 8, 8
Ahorn.resizable(entity::FrostHelperCustomFireBarrierPlacement) = true, true

Ahorn.selection(entity::FrostHelperCustomFireBarrierPlacement) = Ahorn.getEntityRectangle(entity)

edgeColor = (30, 30, 30, 255) ./ 255
centerColor = (60, 60, 60, 102) ./ 255

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::FrostHelperCustomFireBarrierPlacement, room::Maple.Room)
    x = Int(get(entity.data, "x", 0))
    y = Int(get(entity.data, "y", 0))
    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))

    Ahorn.drawRectangle(ctx, 0, 0, width, height, centerColor, edgeColor)
end

end