// Not actually a proper reimplementation, do not use

#define DECLARE_TEXTURE(Name, index) \
    texture Name: register(t##index); \
    sampler Name##Sampler: register(s##index)

#define SAMPLE_TEXTURE(Name, texCoord) tex2D(Name##Sampler, texCoord)

#define _vs(r)  : register(vs, r)
#define _ps(r)  : register(ps, r)
#define _cb(r)


uniform float2 CamPos;
uniform float2 Dimensions;
uniform float Time;

uniform float4x4 TransformMatrix;
uniform float4x4 ViewMatrix;

DECLARE_TEXTURE(text, 0);

// https://gist.github.com/iUltimateLP/5129149bf82757b31542
float3 hsvToColor(float3 input) {
    float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    float3 p = abs(frac(input.xxx + K.xyz) * 6.0 - K.www);

    return input.z * lerp(K.xxx, saturate(p - K.xxx), input.y);
}

float length(float2 pos) {
	return sqrt(pos.x * pos.x + pos.y * pos.y);
}

float yoyo(float value) {
	if (value <= 0.5)
	{
		return value * 2.;
	}
	return 1. - (value - 0.5) * 2.;
}

float4 PS_TestEffect(float2 uv : TEXCOORD0, float4 color : COLOR0, float4 pixelPos : SV_Position) : COLOR
{
    float4 textColor = SAMPLE_TEXTURE(text, uv);
    float2 pos = pixelPos.xy + CamPos;

	float value = ((length(pos * 6.) + Time * 550.) % 280.) / 280.;

    return textColor.a > 0 ? float4(hsvToColor(float3(0.4 + yoyo(value) * 0.4, 0.4, 0.9)), 1.) : float4(0,0,0,0);
}

void SpriteVertexShader(inout float4 color    : COLOR0,
                        inout float2 texCoord : TEXCOORD0,
                        inout float4 position : SV_Position)
{
    position = mul(position, ViewMatrix);
    position = mul(position, TransformMatrix);
}

technique TestEffect
{
    pass pass0
    {
        VertexShader = compile vs_3_0 SpriteVertexShader();
        PixelShader = compile ps_3_0 PS_TestEffect();
    }
} 