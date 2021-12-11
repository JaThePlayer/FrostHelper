module CustomSpinnerFrostHelper

using ..Ahorn, Maple

@mapdef Entity "FrostHelper/IceSpinner" IceSpinner(x::Integer, y::Integer, attachToSolid::Bool=false, directory::String="danger/FrostHelper/icecrystal", spritePathSuffix::String="", destroyColor::String="b0eaff", tint::String="ffffff", borderColor::String="000000", moveWithWind::Bool=false, dashThrough::Bool=false, bloomAlpha::Number=0.0, bloomRadius::Number=0.0, collidable::Bool=true, rainbow::Bool=false, drawOutline::Bool=true)

const placements = Ahorn.PlacementDict(
   "Custom Spinner (Rainbow Spinner Sprite, Frost Helper)" => Ahorn.EntityPlacement(
        IceSpinner,
		"point",
		Dict{String, Any}(),
        function(entity)
			entity.data["directory"] = "danger/crystal"
			entity.data["spritePathSuffix"] = "_white"
        end
    ),
	"Custom Spinner (Frost Helper)" => Ahorn.EntityPlacement(
		IceSpinner
	)
)

function Ahorn.selection(entity::IceSpinner)
    x, y = Ahorn.position(entity)

    return Ahorn.Rectangle(x - 8, y - 8, 16, 16)
end

# TODO - Add support for background
function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::IceSpinner, room::Maple.Room)
	texture = get(entity.data, "directory", "danger/FrostHelper/icecrystal")
	spritePathSuffix = get(entity.data, "spritePathSuffix", "")

	realTexture = "$texture/fg$(spritePathSuffix)03.png"

	rawTint = Ahorn.argb32ToRGBATuple(parse(Int, lstrip(get(entity.data, "tint", "ffffff"), [ '#' ]), base=16))[1:3] ./ 255
	realTint = (rawTint..., 1.0)
    Ahorn.drawSprite(ctx, realTexture, 0, 0, tint = realTint)
end

end