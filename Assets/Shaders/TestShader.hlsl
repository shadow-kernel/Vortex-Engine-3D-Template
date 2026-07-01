// TEST custom shader — deliberately NOT PBR so the difference is obvious: a rainbow colour by world position plus a
// bright cyan "energy" rim glow at grazing angles (Fresnel). Assign it to a material in the Material Editor's Shader
// Asset slot; edit + save this file and re-focus the game/editor to hot-reload it live.
cbuffer PerFrame : register(b0)
{
    row_major float4x4 ViewProjection;
    float3 CameraPosition;      float Padding0;
    float3 LightDirection;      float DirectionalIntensity;
    float3 LightColor;          float AmbientStrength;
    uint PointLightCount; uint SpotLightCount; uint2 FramePadding;
};
cbuffer PerObject : register(b1)
{
    row_major float4x4 World;
    float4 BaseColor;
    float Metallic; float Roughness; float AO; float NormalStrength;
    uint HasAlbedoTexture; uint HasNormalTexture; uint HasMetallicTexture; uint HasRoughnessTexture;
    uint HasAOTexture; uint UseDirectXNormals; uint IsUnlit; float EmissiveStrength;
};
struct VS_IN
{
    float3 pos : POSITION; float3 norm : NORMAL; float2 uv : TEXCOORD0;
    float4 iw0 : INSTANCEWORLD0; float4 iw1 : INSTANCEWORLD1; float4 iw2 : INSTANCEWORLD2; float4 iw3 : INSTANCEWORLD3;
};
struct PS_IN { float4 pos : SV_POSITION; float3 worldPos : TEXCOORD1; float3 norm : TEXCOORD2; float2 uv : TEXCOORD0; };

PS_IN VSMain(VS_IN input)
{
    PS_IN o;
    float4x4 W = float4x4(input.iw0, input.iw1, input.iw2, input.iw3);
    float4 wp = mul(float4(input.pos, 1), W);
    o.worldPos = wp.xyz;
    o.pos = mul(wp, ViewProjection);
    o.norm = normalize(mul(input.norm, (float3x3)W));
    o.uv = input.uv;
    return o;
}

// h in [0,1] -> smooth rainbow
float3 hue(float h)
{
    float3 c = abs(frac(h + float3(0.0, 2.0 / 3.0, 1.0 / 3.0)) * 6.0 - 3.0) - 1.0;
    return saturate(c);
}

float4 PSMain(PS_IN i) : SV_TARGET
{
    float3 n = normalize(i.norm);
    float3 v = normalize(CameraPosition - i.worldPos);

    // Rainbow hue from world position (gives BANDS across big flat surfaces like the ground) PLUS the surface
    // normal (so curved objects — e.g. the material-preview sphere — show a full rainbow too, not one flat colour).
    float h = frac(i.worldPos.x * 0.06 + i.worldPos.z * 0.03 + n.x * 0.35 + n.y * 0.25);
    float3 col = hue(h);

    // Cyan rim (Fresnel) at grazing angles — BLEND toward it (lerp) so it accents the edges but never blows out
    // to solid white (the old additive * 1.6 saturated to white on a sphere, where most of the surface is grazing).
    float fres = pow(1.0 - saturate(dot(n, v)), 3.0);
    col = lerp(col, float3(0.55, 0.95, 1.0), fres * 0.5);

    
    return float4(col, 1.0);
}