#define DECLARE_TEXTURE(Name, index) \
    texture Name: register(t##index); \
    sampler Name##Sampler: register(s##index)

#define SAMPLE_TEXTURE(Name, texCoord) tex2D(Name##Sampler, texCoord)

uniform float2 CamPos;
uniform float2 Dimensions;
uniform float Time;

DECLARE_TEXTURE(text, 0);


float4 PS_TestEffect(float2 uv : TEXCOORD0, float4 color : COLOR0) : COLOR
{
    float4 textColor = SAMPLE_TEXTURE(text, uv);
    return textColor.a > 0 ? color : float4(0,0,0,0);
}

technique TestEffect
{
    pass pass0
    {
        PixelShader = compile ps_2_0 PS_TestEffect();
    }
} 