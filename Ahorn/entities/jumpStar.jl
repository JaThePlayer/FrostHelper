module FrostHelperTheAbyssJumpStar

using ..Ahorn, Maple

@mapdef Entity "FrostHelper/JumpStar" JumpStar(x::Integer, y::Integer, mode::String="Jump", directory::String="theAbyssJumpStar", strength::Integer=1)

const modes = String[
    "Jump",
    "Dash"
]

Ahorn.editingOptions(entity::JumpStar) = Dict{String, Any}(
	"mode" => modes
)

const placements = Ahorn.PlacementDict(
    "Jump Star (Frost Helper)" => Ahorn.EntityPlacement(
        JumpStar
    )
)

function Ahorn.selection(entity::JumpStar)
    x, y = Ahorn.position(entity)

    return Ahorn.getSpriteRectangle("theAbyssJumpStar/Jump/0star00", x, y)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::JumpStar, room::Maple.Room)
    directory = get(entity.data, "directory", "theAbyssJumpStar")
    mode = get(entity.data, "mode", "Jump")
    strength = string(get(entity.data, "strength", 0))
    
    Ahorn.drawSprite(ctx, "$directory/$mode/$(strength)star00", 0, 0)
end

end