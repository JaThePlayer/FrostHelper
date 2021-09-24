module FHIcicle

using ..Ahorn, Maple

@mapdef Entity "FrostHelper/Icicle" Icicle(x::Integer, y::Integer, directory::String="objects/FrostHelper/icicle/", speed::Number=15.0, breakSfx::String="event:/game/09_core/iceball_break")

const placements = Ahorn.PlacementDict(
    "Icicle (Frost Helper)" => Ahorn.EntityPlacement(
        Icicle
    )
)


function Ahorn.selection(entity::Icicle)
    x, y = Ahorn.position(entity)
    return Ahorn.getSpriteRectangle("$(get(entity.data, "directory", "objects/FrostHelper/icicle/"))idle00", x, y)
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::Icicle, room::Maple.Room) = Ahorn.drawSprite(ctx, "$(get(entity.data, "directory", "objects/FrostHelper/icicle/"))idle00", 0, 0)

end