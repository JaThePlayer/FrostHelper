module FrostHelperFallingBlockIgnoreSolids

using ..Ahorn, Maple

@mapdef Entity "FrostHelper/FallingBlockIgnoreSolids" FallingBlockIgnoreSolids(x::Integer, y::Integer, width::Integer=16, height::Integer=16, tiletype::String="3", climbFall::Bool=true, behind::Bool=false)

const placements = Ahorn.PlacementDict(
    "Falling Block (Ignore Solids, Frost Helper)" => Ahorn.EntityPlacement(
        FallingBlockIgnoreSolids,
        "rectangle",
        Dict{String, Any}(),
        Ahorn.tileEntityFinalizer
    ),
)

Ahorn.editingOptions(entity::FallingBlockIgnoreSolids) = Dict{String, Any}(
    "tiletype" => Ahorn.tiletypeEditingOptions()
)

Ahorn.minimumSize(entity::FallingBlockIgnoreSolids) = 8, 8
Ahorn.resizable(entity::FallingBlockIgnoreSolids) = true, true

Ahorn.selection(entity::FallingBlockIgnoreSolids) = Ahorn.getEntityRectangle(entity)

Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::FallingBlockIgnoreSolids, room::Maple.Room) = Ahorn.drawTileEntity(ctx, room, entity)

end