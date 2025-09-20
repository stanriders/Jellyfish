#version 460
#include CommonFrag.frag
#include Shadowmapping.frag

out vec4 outputColor;

in vec2 frag_texCoord;
in vec3 frag_normal;
in vec3 frag_position;
in float frag_clipspaceZ;

layout(binding=0) uniform sampler2D diffuseSampler;
layout(binding=1) uniform sampler2D normalSampler;
layout(binding=2) uniform sampler2D metroughSampler;
layout(binding=3) uniform samplerCube prefilterMap;
layout(binding=4) uniform samplerCube irradianceMap;

layout(binding=SUN_SAMPLER_BINDING) uniform sampler2D sunShadowSampler[CSM_CASCADES];
layout(binding=SUN_SAMPLER_BINDING + CSM_CASCADES + 1) uniform sampler2D shadowSamplers[MAX_LIGHTS];

struct Light {
    vec3 position;
    vec3 direction;
    mat4 lightSpaceMatrix;
    int type;

    float constant;
    float linear;
    float quadratic;
    float cone;
    float outcone;

    float brightness;

    vec3 ambient;
    vec3 diffuse;
    bool hasShadows;

    float near;
    float far;
    bool usePcss;
};
uniform Light lightSources[MAX_LIGHTS];
uniform int lightSourcesCount;

struct Sun {
    vec3 direction;
    mat4 lightSpaceMatrix[CSM_CASCADES];

    float brightness;
    float cascadeFar[CSM_CASCADES];
    float cascadeNear[CSM_CASCADES];

    vec3 ambient;
    vec3 diffuse;
    bool hasShadows;
    bool usePcss;
};

uniform Sun sun;
uniform bool sunEnabled;

uniform mat4 view;
uniform vec3 cameraPos;
uniform bool useNormals;
uniform bool usePbr;
uniform bool useTransparency;
uniform int prefilterMips;
uniform bool iblEnabled;

struct LightContrib {
    vec3 ambient;
    vec3 direct;
};

const float PI = 3.14159265359;

// late include to make sure we have all the uniforms
#include Pbr.frag

float ShadowCalculation(int lightIndex, vec3 lightDir, vec3 normal)
{
    vec4 fragPosLightSpace = lightSources[lightIndex].lightSpaceMatrix * vec4(frag_position, 1.0);
    vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;

    projCoords = projCoords * 0.5 + 0.5;
    
    if(projCoords.z > 1.0)
        return 0.0;
        
    if (lightSources[lightIndex].usePcss)
        return ShadowColor_PCSS4X4_PCF4X4(shadowSamplers[lightIndex], projCoords, lightSources[lightIndex].near, 3f);

    return PoissonPCF(shadowSamplers[lightIndex], projCoords, 2f);
    //return SimplePCF(shadowSamplers[lightIndex], projCoords, 4);
    //return SimpleShadow(shadowSamplers[lightIndex], projCoords);
}  

LightContrib CalcPointLight(int lightIndex, vec3 normal, vec3 fragPos, vec3 viewDir)
{
    Light light = lightSources[lightIndex];
    vec3 lightDir = normalize(light.position - fragPos);

    vec3 ambient = light.ambient;
    vec3 outdiffuse = light.diffuse * light.brightness;

    float distanceToLight = length(light.position - fragPos);
    float attenuation = 255.0 / (light.constant + 
                        light.linear * distanceToLight + 
                        light.quadratic * (distanceToLight * distanceToLight));

    ambient  *= attenuation;
    outdiffuse *= attenuation;

    float shadow = 1.0f;
    if (light.hasShadows) 
    {
        shadow = ShadowCalculation(lightIndex, lightDir, normal);
    }

    LightContrib result;
    result.ambient = ambient;
    result.direct = outdiffuse * shadow;
    return result;
}

LightContrib CalcSpotlight(int lightIndex, vec3 normal, vec3 fragPos, vec3 viewDir)
{
    Light light = lightSources[lightIndex];
    vec3 lightDir = normalize(light.position - fragPos);

    vec3 ambient = light.ambient;
    vec3 outdiffuse = light.diffuse * light.brightness;

    float distanceToLight = length(light.position - fragPos);
    float attenuation = 255.0 / (light.constant + 
                        light.linear * distanceToLight + 
                        light.quadratic * (distanceToLight * distanceToLight));

    ambient  *= attenuation;
    outdiffuse  *= attenuation;

    float theta = dot(lightDir, normalize(-light.direction));
    float epsilon   = light.cone - light.outcone;
    float intensity = clamp((theta - light.outcone) / epsilon, 0.0, 1.0); 

    outdiffuse *= intensity;
    ambient *= intensity;

    float shadow = 1.0f;
    if (light.hasShadows) 
    {
        shadow = ShadowCalculation(lightIndex, lightDir, normal);
    }

    LightContrib result;
    result.ambient = ambient;
    result.direct = outdiffuse * shadow;
    return result;
}

LightContrib CalcSun(vec3 normal, vec3 fragPos, vec3 viewDir)
{
    vec3 lightDir = normalize(-sun.direction);

    vec3 outdiffuse = sun.diffuse * sun.brightness;

    float shadow = 1.0f;
    if (sun.hasShadows) 
    {
        vec4 fragPosViewSpace = view * vec4(frag_position, 1.0);
        float depthValue = frag_clipspaceZ;

        int layer = -1;
        for (int i = 0; i < CSM_CASCADES; ++i)
        {
            if (depthValue < sun.cascadeFar[i])
            {
                layer = i;
                break;
            }
        }

        if (layer == -1)
        {
            layer = CSM_CASCADES;
        }

        vec4 frag_position_sun = sun.lightSpaceMatrix[layer] * vec4(frag_position, 1.0);
        vec3 projCoords = frag_position_sun.xyz / frag_position_sun.w;

        projCoords = projCoords * 0.5 + 0.5;

        if(projCoords.z < 1.0)
        {
            if (sun.usePcss && layer == 0)
            {
                shadow = ShadowColor_PCSS4X4_PCF4X4(sunShadowSampler[layer], projCoords, sun.cascadeNear[layer], 0.075f);
            }
            else
            {
                shadow = PoissonPCF(sunShadowSampler[layer], projCoords, layer == 0 ? 4f : 1f);
                //shadow = SimplePCF(sunShadowSampler[layer], projCoords, layer == 0 ? 4f : 1f);
                //shadow = SimpleShadow(sunShadowSampler[layer], projCoords);
            }

#ifdef CSM_DEBUG
            if (layer == 0)
                outdiffuse *= vec3(0,10,0);
            else if (layer == 1)
                outdiffuse *= vec3(10,0,0);
            else if (layer == 2)
                outdiffuse *= vec3(0,0,10);
            else if (layer == 3)
                outdiffuse *= vec3(10,10,10);
#endif
        }
    }

    LightContrib result;
    result.ambient = sun.ambient;
    result.direct = outdiffuse * shadow;
    return result;
}

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
    if (usePbr && iblEnabled) 
    {
        lighting += ComputeIBL(normal, viewDir, diffuseTex.rgb, roughness, metalness);
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

