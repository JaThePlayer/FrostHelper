module FHArbitraryShapeFireBarrier

using ..Ahorn, Maple

@mapdef Entity "FrostHelper/ArbitraryShapeFireBarrier" ShapeFireBarrier(x::Integer, y::Integer, aEdge::Bool=true, bEdge::Bool=true, cEdge::Bool=true)

const placements = Ahorn.PlacementDict(
    "Arbitrary Shape Fire Barrier (Frost Helper)" => Ahorn.EntityPlacement(
        ShapeFireBarrier,
        "point",
        Dict{String, Any}(),
        function(entity)
            entity.data["nodes"] = [(Int(entity.data["x"]) + 8, Int(entity.data["y"])), (Int(entity.data["x"]) + 8, Int(entity.data["y"]) + 8)]
        end
    ),
)

const seed = "collectables/strawberry/seed00"
const fallback = "collectables/strawberry/normal00"

const transparent = (0.0, 0.0, 0.0, 0.0)
const lightningFillColor = (0.55, 0.97, 0.96, 0.4)
const lightningBorderColor = (0.99, 0.96, 0.47, 1.0)

Ahorn.nodeLimits(entity::ShapeFireBarrier) = 2, -1

function Ahorn.selection(entity::ShapeFireBarrier)
    x, y = Ahorn.position(entity)

    nodes = get(entity.data, "nodes", ())
    hasPips = length(nodes) > 0

    sprite = fallback
    seedSprite = seed

    res = Ahorn.Rectangle[Ahorn.getSpriteRectangle(sprite, x, y)]
    
    for node in nodes
        nx, ny = node

        push!(res, Ahorn.getSpriteRectangle(seedSprite, nx, ny))
    end

    return res
end

function Ahorn.renderSelectedAbs(ctx::Ahorn.Cairo.CairoContext, entity::ShapeFireBarrier)
    x, y = Ahorn.position(entity)

    for node in get(entity.data, "nodes", ())
        nx, ny = node

        Ahorn.drawLines(ctx, Tuple{Number, Number}[(x, y), (nx, ny)], Ahorn.colors.selection_selected_fc)
        Ahorn.drawSprite(ctx, seed, nx, ny)
        x, y = nx,ny
    end
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::ShapeFireBarrier, room::Maple.Room)
    x, y = Ahorn.position(entity)
    nodes = get(entity.data, "nodes", ())
    nodeAmt = length(nodes)

    Ahorn.drawLines(ctx, Base.vcat(Tuple{Number, Number}[(Ahorn.position(entity))], nodes, Tuple{Number, Number}[(Ahorn.position(entity))]), lightningBorderColor; filled=true, fc=lightningFillColor)
    #=
    for node in get(entity.data, "nodes", ())
        nx, ny = node

        Ahorn.drawLines(ctx, Tuple{Number, Number}[(x, y), (nx, ny)], lightningBorderColor)
    
        x, y = nx,ny
    end

    Ahorn.drawLines(ctx, Tuple{Number, Number}[(Ahorn.position(entity)), (x, y)], lightningBorderColor)
    =#
end

end