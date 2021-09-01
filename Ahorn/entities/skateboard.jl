module boardofskates

using ..Ahorn, Maple

@mapdef Entity "FrostHelper/Skateboard" Skateboard(x::Integer, y::Integer, direction::String="Right", speed::Number=90.0, keepMoving::Bool=false, sprite::String="objects/FrostHelper/skateboard")

const placements = Ahorn.PlacementDict(
    "Skateboard (Frost Helper)" => Ahorn.EntityPlacement(
        Skateboard
    )
)

const dirs = Dict{String, String}(
    "Left" => "Left",
    "Right" => "Right",
	"Old" => "Old"
)

Ahorn.editingOptions(entity::Skateboard) = Dict{String, Any}(
    "direction" => dirs,
	"keepMoving" => Dict{String, Any}(
        "Yes" => true,
        "No" => false
    )
)

sprite = "objects/FrostHelper/skateboard.png"

function Ahorn.selection(entity::Skateboard)
    x, y = Ahorn.position(entity)
	return Ahorn.getSpriteRectangle("objects/FrostHelper/skateboard.png", x, y)
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::Skateboard, room::Maple.Room) = Ahorn.drawSprite(ctx, sprite, 0, 0)

end