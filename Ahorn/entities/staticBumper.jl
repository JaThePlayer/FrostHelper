module StaticBumperFrostHelper

using ..Ahorn, Maple

@mapdef Entity "FrostHelper/StaticBumper" StaticBumper(x::Integer, y::Integer, respawnTime::Number=0.6, moveTime::Number=1.81818187, sprite::String="bumper", wobble::Bool=false, notCoreMode::Bool=false, easing::String="CubeInOut")

const easings = String[
    "BackIn",
    "BackInOut",
    "BackOut",
    "BounceIn",
    "BounceInOut",
    "BounceOut",
	"CubeIn",
    "CubeInOut",
	"CubeOut",
	"ElasticIn",
    "ElasticInOut",
	"ElasticOut",
	"ExpoIn",
    "ExpoInOut",
	"ExpoOut",
    "Linear",
	"QuadIn",
    "QuadInOut",
    "QuadOut",
    "SineIn",
    "SineInOut",
    "SineOut"
]

const placements = Ahorn.PlacementDict(
    "Custom Bumper (Frost Helper)" => Ahorn.EntityPlacement(
        StaticBumper
    )
)

Ahorn.editingOptions(entity::StaticBumper) = Dict{String, Any}(
    "easing" => easings
)

Ahorn.nodeLimits(entity::StaticBumper) = 0, 1

sprite = "objects/Bumper/Idle22.png"

function Ahorn.selection(entity::StaticBumper)
    x, y = Ahorn.position(entity)
	nodes = get(entity.data, "nodes", ())

    if !isempty(nodes)
        nx, ny = Int.(nodes[1])

        return [Ahorn.getSpriteRectangle(sprite, x, y), Ahorn.getSpriteRectangle(sprite, nx, ny)]
    end

    return Ahorn.getSpriteRectangle(sprite, x, y)
end

function Ahorn.renderSelectedAbs(ctx::Ahorn.Cairo.CairoContext, entity::StaticBumper)
    x, y = Ahorn.position(entity)
    nodes = get(entity.data, "nodes", ())

    if !isempty(nodes)
        nx, ny = Int.(nodes[1])

        theta = atan(y - ny, x - nx)
        Ahorn.drawArrow(ctx, x, y, nx + cos(theta) * 8, ny + sin(theta) * 8, Ahorn.colors.selection_selected_fc, headLength=6)
        Ahorn.drawSprite(ctx, sprite, nx, ny)
    end
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::StaticBumper, room::Maple.Room) = Ahorn.drawSprite(ctx, sprite, 0, 0)

end