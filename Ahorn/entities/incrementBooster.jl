module FrostHelperIncrementBoosterModule

using ..Ahorn, Maple

@mapdef Entity "FrostHelper/IncrementBooster" FHIncrementalBooster(x::Integer, y::Integer, boostTime::Number=0.25, respawnTime::Number=1.0, particleColor::String="93bd40", directory="objects/FrostHelper/dashIncrementBooster/", reappearSfx::String="event:/game/04_cliffside/greenbooster_reappear", enterSfx::String="event:/game/04_cliffside/greenbooster_enter", boostSfx::String="event:/game/04_cliffside/greenbooster_dash", releaseSfx::String="event:/game/04_cliffside/greenbooster_end", red::Bool=false, dashCap::Integer = -1, dashes::Integer=1, refillBeforeIncrementing::Bool=false)

const colors = sort(collect(keys(Ahorn.XNAColors.colors)))

const placements = Ahorn.PlacementDict(
	"Dash Increment Booster (Frost Helper)" => Ahorn.EntityPlacement(
		FHIncrementalBooster
	),
	"Dash Increment Booster (Red, Frost Helper)" => Ahorn.EntityPlacement(
		FHIncrementalBooster,
        "point",
        Dict{String, Any}(),
        function(entity)
			entity.data["reappearSfx"] = "event:/game/05_mirror_temple/redbooster_reappear"
			entity.data["boostSfx"] = "event:/game/05_mirror_temple/redbooster_dash"
			entity.data["enterSfx"] = "event:/game/05_mirror_temple/redbooster_enter"
			entity.data["releaseSfx"] = "event:/game/05_mirror_temple/redbooster_end"
			entity.data["red"] = true
			entity.data["directory"] = "objects/FrostHelper/dashIncrementBoosterRed/"
			entity.data["particleColor"] = "c268d1"
			entity.data["dashes"] = 2
        end
	)
)

Ahorn.editingOptions(entity::FHIncrementalBooster) = Dict{String, Any}(
    "particleColor" => colors
)

sprite = "objects/booster/booster00"

function Ahorn.selection(entity::FHIncrementalBooster)
    x, y = Ahorn.position(entity)
    return Ahorn.getSpriteRectangle(sprite, x, y)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::FHIncrementalBooster, room::Maple.Room)
	texture = strip(get(entity.data, "directory", "objects/FrostHelper/blueBooster/"), '/')
	realTexture = "$texture/booster00.png"
	
	Ahorn.drawSprite(ctx, realTexture, 0, 0)
end
	
end