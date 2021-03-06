﻿#version 460

out vec4 outputColor;

in vec2 frag_texCoord;
in vec3 frag_normal;
in vec3 frag_position;

uniform sampler2D diffuseSampler;
uniform sampler2D normalSampler;
uniform vec3 cameraPos;

struct Light {
    vec3 position;

    float constant;
    float linear;
    float quadratic;

    vec3 ambient;
    vec3 diffuse;
};  
uniform Light lightSources[4];
uniform int lightSourcesCount;

vec3 CalcPointLight(Light light, vec3 normal, vec3 fragPos, vec3 viewDir)
{
    vec3 lightDir = normalize(light.position - fragPos);

    //diffuse shading
    float diff = max(dot(normal, lightDir), 0.0);

    vec3 ambient = light.ambient;
    vec3 outdiffuse = light.diffuse * diff;

    //attenuation
    float distanceToLight = length(light.position - fragPos);
    float attenuation = 255.0 / (light.constant + 
                        light.linear * distanceToLight + 
                        light.quadratic * (distanceToLight * distanceToLight));

    ambient  *= attenuation;
    outdiffuse  *= attenuation;

    return (ambient + outdiffuse);
}

vec3 CalcLighting(vec3 normal, vec3 fragPos, vec3 viewDir)
{
    vec3 result = vec3(0.0, 0.0, 0.0);

    for(int i = 0; i < lightSourcesCount; i++)
        result += CalcPointLight(lightSources[i], normal, fragPos, viewDir);

    return result;
}

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

    vec3 viewDir = normalize(cameraPos - frag_position);

    vec3 result = diffuseTex.rgb;
    vec3 lighting = CalcLighting(frag_normal, frag_position, viewDir);
    result *= lighting;

    outputColor = vec4(result, 1.0);
}

