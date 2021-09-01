module FrostHelperTempLockblock

using ..Ahorn, Maple

@mapdef Entity "FrostHelper/TemporaryKeyDoor" TempLockBlock(x::Integer, y::Integer, sprite::String="wood", unlock_sfx::String="", stepMusicProgress::Bool=false)


sprites = Dict{String, String}(
    "wood" => "objects/door/lockdoor00",
    "temple_a" => "objects/door/lockdoorTempleA00",
    "temple_b" => "objects/door/lockdoorTempleB00",
    "moon" => "objects/door/moonDoor11"
)

const placements = Ahorn.PlacementDict(
    "Temporary Locked Door ($(Ahorn.humanizeVariableName(sprite)), Frost Helper)" => Ahorn.EntityPlacement(
        TempLockBlock,
        "point",
        Dict{String, Any}(
            "sprite" => sprite
        )
    ) for (sprite, texture) in sprites
)

Ahorn.editingOptions(entity::TempLockBlock) = Dict{String, Any}(
    "sprite" => sort(collect(keys(sprites)))
)

function Ahorn.selection(entity::TempLockBlock)
    x, y = Ahorn.position(entity)

    return Ahorn.Rectangle(x, y, 32, 32)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::TempLockBlock, room::Maple.Room)
    sprite = get(entity.data, "sprite", "wood")

    if haskey(sprites, sprite)
        Ahorn.drawSprite(ctx, sprites[sprite], 16, 16)
    end
end

end