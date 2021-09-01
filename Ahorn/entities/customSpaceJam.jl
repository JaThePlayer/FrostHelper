module FrostHelperSpaceJam

using ..Ahorn, Maple

@mapdef Entity "FrostHelper/CustomDreamBlock" CustomDreamBlock(x::Integer, y::Integer, width::Integer=16, height::Integer=16, oneUse::Bool=false, below::Bool=false, speed::Number=240.0, activeBackColor::String="Black", disabledBackColor::String="1f2e2d", activeLineColor::String="White", disabledLineColor::String="6a8480", allowRedirects::Bool=false, allowSameDirectionDash::Bool=false, sameDirectionSpeedMultiplier::Number=2.0, old::Bool=false, moveEase::String="SineInOut", moveSpeedMult::Number=1.0, conserveSpeed::Bool=false)

const colors = sort(collect(keys(Ahorn.XNAColors.colors)))

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
    "Custom Space Jam (Frost Helper)" => Ahorn.EntityPlacement(
        CustomDreamBlock,
        "rectangle"
    )
)

Ahorn.editingOptions(entity::CustomDreamBlock) = Dict{String, Any}(
    "activeBackColor" => colors,
	"activeLineColor" => colors,
	"disabledBackColor" => colors,
	"disabledLineColor" => colors,
	"moveEase" => easings
)

Ahorn.nodeLimits(entity::CustomDreamBlock) = 0, 1

Ahorn.minimumSize(entity::CustomDreamBlock) = 8, 8
Ahorn.resizable(entity::CustomDreamBlock) = true, true

function Ahorn.selection(entity::CustomDreamBlock)
    x, y = Ahorn.position(entity)

    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))

    nodes = get(entity.data, "nodes", ())
    if isempty(nodes)
        return Ahorn.Rectangle(x, y, width, height)

    else
        nx, ny = Int.(nodes[1])
        return [Ahorn.Rectangle(x, y, width, height), Ahorn.Rectangle(nx, ny, width, height)]
    end
end

function renderSpaceJam(ctx::Ahorn.Cairo.CairoContext, x::Number, y::Number, width::Number, height::Number)
    Ahorn.Cairo.save(ctx)

    Ahorn.set_antialias(ctx, 1)
    Ahorn.set_line_width(ctx, 1)

    Ahorn.drawRectangle(ctx, x, y, width, height, (0.0, 0.0, 0.0, 0.4), (1.0, 1.0, 1.0, 1.0))

    Ahorn.restore(ctx)
end

function Ahorn.renderSelectedAbs(ctx::Ahorn.Cairo.CairoContext, entity::CustomDreamBlock)
    x, y = Ahorn.position(entity)
    nodes = get(entity.data, "nodes", ())

    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))
    
    if !isempty(nodes)
        nx, ny = Int.(nodes[1])

        cox, coy = floor(Int, width / 2), floor(Int, height / 2)

        renderSpaceJam(ctx, nx, ny, width, height)
        Ahorn.drawArrow(ctx, x + cox, y + coy, nx + cox, ny + coy, Ahorn.colors.selection_selected_fc, headLength=6)
    end
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::CustomDreamBlock, room::Maple.Room)
    x = Int(get(entity.data, "x", 0))
    y = Int(get(entity.data, "y", 0))

    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))

    renderSpaceJam(ctx, 0, 0, width, height)
end

end