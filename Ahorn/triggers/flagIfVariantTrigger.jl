module FrostHelperFlagIfVariantTriggerModule

using ..Ahorn, Maple

@mapdef Trigger "FrostHelper/FlagIfVariantTrigger" FlagIfVariantPlacement(x::Integer, y::Integer, width::Integer=16, height::Integer=16, variant::String="Invincible", flag::String="", variantValue::String="true", inverted::Bool=false)

const placements = Ahorn.PlacementDict(
	"Flag If Variant (Frost Helper)" => Ahorn.EntityPlacement(
		FlagIfVariantPlacement,
		"rectangle"
	)
)

const variants = String[
	"DashAssist",
    "GameSpeed",
    "Hiccups",
    "InfiniteStamina",
    "Invincible",
    "InvisibleMotion",
    "LowFriction",
    "MirrorMode",
    "NoGrabbing",
    "PlayAsBadeline",
    "SuperDashing",
    "ThreeSixtyDashing",
]

const variantValuesBool = String[
	"true", "false"
]

const variantValuesGameSpeed = String[
	"160", "150", "140", "130", "120", "110", "100", "90", "80", "70", "60", "50"
]

function getEditingOptions(entity::FlagIfVariantPlacement)
	variant = get(entity.data, "variant", "Invincible")
	if variant == "GameSpeed"
		return Dict{String, Any}(
    		"variant" => variants,
			"variantValue" => variantValuesGameSpeed
		)
	else
		return Dict{String, Any}(
    		"variant" => variants,
			"variantValue" => variantValuesBool
		)
	end
end

#=
Ahorn.editingOptions(entity::FlagIfVariantPlacement) = Dict{String, Any}(
	"variant" => variants,
	"variantValue" => variantValuesBool
)
=#

Ahorn.editingOptions(entity::FlagIfVariantPlacement) = getEditingOptions(entity)
#=

function Ahorn.editingOptions(entity::FlagIfVariantPlacement)
	variant = Int(get(entity.data, "variant", "Invincible"))
	if variant == "GameSpeed"
		return Dict{String, Any}(
    		"variant" => variants,
			"variantValue" => variantValuesGameSpeed
		)
	else
		return Dict{String, Any}(
    		"variant" => variants,
			"variantValue" => variantValuesBool
		)
	end
end =#


end