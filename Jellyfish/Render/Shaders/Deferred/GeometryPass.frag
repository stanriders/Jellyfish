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

mat3 GetTBN(vec3 fragPos, vec2 texCoord, vec3 worldNormal)
{
    vec3 p_dx = dFdx(fragPos);
    vec3 p_dy = dFdy(fragPos);

    vec2 tc_dx = dFdx(texCoord);
    vec2 tc_dy = dFdy(texCoord);

    vec3 tangent = normalize( tc_dy.y * p_dx - tc_dx.y * p_dy );
    vec3 bitangent = normalize( tc_dy.x * p_dx - tc_dx.x * p_dy ); // sign inversion

    vec3 normal = normalize(worldNormal);
    vec3 x = cross(normal, tangent);
    tangent = cross(x, normal);
    tangent = normalize(tangent);

    // get updated bi-tangent
    x = cross(bitangent, normal);
    bitangent = cross(normal, x);
    bitangent = normalize(bitangent);

    return mat3(tangent, bitangent, normal);
}

void main()
{
    vec4 diffuseTex = texture(diffuseSampler, frag_texCoord * vec2(1.0, -1.0));
    vec3 normalTex = texture(normalSampler, frag_texCoord * vec2(1.0, -1.0)).rgb;

    vec3 tangentSpaceNormal = normalize(normalTex * 2.0 - 1.0);
    mat3 tbn = GetTBN(frag_position, frag_texCoord, frag_normal);
    tangentSpaceNormal = normalize(tbn * tangentSpaceNormal);  

    WorldPosOut = frag_position;
    DiffuseOut = diffuseTex;
    NormalOut = tangentSpaceNormal;
    TexCoordOut = vec3(frag_texCoord, 0.0);
}