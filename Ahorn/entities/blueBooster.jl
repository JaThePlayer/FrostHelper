module FrostHelperBlueBoosterModule

using ..Ahorn, Maple

@mapdef Entity "FrostHelper/BlueBooster" FrostHelperBlueBoosterPlacement(x::Integer, y::Integer, boostTime::Number=0.25, respawnTime::Number=1.0, particleColor::String="LightSkyBlue", directory="objects/FrostHelper/blueBooster/", reappearSfx::String="event:/game/04_cliffside/greenbooster_reappear", enterSfx::String="event:/game/04_cliffside/greenbooster_enter", boostSfx::String="event:/game/04_cliffside/greenbooster_dash", releaseSfx::String="event:/game/04_cliffside/greenbooster_end", red::Bool=false)

const colors = sort(collect(keys(Ahorn.XNAColors.colors)))

const placements = Ahorn.PlacementDict(
	"Blue Booster (Frost Helper)" => Ahorn.EntityPlacement(
		FrostHelperBlueBoosterPlacement
	)
)

Ahorn.editingOptions(entity::FrostHelperBlueBoosterPlacement) = Dict{String, Any}(
    "particleColor" => colors
)

sprite = "objects/booster/booster00"

function Ahorn.selection(entity::FrostHelperBlueBoosterPlacement)
    x, y = Ahorn.position(entity)
    return Ahorn.getSpriteRectangle(sprite, x, y)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::FrostHelperBlueBoosterPlacement, room::Maple.Room)
	texture = strip(get(entity.data, "directory", "objects/FrostHelper/blueBooster/"), '/')
	realTexture = "$texture/booster00.png"
	
	Ahorn.drawSprite(ctx, realTexture, 0, 0)
end
	
end