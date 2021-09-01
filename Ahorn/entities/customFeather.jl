module FrostHelperCustomFeatherModule

using ..Ahorn, Maple

@mapdef Entity "FrostHelper/CustomFeather" FrostHelperCustomFeatherPlacement(x::Integer, y::Integer, shielded::Bool=false, singleUse::Bool=false, respawnTime::Number=3.0, flyColor::String="ffd65c", spriteColor="White", flyTime::Number=2.0, maxSpeed::Number=190.0, lowSpeed::Number=140.0, neutralSpeed::Number=91.0, spritePath::String="objects/flyFeather/")

const colors = sort(collect(keys(Ahorn.XNAColors.colors)))

const placements = Ahorn.PlacementDict(
	"Custom Feather (Frost Helper)" => Ahorn.EntityPlacement(
		FrostHelperCustomFeatherPlacement
	)
)

Ahorn.editingOptions(entity::FrostHelperCustomFeatherPlacement) = Dict{String, Any}(
    "flyColor" => colors,
	"spriteColor" => colors
)

sprite = "objects/flyFeather/idle00.png"

function Ahorn.selection(entity::FrostHelperCustomFeatherPlacement)
    x, y = Ahorn.position(entity)
    return Ahorn.getSpriteRectangle(sprite, x, y)
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::FrostHelperCustomFeatherPlacement, room::Maple.Room) = Ahorn.drawSprite(ctx, sprite, 0, 0)

end