float4x4 World;
float4x4 View;
float4x4 Projection;
float3 CameraPosition;

texture BaseTexture;
sampler BaseTextureSampler = sampler_state {
	texture = <BaseTexture>;
	AddressU = Wrap;
	AddressV = Wrap;
	MinFilter = Anisotropic;
	MagFilter = Anisotropic;
};

float4 ClipPlane;
bool ClipPlaneEnabled = false;

float3 AmbientLightColor = (0.1, 0.1, 0.1);
float3 DiffuseColor = float3(.85, .85, .85);
float3 LightPosition = float3(8000, 8000, 8000);
float3 LightColor = float3(1.0, 1.0, 1.0);
float LightAttenuation = 200000;

float3 LightDirection = float3(1, 0, 0);
float TextureTiling = 1;

texture WeightMap;
sampler WeightMapSampler = sampler_state {
	texture = <WeightMap>;
	AddressU = Clamp;
	AddressV = Clamp;
	MinFilter = Linear;
	MagFilter = Linear;
};

struct VertexShaderInput
{
    float4 Position : POSITION0;
	float2 UV : TEXCOORD0;
	float3 Normal : NORMAL0;
};

struct VertexShaderOutput
{
    float4 Position			: POSITION0;
	float2 UV				: TEXCOORD0;
	float3 Normal			: TEXCOORD1;
	float Depth				: TEXCOORD2;
	float3 WorldPosition	: TEXCOORD3;
};

float DetailTextureTiling;
float DetailDistance = 2500;

texture DetailTexture;
sampler DetailSampler = sampler_state {
	texture = <DetailTexture>;
	AddressU = Wrap;
	AddressV = Wrap;
	MinFilter = Linear;
	MagFilter = Linear;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input) {
    VertexShaderOutput output;

	float4 worldPosition = input.Position;
	float4 viewPosition = mul(worldPosition, View);

	output.Position = mul(viewPosition, Projection);

	output.WorldPosition = worldPosition;
	output.Normal = input.Normal;
	output.UV = input.UV;
	output.Depth = output.Position.z;

    return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0 {

	float3 diffuseColor = clamp(1.0f, 0, 1);

	diffuseColor *= tex2D(BaseTextureSampler, input.UV * TextureTiling);	
	diffuseColor *= AmbientLightColor;

	float3 totalLight = dot(normalize(input.Normal), normalize(LightDirection));

	//totalLight += AmbientLightColor;

	float3 lightDir = normalize(LightPosition - input.WorldPosition);
	float diffuse = saturate(dot(normalize(input.Normal), lightDir));

	float d = distance(LightPosition, input.WorldPosition);
	float att = 1 - pow(clamp(d / LightAttenuation, 0, 1), 2);

	totalLight += diffuse * att * LightColor;

	float3 detail = tex2D(DetailSampler, input.UV * DetailTextureTiling);
	float detailAmt = input.Depth / DetailDistance;
	detail = lerp(detail, 1, clamp(detailAmt, 0, 1));

	return float4(detail * diffuseColor * totalLight, 1);
}

technique Technique1
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
