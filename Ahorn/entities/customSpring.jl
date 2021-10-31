module FrostHelperCustomSpring

using ..Ahorn, Maple

@mapdef Entity "FrostHelper/SpringFloor" CustomSpring(x::Integer, y::Integer, playerCanUse::Bool=true, directory::String="objects/spring/", speedMult::Number=1.0, oneUse::Bool=false, renderOutline::Bool=true)
@mapdef Entity "FrostHelper/SpringRight" CustomSpringRight(x::Integer, y::Integer, playerCanUse::Bool=true, directory::String="objects/spring/", speedMult::String="1.0,1.0", oneUse::Bool=false, renderOutline::Bool=true)
@mapdef Entity "FrostHelper/SpringLeft" CustomSpringLeft(x::Integer, y::Integer, playerCanUse::Bool=true, directory::String="objects/spring/", speedMult::String="1.0,1.0", oneUse::Bool=false, renderOutline::Bool=true)
@mapdef Entity "FrostHelper/SpringCeiling" CustomSpringCeiling(x::Integer, y::Integer, playerCanUse::Bool=true, directory::String="objects/spring/", speedMult::Number=1.0, oneUse::Bool=false, renderOutline::Bool=true)

const placements = Ahorn.PlacementDict(
    "Custom Spring (Up, Frost Helper)" => Ahorn.EntityPlacement(
        CustomSpring
    ),
    "Custom Spring (Left, Frost Helper)" => Ahorn.EntityPlacement(
        CustomSpringRight
    ),
    "Custom Spring (Right, Frost Helper)" => Ahorn.EntityPlacement(
        CustomSpringLeft
    ),
    "Custom Spring (Down, Frost Helper)" => Ahorn.EntityPlacement(
        CustomSpringCeiling
    ),
)

function Ahorn.selection(entity::CustomSpring)
    x, y = Ahorn.position(entity)

    return Ahorn.Rectangle(x - 6, y - 3, 12, 5)
end

function Ahorn.selection(entity::CustomSpringLeft)
    x, y = Ahorn.position(entity)

    entity.data["speedMult"] = string(get(entity.data, "speedMult", "1.0"))
    return Ahorn.Rectangle(x - 1, y - 6, 5, 12)
end

function Ahorn.selection(entity::CustomSpringRight)
    x, y = Ahorn.position(entity)

    return Ahorn.Rectangle(x - 4, y - 6, 5, 12)
end

function Ahorn.selection(entity::CustomSpringCeiling)
    x, y = Ahorn.position(entity)

    return Ahorn.Rectangle(x - 6, y - 1, 12, 5)
end

sprite = "objects/spring/00.png"

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::CustomSpring, room::Maple.Room) = Ahorn.drawSprite(ctx, sprite, 0, -8)
function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::CustomSpringLeft, room::Maple.Room)
    entity.data["speedMult"] = string(get(entity.data, "speedMult", "1.0"))
    Ahorn.drawSprite(ctx, sprite, 9, -11, rot=pi / 2)
end
function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::CustomSpringRight, room::Maple.Room)
    entity.data["speedMult"] = string(get(entity.data, "speedMult", "1.0"))
    Ahorn.drawSprite(ctx, sprite, 3, 1, rot=-pi / 2)
end
Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::CustomSpringCeiling, room::Maple.Room) = Ahorn.drawSprite(ctx, sprite, 12, -2, rot=pi)

function getAllAttributes(entity)
    return entity.x, entity.y, Bool(get(entity.data, "playerCanUse", true)), get(entity.data, "directory", "objects/spring/"), string(get(entity.data, "speedMult", 1.0)), Bool(get(entity.data, "oneUse", false)), Bool(get(entity.data, "renderOutline", true))
end

function turnIntoRight(entity)
    x, y, playerCanUse, directory, speedMult, oneUse, renderOutline = getAllAttributes(entity) 
    return CustomSpringRight(x, y, playerCanUse, directory, speedMult, oneUse, renderOutline)
end

function turnIntoLeft(entity)
    x, y, playerCanUse, directory, speedMult, oneUse, renderOutline = getAllAttributes(entity) 
    return CustomSpringLeft(x, y, playerCanUse, directory, speedMult, oneUse, renderOutline)
end

function turnIntoUp(entity)
    x, y, playerCanUse, directory, speedMult, oneUse, renderOutline = getAllAttributes(entity) 
    return CustomSpring(x, y, playerCanUse, directory, parse(Float32, speedMult), oneUse, renderOutline)
end

function turnIntoDown(entity)
    x, y, playerCanUse, directory, speedMult, oneUse, renderOutline = getAllAttributes(entity) 
    return CustomSpringCeiling(x, y, playerCanUse, directory, parse(Float32, speedMult), oneUse, renderOutline)
end

function Ahorn.flipped(entity::CustomSpringLeft, horizontal::Bool)
    if horizontal
        return turnIntoRight(entity)
    end

    return turnIntoUp(entity)
end

function Ahorn.flipped(entity::CustomSpringRight, horizontal::Bool)
    if horizontal
        return turnIntoLeft(entity) 
    end

    return turnIntoUp(entity)
end

function Ahorn.flipped(entity::CustomSpring, horizontal::Bool)
    if horizontal
        return turnIntoRight(entity)
    end

    return turnIntoDown(entity)
end

function Ahorn.flipped(entity::CustomSpringCeiling, horizontal::Bool)
    if horizontal
        return turnIntoRight(entity)
    end

    return turnIntoUp(entity)
end

function Ahorn.rotated(entity::CustomSpringRight, steps::Int)
    if steps > 0 # r
        return Ahorn.rotated(turnIntoUp(entity), steps - 1)

    elseif steps < 0 # l
        return Ahorn.rotated(turnIntoDown(entity), steps + 1)
    end

    return entity
end

function Ahorn.rotated(entity::CustomSpringLeft, steps::Int)
    if steps > 0 # r
        return Ahorn.rotated(turnIntoDown(entity), steps - 1)

    elseif steps < 0 # l
        return Ahorn.rotated(turnIntoUp(entity), steps + 1)
    end

    return entity
end

function Ahorn.rotated(entity::CustomSpring, steps::Int)
    if steps > 0 # r
        return Ahorn.rotated(turnIntoLeft(entity), steps - 1)

    elseif steps < 0
        return Ahorn.rotated(turnIntoRight(entity), steps + 1)
    end

    return entity
end

function Ahorn.rotated(entity::CustomSpringCeiling, steps::Int)
    if steps > 0 # r
        return Ahorn.rotated(turnIntoRight(entity), steps - 1)

    elseif steps < 0
        return Ahorn.rotated(turnIntoLeft(entity), steps + 1)
    end

    return entity
end

end