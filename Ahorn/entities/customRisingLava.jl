module FrostHelperCustomRisingLava

using ..Ahorn, Maple

@mapdef Entity "FrostHelper/CustomRisingLava" FHCusRisingLava(x::Integer, y::Integer, intro::Bool=false, speed::Integer=-30, coldColor1::String="33ffe7", coldColor2::String="4ca2eb", coldColor3::String="0151d0", hotColor1::String="ff8933", hotColor2::String="f25e29", hotColor3::String="d01c01", reverseCoreMode::Bool=false, doRubberbanding::Bool=true)

const placements = Ahorn.PlacementDict(
    "Custom Rising Lava (Frost Helper)" => Ahorn.EntityPlacement(
        FHCusRisingLava
    )
)

function Ahorn.selection(entity::FHCusRisingLava)
    x, y = Ahorn.position(entity)

    return Ahorn.Rectangle(x - 12, y - 12, 24, 24)
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::FHCusRisingLava, room::Maple.Room) = Ahorn.drawImage(ctx, Ahorn.Assets.risingLava, -12, -12)

end