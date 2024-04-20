uniform float4 SolidColor;
uniform float4x4 World;

float4 PS_TestEffect(float4 color : COLOR0) : COLOR0
{
    return color.a > 0 ? SolidColor : color;
}

void SpriteVertexShader(inout float4 color    : COLOR0,
                        inout float4 position : SV_Position)
{
    position = mul(position, World);
}

technique TestEffect
{
    pass pass0
    {
        VertexShader = compile vs_2_0 SpriteVertexShader();
        PixelShader = compile ps_2_0 PS_TestEffect();
    }
} 