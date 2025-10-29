#version 460
#extension GL_ARB_bindless_texture : require
#extension GL_ARB_gpu_shader_int64 : require
#include CommonFrag.frag

out vec4 outputColor;

in vec2 frag_texCoord;
in vec3 frag_normal;
in vec3 frag_position;
in float frag_clipspaceZ;

layout(binding=0) uniform sampler2D diffuseSampler;
layout(binding=1) uniform sampler2D normalSampler;
layout(binding=2) uniform sampler2D metroughSampler;
layout(binding=3) uniform sampler2D reflectionMap;

uniform mat4 view;
uniform vec3 cameraPos;
uniform bool useNormals;
uniform bool usePbr;
uniform bool useTransparency;
uniform int prefilterMips;
uniform bool iblEnabled;
uniform bool iblPrefilterEnabled;
uniform bool sslrEnabled;
uniform vec2 screenSize;

// late include to make sure we have all the uniforms
#include Lighting.frag
#include Pbr.frag

vec3 ApplyLight(LightContrib lc, vec3 diffuseColor, vec3 L, vec3 N, vec3 V, vec3 F0, float roughness, float metalness)
{
    float NdotL = max(dot(N, L), 0.0);

    if (usePbr)
    {
        BRDFResult brdf = ComputeBRDF(N, V, L, F0, roughness, metalness);

        vec3 diffuse = brdf.kD * diffuseColor;
        return diffuse * lc.ambient + max(vec3(0.0), (diffuse + brdf.specular) * lc.direct * NdotL);
    }
    else
    {
        return (lc.direct + lc.ambient) * NdotL;
    }
}

void main()
{
    vec4 diffuseTex = texture(diffuseSampler, frag_texCoord * vec2(1.0, -1.0));
    //if (alphaTest && diffuseTex.a < 0.5)
    //    discard;

    vec3 normal = normalize(frag_normal);
    if (useNormals)
    {
        vec3 normalTex = texture(normalSampler, frag_texCoord * vec2(1.0, -1.0)).rgb;

        vec3 tangentSpaceNormal = normalize(normalTex * 2.0 - 1.0);
        mat3 tbn = GetTBN(frag_position, frag_texCoord, frag_normal);
        normal = normalize(tbn * tangentSpaceNormal);  
    }

    vec3 viewDir = normalize(cameraPos - frag_position);
    
    vec3 metroughTex = vec3(0);
    if (usePbr)
        metroughTex = texture(metroughSampler, frag_texCoord * vec2(1.0, -1.0)).rgb;

    float metalness = metroughTex.b;
    float roughness = metroughTex.g;

    vec3 lighting = vec3(0);
    if (usePbr) 
    {
        lighting += ComputeIBL(normal, viewDir, diffuseTex.rgb, roughness, metalness, gl_FragCoord.xy / screenSize);
    }
    
    vec3 dielectricCoefficient = mix(vec3(0.04), diffuseTex.rgb, metalness); // F0

    for(int i = 0; i < lightSourcesCount; i++)
    {
        Light light = lightSources[i];
        vec3 lightDir = normalize(light.position - frag_position);

        LightContrib lightContrib;
        switch(lightSources[i].type)
        {
            case 0: // point
            {
              lightContrib = CalcPointLight(i, normal, frag_position, viewDir);
              break;
            }
            case 1: // spot
            {
              lightContrib = CalcSpotlight(i, normal, frag_position, viewDir);
              break;
            }
        }

        lighting += ApplyLight(lightContrib, diffuseTex.rgb, lightDir, normal, viewDir, dielectricCoefficient, roughness, metalness);
    }

    if (sunEnabled) 
    {
        vec3 lightDir = normalize(-sun.direction);
        LightContrib lightContrib = CalcSun(normal, frag_position, viewDir);

        lighting += ApplyLight(lightContrib, diffuseTex.rgb, lightDir, normal, viewDir, dielectricCoefficient, roughness, metalness);
    }

    if (!usePbr)
        lighting *= diffuseTex.rgb;

    if (!useTransparency)
        outputColor = vec4(lighting, 1.0);
    else
        outputColor = vec4(lighting, diffuseTex.a);
}

