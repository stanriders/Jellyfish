#version 460
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

    vec3 ambient;
    vec3 diffuse;
    bool hasShadows;
};

uniform Sun sun;
uniform bool sunEnabled;

uniform mat4 view;
uniform vec3 cameraPos;
uniform bool useNormals;
uniform bool usePbr;
uniform bool alphaTest;
uniform int prefilterMips;
uniform bool iblEnabled;

struct LightContrib {
    vec3 ambient;
    vec3 direct;
};

const float PI = 3.14159265359;

float DistributionGGX(vec3 N, vec3 H, float roughness)
{
    float alpha      = roughness*roughness;
    float alphaSq     = alpha*alpha;
    float NdotH  = max(dot(N, H), 0.0);
    float NdotHSq = NdotH*NdotH;
	
    float denom = (NdotHSq * (alphaSq - 1.0) + 1.0);
    return alphaSq / (PI * denom * denom);
}

float GeometrySchlickGGX(float NdotV, float roughness)
{
    float r = (roughness + 1.0);
    float k = (r*r) / 8.0;

    float num   = NdotV;
    float denom = NdotV * (1.0 - k) + k;
	
    return num / denom;
}

float GeometrySmith(vec3 N, vec3 V, vec3 L, float roughness)
{
    float NdotV = max(dot(N, V), 0.0);
    float NdotL = max(dot(N, L), 0.0);
    float ggx2  = GeometrySchlickGGX(NdotV, roughness);
    float ggx1  = GeometrySchlickGGX(NdotL, roughness);
	
    return ggx1 * ggx2;
}

vec3 fresnelSchlick(float cosTheta, vec3 F0)
{
    return F0 + (1.0 - F0) * pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0);
}

