module StaticDoorFrostHelper

using ..Ahorn, Maple

@mapdef Entity "FrostHelper/StaticDoor" StaticDoor(x::Integer, y::Integer, width::Integer=16, height::Integer=16, 
                                                                       type::String="wood", openSfx::String="", closeSfx::String="",
                                                                       lightOccludeAlpha::Number=1.0)

const textures = ["wood", "metal"]
const placements = Ahorn.PlacementDict(
    "Static Door ($(uppercasefirst(texture))) (Frost Helper)" => Ahorn.EntityPlacement(
        StaticDoor,
        "point",
        Dict{String, Any}(
            "type" => texture
        )
    ) for texture in textures
)

function doorSprite(entity::StaticDoor)
    variant = get(entity.data, "type", "wood")

    return variant == "wood" ? "objects/door/door00.png" : "objects/door/metaldoor00.png"
end

Ahorn.editingOptions(entity::StaticDoor) = Dict{String, Any}(
    "type" => textures
)

function Ahorn.selection(entity::StaticDoor)
    x, y = Ahorn.position(entity)
    sprite = doorSprite(entity)

    return Ahorn.getSpriteRectangle(sprite, x, y, jx=0.5, jy=1.0)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::StaticDoor, room::Maple.Room)
    sprite = doorSprite(entity)
    Ahorn.drawSprite(ctx, sprite, 0, 0, jx=0.5, jy=1.0)
end

end