module PlusOneRefillFrostHelper

using ..Ahorn, Maple

@mapdef Entity "FrostHelper/PlusOneRefill" PlusOneRefill(x::Integer, y::Integer, oneUse::Bool=false, directory::String="objects/FrostHelper/plusOneRefill", dashCount::Integer=1, respawnTime::Number=2.5, particleColor::String="White", recoverStamina::Bool=true)

const placements = Ahorn.PlacementDict(
    "Plus One Refill (Frost Helper)" => Ahorn.EntityPlacement(
        PlusOneRefill
    )
)

const colors = sort(collect(keys(Ahorn.XNAColors.colors)))

Ahorn.editingOptions(entity::PlusOneRefill) = Dict{String, Any}(
    "particleColor" => colors
)

function Ahorn.selection(entity::PlusOneRefill)
    x, y = Ahorn.position(entity)
	texture = get(entity.data, "directory", "objects/FrostHelper/plusOneRefill")
	realTexture = "$texture/idle00.png"
    return Ahorn.getSpriteRectangle(realTexture, x, y)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::PlusOneRefill, room::Maple.Room) 
	texture = get(entity.data, "directory", "objects/FrostHelper/plusOneRefill")
	realTexture = "$texture/idle00.png"
	Ahorn.drawSprite(ctx, realTexture, 0, 0)
end

end