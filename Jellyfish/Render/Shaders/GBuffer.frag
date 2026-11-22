#version 460
#include CommonFrag.frag

in vec2 frag_texCoord;
in vec3 frag_normal;
in vec3 frag_position;

//layout (location = 0) out vec3 WorldPosOut;
//layout (location = 1) out vec4 DiffuseOut;
layout (location = 0) out vec3 NormalOut;
//layout (location = 3) out vec3 TexCoordOut;
layout (location = 1) out float RoughnessOut;

layout (binding = 0) uniform sampler2D diffuseSampler;
layout (binding = 1) uniform sampler2D normalSampler;
layout (binding = 2) uniform sampler2D metroughSampler;

uniform mat4 view;
uniform bool hasNormalMap;
uniform bool hasAlphaTest;
uniform bool hasMetRough;

#define VIEWSPACE_NORMALS 1

void main()
{
    if (hasAlphaTest && texture(diffuseSampler, frag_texCoord * vec2(1.0, -1.0)).a < 0.5)
        discard;

    vec3 normal;
    if (hasNormalMap) {
        vec3 normalTex = texture(normalSampler, frag_texCoord * vec2(1.0, -1.0)).rgb;
        vec3 tangentSpaceNormal = normalize(normalTex * 2.0 - 1.0);

        mat3 tbn = GetTBN(frag_position, frag_texCoord, frag_normal);
        normal = normalize(tbn * tangentSpaceNormal);
    }
    else {
        normal = normalize(frag_normal);
    }
    
    // same roughness the main pass ends up with, so screenspace passes can skip pixels that won't use them
    float roughnessTex = hasMetRough ? texture(metroughSampler, frag_texCoord * vec2(1.0, -1.0)).g : 0.9;
    float roughness = SpecularAntiAliasing(normal, roughnessTex);

    if (VIEWSPACE_NORMALS == 1) {
        normal = normalize((view * vec4(normal, 0.0)).xyz);
    }

    //WorldPosOut = frag_position;
    //DiffuseOut = diffuseTex;
    NormalOut = normalize(normal);
    RoughnessOut = roughness;
    //TexCoordOut = vec3(frag_texCoord, 0.0);
}