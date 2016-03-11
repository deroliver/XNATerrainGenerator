float4x4 View;
float4x4 Projection;

float3 LightDirection = float3(1, -1, 0);
float TextureTiling = 1;

float DetailTextureTiling;
float DetailDistance = 2500;

texture2D DetailTexture;
sampler2D DetailSampler = sampler_state {
 texture = <DetailTexture>;
 AddressU = Wrap;
 AddressV = Wrap;
 MinFilter = Linear;
 MagFilter = Linear;
};


texture2D RTexture;
sampler2D RTextureSampler = sampler_state {
 texture = <RTexture>;
 AddressU = Wrap;
 AddressV = Wrap;
 MinFilter = Anisotropic;
 MagFilter = Anisotropic;
};

texture2D GTexture;
sampler2D GTextureSampler = sampler_state {
 texture = <GTexture>;
 AddressU = Wrap;
 AddressV = Wrap;
 MinFilter = Anisotropic;
 MagFilter = Anisotropic;
};

texture2D BTexture;
sampler2D BTextureSampler = sampler_state {
 texture = <BTexture>;
 AddressU = Wrap;
 AddressV = Wrap;
 MinFilter = Anisotropic;
 MagFilter = Anisotropic;
};


texture2D BaseTexture;
sampler2D BaseTextureSampler = sampler_state {
	Texture = <BaseTexture>;
	AddressU = Wrap;
	AddressV = Wrap;
	MinFilter = Anisotropic;
	MagFilter = Anisotropic;
};


texture2D WeightMap;
sampler2D WeightMapSampler = sampler_state {
 texture = <WeightMap>;
 AddressU = Clamp;
 AddressV = Clamp;
 MinFilter = Linear;
 MagFilter = Linear;
};

struct VertexShaderInput
{
    float4 Position : SV_POSITION;
	float2 UV : TEXCOORD0;
	float3 Normal : NORMAL0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
	float2 UV : TEXCOORD0;
	float3 Normal : TEXCOORD1;
	float Depth : TEXCOORD2;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;

	output.Position = mul(input.Position, mul(View, Projection));
	output.Normal = input.Normal;
	output.UV = input.UV;

    return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
	float light = dot(normalize(input.Normal), normalize(LightDirection));
 	light = clamp(light + 0.4f, 0, 1);
 	float3 rTex = tex2D(RTextureSampler, input.UV * TextureTiling);
 	float3 gTex = tex2D(GTextureSampler, input.UV * TextureTiling);
 	float3 bTex = tex2D(BTextureSampler, input.UV * TextureTiling);

 	float3 base = tex2D(BaseTextureSampler, input.UV * TextureTiling);
 	float3 weightMap = tex2D(WeightMapSampler, input.UV);
 	float3 output = clamp(1.0f - weightMap.r - weightMap.g -
	weightMap.b, 0, 1);
 	output *= base;
 	output += weightMap.r * rTex + weightMap.g * gTex +
	weightMap.b * bTex;

	float3 detail = tex2D(DetailSampler, input.UV * DetailTextureTiling);
	float detailAmt = input.Depth / DetailDistance;
	detail = lerp(detail, 1, clamp(detailAmt, 0, 1));
	
	return float4(detail * output * light, 1);
}

technique Technique1
{
    pass Pass1
    {
        VertexShader = compile vs_4_0_level_9_1 VertexShaderFunction();
        PixelShader = compile ps_4_0_level_9_1 PixelShaderFunction();
    }
}