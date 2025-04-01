#version 460

out vec4 outputColor;

in vec2 frag_texCoord;
in vec3 frag_normal;
in vec3 frag_position;
in vec4 frag_position_sun;
in vec4 frag_position_lightspace[4];

uniform vec3 cameraPos;
layout(binding=0) uniform sampler2D diffuseSampler;
layout(binding=1) uniform sampler2D normalSampler;
layout(binding=2) uniform sampler2D metroughSampler;

layout(binding=3) uniform sampler2DShadow sunShadowSampler;
layout(binding=4) uniform sampler2DShadow shadowSamplers[4];

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
};
uniform Light lightSources[4];
uniform int lightSourcesCount;

uniform Light sun;
uniform bool sunEnabled;

uniform bool useNormals;
uniform bool usePbr;
uniform bool alphaTest;

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

float SimpleShadow(sampler2DShadow DepthSampler, vec3 projCoords)
{
    float currentDepth = projCoords.z;

    float shadow = texture(DepthSampler, vec3(projCoords.xy, currentDepth));  

    return shadow;
}  

float SimplePCF(sampler2DShadow DepthSampler, vec3 projCoords)
{
    float currentDepth = projCoords.z;

    float shadow = 0.0f;
    vec2 texelSize = vec2(1.0f / textureSize(DepthSampler, 0).x);
    const int halfkernelWidth = 3;
    for(int x = -halfkernelWidth; x <= halfkernelWidth; ++x)
    {
	    for(int y = -halfkernelWidth; y <= halfkernelWidth; ++y)
	    {
		    float pcfDepth = texture(DepthSampler, vec3(projCoords.xy + vec2(x, y) * texelSize, currentDepth));
		    shadow += pcfDepth;
	    }
    }
    shadow /= ((halfkernelWidth*2+1)*(halfkernelWidth*2+1));

    return shadow;
}

float ShadowCalculation(int lightIndex, vec3 lightDir, vec3 normal)
{
    vec4 fragPosLightSpace = frag_position_lightspace[lightIndex];
    vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;

    projCoords = projCoords * 0.5 + 0.5;
    
    if(projCoords.z > 1.0)
        return 0.0;
        
    //return SimplePCF(shadowSamplers[lightIndex], projCoords);
    return SimpleShadow(shadowSamplers[lightIndex], projCoords);
}  

vec3 CalcPointLight(int lightIndex, vec3 normal, vec3 fragPos, vec3 viewDir)
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

    float shadow = 1f;
    if (light.hasShadows) 
    {
        shadow = ShadowCalculation(lightIndex, lightDir, normal);
    }

    return ambient + outdiffuse * shadow;
}

vec3 CalcSpotlight(int lightIndex, vec3 normal, vec3 fragPos, vec3 viewDir)
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

    float shadow = 1f;
    if (light.hasShadows) 
    {
        shadow = ShadowCalculation(lightIndex, lightDir, normal);
    }

    return ambient + outdiffuse * shadow;
}

vec3 CalcSun(vec3 normal, vec3 fragPos, vec3 viewDir)
{
    vec3 lightDir = normalize(-sun.direction);

    vec3 outdiffuse = sun.diffuse * sun.brightness;

    float shadow = 1f;
    if (sun.hasShadows) 
    {
        vec3 projCoords = frag_position_sun.xyz / frag_position_sun.w;

        projCoords = projCoords * 0.5 + 0.5;
    
        if(projCoords.z < 1.0)
        {
            //shadow = SimplePCF(sunShadowSampler, projCoords);
            shadow = SimpleShadow(sunShadowSampler, projCoords);
        }
    }

    return outdiffuse * shadow;
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
    for(int i = 0; i < lightSourcesCount; i++)
    {
        Light light = lightSources[i];
        vec3 lightDir = normalize(light.position - frag_position);
        if (lightSources[i].type == 1) // sun
            lightDir = normalize(-light.direction);

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
        
        vec3 radiance = vec3(0);
        switch(lightSources[i].type)
        {
            case 0: // point
            {
              radiance = CalcPointLight(i, normal, frag_position, viewDir);
              break;
            }
            case 1: // spot
            {
              radiance = CalcSpotlight(i, normal, frag_position, viewDir);
              break;
            }
        }

        if (usePbr)
        {
            // add to outgoing radiance Lo
            vec3 diffuseBDR = diffuseTex.rgb;
            lighting += max(vec3(0), (diffuseBDR + specular) * radiance * NdotL); 
        }
        else 
        {
            lighting += radiance * NdotL;
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

        vec3 radiance = CalcSun(normal, frag_position, viewDir);

        if (usePbr)
        {
            // add to outgoing radiance Lo
            vec3 diffuseBDR = diffuseTex.rgb;
            lighting += max(vec3(0), (diffuseBDR + specular) * (radiance * NdotL + sun.ambient)); 
        }
        else 
        {
            lighting += sun.ambient;
            lighting += radiance * NdotL;
        }
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

