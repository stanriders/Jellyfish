#version 460

in vec2 frag_texCoord;
in vec3 frag_normal;
in vec3 frag_position;

layout (location = 0) out vec3 WorldPosOut;
layout (location = 1) out vec4 DiffuseOut;
layout (location = 2) out vec3 NormalOut;
layout (location = 3) out vec3 TexCoordOut;

layout (binding = 0) uniform sampler2D diffuseSampler;
layout (binding = 1) uniform sampler2D normalSampler;

uniform mat4 view;
uniform bool hasNormalMap;

#define VIEWSPACE_NORMALS 1

mat3 GetTBN(vec3 pos, vec2 uv, vec3 normal)
{
    vec3 dp1 = dFdx(pos);
    vec3 dp2 = dFdy(pos);
    vec2 duv1 = dFdx(uv);
    vec2 duv2 = dFdy(uv);

    float r = 1.0 / (duv1.x * duv2.y - duv1.y * duv2.x);
    vec3 tangent   = normalize((dp1 * duv2.y - dp2 * duv1.y) * r);
    vec3 bitangent = normalize((dp2 * duv1.x - dp1 * duv2.x) * r);

    // Orthonormalize to avoid accumulated floating-point drift
    tangent   = normalize(tangent - normal * dot(normal, tangent));
    bitangent = normalize(bitangent - normal * dot(normal, bitangent));

    return mat3(tangent, bitangent, normalize(normal));
}

void main()
{
    vec4 diffuseTex = texture(diffuseSampler, frag_texCoord * vec2(1.0, -1.0));

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
    
    if (VIEWSPACE_NORMALS == 1) {
        normal = normalize((view * vec4(normal, 0.0)).xyz);
    }

    WorldPosOut = frag_position;
    DiffuseOut = diffuseTex;
    NormalOut = normalize(normal);
    TexCoordOut = vec3(frag_texCoord, 0.0);
}