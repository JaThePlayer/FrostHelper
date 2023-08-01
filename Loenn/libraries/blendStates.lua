local blendStates = {}

blendStates.blendFunctions =
{
    -- The function will add destination to the source. (srcColor * srcBlend) + (destColor * destBlend)
    "Add",
    -- The function will subtract destination from source. (srcColor * srcBlend) - (destColor * destBlend)
    "Subtract",
    -- The function will subtract source from destination. (destColor * destBlend) - (srcColor * srcBlend)
    "ReverseSubtract",
    -- The function will extract minimum of the source and destination. min((srcColor * srcBlend),(destColor * destBlend))
    "Max",
    -- The function will extract maximum of the source and destination. max((srcColor * srcBlend),(destColor * destBlend))
    "Min"
}

blendStates.blends = {
    -- Each component of the color is multiplied by {1, 1, 1, 1}.
    "One",
    -- Each component of the color is multiplied by {0, 0, 0, 0}.
    "Zero",
    -- Each component of the color is multiplied by the source color. {Rs, Gs, Bs, As},
    -- where Rs, Gs, Bs, As are color source values.
    "SourceColor",
    -- Each component of the color is multiplied by the inverse of the source color.
    -- {1 - Rs, 1 - Gs, 1 - Bs, 1 - As}, where Rs, Gs, Bs, As are color source values.
    "InverseSourceColor",
    -- Each component of the color is multiplied by the alpha value of the source. {As,
    -- As, As, As}, where As is the source alpha value.
    "SourceAlpha",
    -- Each component of the color is multiplied by the inverse of the alpha value of
    -- the source. {1 - As, 1 - As, 1 - As, 1 - As}, where As is the source alpha value.
    "InverseSourceAlpha",
    -- Each component color is multiplied by the destination color. {Rd, Gd, Bd, Ad},
    -- where Rd, Gd, Bd, Ad are color destination values.
    "DestinationColor",
    -- Each component of the color is multiplied by the inversed destination color.
    -- {1 - Rd, 1 - Gd, 1 - Bd, 1 - Ad}, where Rd, Gd, Bd, Ad are color destination
    -- values.
    "InverseDestinationColor",
    -- Each component of the color is multiplied by the alpha value of the destination.
    -- {Ad, Ad, Ad, Ad}, where Ad is the destination alpha value.
    "DestinationAlpha",
    -- Each component of the color is multiplied by the inversed alpha value of the
    -- destination. {1 - Ad, 1 - Ad, 1 - Ad, 1 - Ad}, where Ad is the destination alpha
    -- value.
    "InverseDestinationAlpha",
    -- Each component of the color is multiplied by a constant in the Microsoft.Xna.Framework.Graphics.GraphicsDevice.BlendFactor.
    "BlendFactor",
    -- Each component of the color is multiplied by a inversed constant in the Microsoft.Xna.Framework.Graphics.GraphicsDevice.BlendFactor.
    "InverseBlendFactor",
    -- Each component of the color is multiplied by either the alpha of the source color,
    -- or the inverse of the alpha of the source color, whichever is greater. {f, f,
    -- f, 1}, where f = min(As, 1 - As), where As is the source alpha value.
    "SourceAlphaSaturation"
}

--[Flags]
blendStates.colorWriteChannels =
{
    -- No channels selected.
    None = 0x0,
    -- Red channel selected.
    Red = 0x1,
    -- Green channel selected.
    Green = 0x2,
    -- Blue channel selected.
    Blue = 0x4,
    -- Alpha channel selected.
    Alpha = 0x8,
    -- All channels selected.
    All = 0xF
}

return blendStates