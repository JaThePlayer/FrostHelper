module FrostHelperShaderWrapperBackdrop

using ..Ahorn, Maple

@mapdef Effect "FrostHelper/ShaderWrapper" ShaderWrapper(only::String="*", exclude::String="", wrappedTag::String="", shader::String="")

placements = ShaderWrapper

function Ahorn.canFgBg(effect::ShaderWrapper)
    return true, true
end

function Ahorn.editingOptions(effect::ShaderWrapper)
    return Dict{String, Any}(
        "side" => String["Top", "Bottom", "Left", "Right"]
    )
end

end