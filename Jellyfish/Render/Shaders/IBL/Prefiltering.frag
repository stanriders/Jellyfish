#version 460

in vec3 WorldPos;
out vec4 FragColor;

layout (binding = 0) uniform samplerCube environmentMap;

uniform float roughness;   // current mip roughness
uniform int mip;
uniform float envMapResolution; // size of a single environment map face at mip 0
const float PI = 3.14159265359;

float DistributionGGX(float NdotH, float roughness)
{
    float a = roughness * roughness;
    float a2 = a * a;
    float denom = NdotH * NdotH * (a2 - 1.0) + 1.0;
    return a2 / (PI * denom * denom);
}

float RadicalInverse_VdC(uint bits)
{
    bits = (bits << 16u) | (bits >> 16u);
    bits = ((bits & 0x55555555u) << 1u) | ((bits & 0xAAAAAAAAu) >> 1u);
    bits = ((bits & 0x33333333u) << 2u) | ((bits & 0xCCCCCCCCu) >> 2u);
    bits = ((bits & 0x0F0F0F0Fu) << 4u) | ((bits & 0xF0F0F0F0u) >> 4u);
    bits = ((bits & 0x00FF00FFu) << 8u) | ((bits & 0xFF00FF00u) >> 8u);
    return float(bits) * 2.3283064365386963e-10; // / 0x100000000
}

vec2 Hammersley(uint i, uint N)
{
    return vec2(float(i) / float(N), RadicalInverse_VdC(i));
}

vec3 ImportanceSampleGGX(vec2 Xi, vec3 N, float roughness)
{
    float a = roughness*roughness;
	
    float phi = 2.0 * PI * Xi.x;
    float cosTheta = sqrt((1.0 - Xi.y) / (1.0 + (a*a - 1.0) * Xi.y));
    float sinTheta = sqrt(1.0 - cosTheta*cosTheta);
	
    // from spherical coordinates to cartesian coordinates
    vec3 H;
    H.x = cos(phi) * sinTheta;
    H.y = sin(phi) * sinTheta;
    H.z = cosTheta;
	
    // from tangent-space vector to world-space sample vector
    vec3 up        = abs(N.z) < 0.999 ? vec3(0.0, 0.0, 1.0) : vec3(1.0, 0.0, 0.0);
    vec3 tangent   = normalize(cross(up, N));
    vec3 bitangent = cross(N, tangent);
	
    vec3 sampleVec = tangent * H.x + bitangent * H.y + N * H.z;
    return normalize(sampleVec);
} 

void main()
{
    vec3 N = normalize(WorldPos);
    vec3 R = N;
    vec3 V = R;

    uint SAMPLE_COUNT = 1024u >> mip;
    SAMPLE_COUNT = max(SAMPLE_COUNT, 32u);
    
    vec3 prefilteredColor = vec3(0.0);
    float totalWeight = 0.0;

    // solid angle of a single environment map texel at mip 0
    float saTexel = 4.0 * PI / (6.0 * envMapResolution * envMapResolution);

    for(uint i = 0u; i < SAMPLE_COUNT; ++i)
    {
        vec2 Xi = Hammersley(i, SAMPLE_COUNT);
        vec3 H  = ImportanceSampleGGX(Xi, N, roughness);
        vec3 L  = normalize(2.0 * dot(V, H) * H - V);

        float NdotL = max(dot(N, L), 0.0);
        if(NdotL > 0.0)
        {
            // filtered importance sampling: pick the mip whose texel covers the same solid angle as this sample,
            // otherwise low-probability samples hitting small bright spots turn into speckles
            // N == V here, so NdotH == HdotV and the pdf simplifies to D / 4
            float NdotH = max(dot(N, H), 0.0);
            float pdf = DistributionGGX(NdotH, roughness) / 4.0 + 1e-4;
            float saSample = 1.0 / (float(SAMPLE_COUNT) * pdf + 1e-4);
            float mipLevel = roughness == 0.0 ? 0.0 : 0.5 * log2(saSample / saTexel);

            prefilteredColor += textureLod(environmentMap, L, mipLevel).rgb * NdotL;
            totalWeight += NdotL;
        }
    }
    prefilteredColor = prefilteredColor / max(totalWeight, 1e-4);

    FragColor = vec4(prefilteredColor, 1.0);
}