float ShadowCalculation(int lightIndex, vec3 lightDir, vec3 normal)
{
    vec4 fragPosLightSpace = lightSources[lightIndex].lightSpaceMatrix * vec4(frag_position, 1.0);
    vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;

    projCoords = projCoords * 0.5 + 0.5;
    
    if(projCoords.z > 1.0)
        return 0.0;
        
    if (lightSources[lightIndex].usePcss)
        return ShadowColor_PCSS4X4_PCF4X4(shadowSamplers[lightIndex], projCoords, lightSources[lightIndex].near, lightSources[lightIndex].far, 0.0003f, true);

    return SimplePCF(shadowSamplers[lightIndex], projCoords, 4);
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
            shadow = SimplePCF(sunShadowSampler[layer], projCoords, layer == 0 ? 4 : 1);
            //shadow = SimpleShadow(sunShadowSampler[layer], projCoords);
            //shadow = ShadowColor_PCSS4X4_PCF4X4(sunShadowSampler[layer], projCoords, sun.cascadeNear[layer], sun.cascadeFar[layer], 0.01f, false);
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

vec2 integrateBRDFApprox(float NdotV, float roughness)
{
    const vec4 c0 = vec4(-1.0, -0.0275, -0.572, 0.022);
    const vec4 c1 = vec4( 1.0,  0.0425,  1.04, -0.04);
    vec4 r = roughness * c0 + c1;
    float a004 = min(r.x * r.x, exp2(-9.28 * NdotV)) * r.x + r.y;
    return vec2(-1.04, 1.04) * a004 + r.zw;
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
    
    vec3 metroughTex = texture(metroughSampler, frag_texCoord * vec2(1.0, -1.0)).rgb;
    float metalness = metroughTex.b;
    float roughness = metroughTex.g;
   
    vec3 dielectricCoefficient = vec3(0.04);  //F0 dielectric
    dielectricCoefficient = mix(dielectricCoefficient, diffuseTex.rgb, metalness);

    vec3 result = vec3(0.0, 0.0, 0.0);
    
    vec3 lighting = vec3(0);
    vec3 ibl = vec3(0);

    if (usePbr && iblEnabled) 
    {
        vec3 F0 = mix(vec3(0.04), diffuseTex.rgb, metalness);
        float NdotV = max(dot(normal, viewDir), 0.0);

        // Fresnel for environment uses N·V (not H)
        vec3 F_env = fresnelSchlick(NdotV, F0);
        vec3 kS_env = F_env;
        vec3 kD_env = (1.0 - kS_env) * (1.0 - metalness);

        // Diffuse IBL (irradiance map must be preconvolved)
        vec3 diffuseIBL = texture(irradianceMap, normal).rgb * diffuseTex.rgb;

        // Specular IBL
        vec3 R = normalize(reflect(-viewDir, normal));

        vec3 prefilteredColor = textureLod(prefilterMap, R, roughness * prefilterMips).rgb;
        vec2 brdf = integrateBRDFApprox(NdotV, roughness);
        vec3 specularIBL = prefilteredColor * (F_env * brdf.x + brdf.y);

        ibl = kD_env * diffuseIBL + specularIBL;
    }

    for(int i = 0; i < lightSourcesCount; i++)
    {
        Light light = lightSources[i];
        vec3 lightDir = normalize(light.position - frag_position);

        float NdotL = max(dot(normal, lightDir), 0.0);    

        vec3 specular = vec3(0);
        vec3 kD = vec3(0);
        if (usePbr)
        {
            // cook-torrance brdf
            vec3 H = normalize(viewDir + lightDir);
            float NDF = DistributionGGX(normal, H, roughness);        
            float G   = GeometrySmith(normal, viewDir, lightDir, roughness);      
            vec3 F    = fresnelSchlick(max(dot(H, viewDir), 0.0), dielectricCoefficient);      
    
            vec3 kS = F;
            kD = vec3(1.0) - kS;
            kD *= 1.0 - metalness;
        
            vec3 numerator    = NDF * G * F;
            float denominator = max(0.00001, 4.0 * max(dot(normal, viewDir), 0.0) * NdotL);
            specular          = numerator / denominator; 
        }
        
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

        if (usePbr)
        {
            // add to outgoing radiance Lo
            vec3 diffuseBDR = diffuseTex.rgb;
            lighting += kD * diffuseBDR * lightContrib.ambient;
            lighting += max(vec3(0), (kD * diffuseBDR + specular) * lightContrib.direct * NdotL); 
        }
        else 
        {
            lighting += (lightContrib.direct + lightContrib.ambient) * NdotL;
        }
    }

    if (sunEnabled) 
    {
        vec3 lightDir = normalize(-sun.direction);
        float NdotL = max(dot(normal, lightDir), 0.0);

        vec3 specular = vec3(0);
        vec3 kD = vec3(0);
        if (usePbr)
        {
            // cook-torrance brdf
            vec3 H = normalize(viewDir + lightDir);
            float NDF = DistributionGGX(normal, H, roughness);
            float G   = GeometrySmith(normal, viewDir, lightDir, roughness);
            vec3 F    = fresnelSchlick(max(dot(H, viewDir), 0.0), dielectricCoefficient);

            vec3 kS = F;
            kD = vec3(1.0) - kS;
            kD *= 1.0 - metalness;

            vec3 numerator    = NDF * G * F;
            float denominator = max(0.00001, 4.0 * max(dot(normal, viewDir), 0.0) * NdotL);
            specular          = numerator / denominator; 
        }

        LightContrib lightContrib = CalcSun(normal, frag_position, viewDir);

        if (usePbr)
        {
            // add to outgoing radiance Lo
            vec3 diffuseBDR = diffuseTex.rgb;
            lighting += kD * diffuseBDR * lightContrib.ambient;
            lighting += max(vec3(0), (kD * diffuseBDR + specular) * (lightContrib.direct * NdotL)); 
        }
        else 
        {
            lighting += sun.ambient;
            lighting += lightContrib.direct * NdotL;
        }
    }

    if (usePbr) {
        lighting += ibl;
    }

    result = lighting;

    if (!usePbr)
    {
        result *= diffuseTex.rgb;
    }

    if (!alphaTest)
        outputColor = vec4(result, 1.0);
    else
        outputColor = vec4(result, diffuseTex.a);

}

