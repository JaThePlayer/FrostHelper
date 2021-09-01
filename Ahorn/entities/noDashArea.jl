module FrostHelperNoDashArea

using ..Ahorn, Maple

@mapdef Entity "FrostHelper/NoDashArea" NoDashAreaFrostHelper(x::Integer, y::Integer, width::Integer=Maple.defaultBlockWidth, height::Integer=Maple.defaultBlockHeight, fastMoving::Bool=false)

const placements = Ahorn.PlacementDict(
    "No Dash Area (Frost Helper)" => Ahorn.EntityPlacement(
        NoDashAreaFrostHelper,
        "rectangle"
    ),
)

Ahorn.nodeLimits(entity::NoDashAreaFrostHelper) = 0, 1
Ahorn.minimumSize(entity::NoDashAreaFrostHelper) = 8, 8
Ahorn.resizable(entity::NoDashAreaFrostHelper) = true, true

function Ahorn.selection(entity::NoDashAreaFrostHelper)
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

edgeColor = (38, 0, 0, 255) ./ 255
centerColor = (64, 0, 0, 102) ./ 255

function renderSpaceJam(ctx::Ahorn.Cairo.CairoContext, x::Number, y::Number, width::Number, height::Number)
    Ahorn.Cairo.save(ctx)

    Ahorn.set_antialias(ctx, 1)
    Ahorn.set_line_width(ctx, 1)

    Ahorn.drawRectangle(ctx, x, y, width, height, edgeColor, centerColor)

    Ahorn.restore(ctx)
end

function Ahorn.renderSelectedAbs(ctx::Ahorn.Cairo.CairoContext, entity::NoDashAreaFrostHelper)
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

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::NoDashAreaFrostHelper, room::Maple.Room)
    x = Int(get(entity.data, "x", 0))
    y = Int(get(entity.data, "y", 0))

    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))

    renderSpaceJam(ctx, 0, 0, width, height)
end

end