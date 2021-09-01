module CustomFireBarrierFrostHelper

using ..Ahorn, Maple

@mapdef Entity "FrostHelper/CustomFireBarrier" CustomFireBarrier(x::Integer, y::Integer, width::Integer=Maple.defaultBlockWidth, height::Integer=Maple.defaultBlockHeight, isIce::Bool=false, surfaceColor::String="ff8933", edgeColor::String="f25e29", centerColor::String="d01c01", silent::Bool=false)

const placements = Ahorn.PlacementDict(
    "Custom Fire Barrier (Frost Helper)" => Ahorn.EntityPlacement(
        CustomFireBarrier,
        "rectangle"
    ),
)

Ahorn.minimumSize(entity::CustomFireBarrier) = 8, 8
Ahorn.resizable(entity::CustomFireBarrier) = true, true

Ahorn.selection(entity::CustomFireBarrier) = Ahorn.getEntityRectangle(entity)

edgeColor = (246, 98, 18, 255) ./ 255
centerColor = (209, 9, 1, 102) ./ 255

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::CustomFireBarrier, room::Maple.Room)
    x = Int(get(entity.data, "x", 0))
    y = Int(get(entity.data, "y", 0))

    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))

	edgeColor = Ahorn.argb32ToRGBATuple(parse(Int, get(entity.data, "edgeColor", "f25e29"), base=16))[1:3] ./ 255
	realEdgeColor = (edgeColor..., 1.0)
	
	centerColor = Ahorn.argb32ToRGBATuple(parse(Int, get(entity.data, "centerColor", "d01c01"), base=16))[1:3] ./ 255
	realCenterColor = (centerColor..., 1.0)
	
    Ahorn.drawRectangle(ctx, 0, 0, width, height, realCenterColor, realEdgeColor)
end

end