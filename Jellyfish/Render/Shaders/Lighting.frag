
#include LightingDefinitions.shared

// general PCF
float InterleavedGradientNoise(vec2 uv)
{
    const float a = 0.06711056;
    const float b = 0.00583715;
    const float c = 52.9829189;
    return fract(c * fract(dot(uv, vec2(a, b))));
}

float PoissonPCF(sampler2D depthMap, vec3 projCoords, float filterRadiusUV)
{
    float shadow = 0.0;

    float noise = InterleavedGradientNoise(gl_FragCoord.xy);
    float angle = noise * PI; // 0..2pi
    mat2 rot = mat2(cos(angle), -sin(angle), sin(angle), cos(angle));

    for (int i = 0; i < PCSS_FILTER_SIZE; i++)
    {
        vec2 offset = rot * poissonDisk[i] * filterRadiusUV;
        float shadowDepth = texture(depthMap, projCoords.xy + offset).r;

        shadow += step(projCoords.z, shadowDepth);
    }

    return shadow / float(PCSS_FILTER_SIZE);
}

// PCSS
void FindBlocker4x4
(
    out float avgBlockerDepth,
    out float numBlockers,
    sampler2D depthMap, vec2 uv, float zReceiver, float zNear, float lightSizeUV
)
{
    //This uses similar triangles to compute what //area of the shadow map we should search
    float searchWidth = lightSizeUV * (zReceiver - zNear) / zNear;

    float blockerSum = 0;
    numBlockers = 0;

    for (int i = 0; i < PCSS_FILTER_SIZE; i++)
    {
        float shadowMapDepth = texture( depthMap, uv + poissonDisk[i] * searchWidth ).r;
        if ( shadowMapDepth > zReceiver ) 
        {
            blockerSum += shadowMapDepth;
            numBlockers++;
        }
    }

    avgBlockerDepth = blockerSum / numBlockers;
}

float PenumbraSize( float zReceiver, float zBlocker ) //Parallel plane estimation
{
    return (zReceiver - zBlocker) / zBlocker;
}

// shadowing options
float PoissonPCSSShadow( sampler2D depthMap, vec3 uvw, float zNear, float lightSizeUV )
{
    float avgBlockerDepth = 0;
    float numBlockers = 0;

    FindBlocker4x4( avgBlockerDepth, numBlockers, depthMap, uvw.xy, uvw.z, zNear, lightSizeUV );

    float flOut = 0.0f;
    
    if( numBlockers >= 1 )
    {
        // STEP 2: penumbra size
        float penumbraRatio = PenumbraSize( uvw.z, avgBlockerDepth );
        float filterRadiusUV = penumbraRatio * lightSizeUV / uvw.z;

        flOut = PoissonPCF( depthMap, uvw, filterRadiusUV );
    }

    return flOut;
}

float PoissonPCFShadow(sampler2D depthMap, vec3 projCoords, float baseRadius)
{
    float texelSize = 1.0 / textureSize(depthMap, 0).x;
    float filterRadiusUV = baseRadius * texelSize;

    return PoissonPCF(depthMap, projCoords, filterRadiusUV);
}

float SimplePCFShadow(sampler2D DepthSampler, vec3 projCoords, int halfkernelWidth)
{
    float currentDepth = projCoords.z;
    float shadow = 0.0f;

    vec2 texelSize = vec2(1.0f / textureSize(DepthSampler, 0).x);
    for(int x = -halfkernelWidth; x <= halfkernelWidth; ++x)
    {
        for(int y = -halfkernelWidth; y <= halfkernelWidth; ++y)
        {
            float pcfDepth = texture(DepthSampler, vec2(projCoords.xy + vec2(x, y) * texelSize)).r;
            shadow += step(currentDepth, pcfDepth);
        }
    }
    shadow /= ((halfkernelWidth*2+1)*(halfkernelWidth*2+1));

    return shadow;
}

float SimpleShadow(sampler2D DepthSampler, vec3 projCoords)
{
    float currentDepth = projCoords.z;

    float shadow = texture(DepthSampler, projCoords.xy).r;  

    return step(currentDepth, shadow);
}  

float ShadowCalculation(int lightIndex, vec3 lightDir, vec3 normal)
{
    vec4 fragPosLightSpace = lightSources[lightIndex].lightSpaceMatrix * vec4(frag_position, 1.0);
    vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;

    projCoords = projCoords * 0.5 + 0.5;
    
    if(projCoords.z > 1.0)
        return 0.0;
        
    sampler2D shadow = sampler2D(lightSources[lightIndex].shadow);

    if (lightSources[lightIndex].usePcss)
        return PoissonPCSSShadow(shadow, projCoords, lightSources[lightIndex].near, 6.0f);

    return PoissonPCFShadow(shadow, projCoords, 2.0f);
    //return SimplePCFShadow(shadow, projCoords, 4);
    //return SimpleShadow(shadow, projCoords);
}  

// lights
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

        if (projCoords.x >= 0.0 || projCoords.x <= 1.0 ||
            projCoords.y >= 0.0 || projCoords.y <= 1.0 ||
            projCoords.z >= 0.0 || projCoords.z < 1.0)
        {
            sampler2D shadowSampler = sampler2D(sun.shadow[layer]);
            if (sun.usePcss && layer == 0)
            {
                shadow = PoissonPCSSShadow(shadowSampler, projCoords, sun.cascadeNear[layer], 0.1f);
            }
            else
            {
                shadow = PoissonPCFShadow(shadowSampler, projCoords, layer == 0 ? 4.0f : 1.0f);
                //shadow = SimplePCFShadow(shadowSampler, projCoords, layer == 0 ? 4.0f : 1.0f);
                //shadow = SimpleShadow(shadowSampler, projCoords);
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
