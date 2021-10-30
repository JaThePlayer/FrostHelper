module FrostHelperDirectionalPuffer

using ..Ahorn, Maple

@mapdef Entity "FrostHelper/DirectionalPuffer" DirectionalPuffer(x::Integer, y::Integer, right::Bool=true, directory::String="objects/puffer/", explodeDirection::String="Right", dashRecovery::Integer=1)

const placements = Ahorn.PlacementDict(
    "Directional Puffer (FrostHelper, Right)" => Ahorn.EntityPlacement(
        DirectionalPuffer,
        "point",
        Dict{String, Any}(
            "right" => true,
            "explodeDirection" => "Right"
        )
    ),
    "Directional Puffer (FrostHelper, Left)" => Ahorn.EntityPlacement(
        DirectionalPuffer,
        "point",
        Dict{String, Any}(
            "right" => false,
            "explodeDirection" => "Left"
        )
    ),
    "Directional Puffer (FrostHelper, None)" => Ahorn.EntityPlacement(
        DirectionalPuffer,
        "point",
        Dict{String, Any}(
            "explodeDirection" => "None"
        )
    )
)

const explodeDirections = String[
    "Left",
    "Right",
    "Both",
    "None",
]

Ahorn.editingOptions(entity::DirectionalPuffer) = Dict{String, Any}(
    "explodeDirection" => explodeDirections
)

function getSprite(entity::DirectionalPuffer)
    return string(get(entity, "directory", "objects/puffer/"), "idle00")
end

function Ahorn.selection(entity::DirectionalPuffer)
    x, y = Ahorn.position(entity)
    scaleX = get(entity, "right", false) ? 1 : -1

    return Ahorn.getSpriteRectangle("objects/puffer/idle00", x, y, sx=scaleX)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::DirectionalPuffer, room::Maple.Room)
    scaleX = get(entity, "right", false) ? 1 : -1

    Ahorn.drawSprite(ctx, getSprite(entity), 0, 0, sx=scaleX)
end

function Ahorn.flipped(entity::DirectionalPuffer, horizontal::Bool)
    if horizontal
        entity.right = !entity.right

        return entity
    end
end

end