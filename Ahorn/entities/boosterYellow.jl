module FrostHelperYellowBoosterModule

using ..Ahorn, Maple

@mapdef Entity "FrostHelper/YellowBooster" FrostHelperYellowBoosterPlacement(x::Integer, y::Integer, boostTime::Number=0.3, respawnTime::Number=1.0, particleColor::String="Yellow", flashTint::String="Red", directory="objects/FrostHelper/yellowBooster/", reappearSfx::String="event:/game/04_cliffside/greenbooster_reappear", enterSfx::String="event:/game/04_cliffside/greenbooster_enter", boostSfx::String="event:/game/04_cliffside/greenbooster_dash", releaseSfx::String="event:/game/04_cliffside/greenbooster_end", dashes::Integer=-1, preserveSpeed::Bool=false)

const colors = sort(collect(keys(Ahorn.XNAColors.colors)))

const placements = Ahorn.PlacementDict(
	"Yellow Booster (Frost Helper)" => Ahorn.EntityPlacement(
		FrostHelperYellowBoosterPlacement
	)
)

Ahorn.editingOptions(entity::FrostHelperYellowBoosterPlacement) = Dict{String, Any}(
    "particleColor" => colors,
	"flashTint" => colors
)

sprite = "objects/booster/booster00"

function Ahorn.selection(entity::FrostHelperYellowBoosterPlacement)
    x, y = Ahorn.position(entity)
    return Ahorn.getSpriteRectangle(sprite, x, y)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::FrostHelperYellowBoosterPlacement, room::Maple.Room)
	texture = strip(get(entity.data, "directory", "objects/FrostHelper/yellowBooster/"), '/')
	realTexture = "$texture/booster00.png"
	
	Ahorn.drawSprite(ctx, realTexture, 0, 0)
end
	
end