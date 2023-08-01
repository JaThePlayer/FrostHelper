#define DECLARE_TEXTURE(Name, index) \
    texture Name: register(t##index); \
    sampler Name##Sampler: register(s##index)

#define SAMPLE_TEXTURE(Name, texCoord) tex2D(Name##Sampler, texCoord)

uniform float2 CamPos;
uniform float2 Dimensions;
uniform float Time;

uniform float4x4 TransformMatrix;
uniform float4x4 ViewMatrix;
uniform float4x4 World;

DECLARE_TEXTURE(text, 0);
DECLARE_TEXTURE(prev, 1);

float4 PS_TestEffect(float2 uv : TEXCOORD0, float4 color : COLOR0) : COLOR
{
    float4 textColor = SAMPLE_TEXTURE(text, uv);
    float4 prevColor = SAMPLE_TEXTURE(prev, uv);

    return textColor.a > 0 ? (prevColor) : (0);
}

technique TestEffect
{
    pass pass0
    {
        PixelShader = compile ps_2_0 PS_TestEffect();
    }
} 