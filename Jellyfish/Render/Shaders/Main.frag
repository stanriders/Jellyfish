#version 460

out vec4 outputColor;

#define MAX_LIGHTS 12
#define CSM_CASCADES 4
#define SUN_SAMPLER_BINDING 3
//#define CSM_DEBUG

in vec2 frag_texCoord;
in vec3 frag_normal;
in vec3 frag_position;
in float frag_clipspaceZ;

layout(binding=0) uniform sampler2D diffuseSampler;
layout(binding=1) uniform sampler2D normalSampler;
layout(binding=2) uniform sampler2D metroughSampler;

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

#define PCSS_FILTER_SIZE 16
vec2 poissonDisk[PCSS_FILTER_SIZE] = 
{
		vec2( -0.94201624, -0.39906216 ),
		vec2( 0.94558609, -0.76890725 ),
		vec2( -0.094184101, -0.92938870 ),
		vec2( 0.34495938, 0.29387760 ),
		vec2( -0.91588581, 0.45771432 ),
		vec2( -0.81544232, -0.87912464 ),
		vec2( -0.38277543, 0.27676845 ),
		vec2( 0.97484398, 0.75648379 ),
		vec2( 0.44323325, -0.97511554 ),
		vec2( 0.53742981, -0.47373420 ),
		vec2( -0.26496911, -0.41893023 ),
		vec2( 0.79197514, 0.19090188 ),
		vec2( -0.24188840, 0.99706507 ),
		vec2( -0.81409955, 0.91437590 ),
		vec2( 0.19984126, 0.78641367 ),
		vec2( 0.14383161, -0.14100790 )
};

float LinearizeDepth(float d, float near, float far) {
    float z_ndc = d * 2.0 - 1.0;
    return (2.0 * near) / (far + near - z_ndc * (far - near));
}

void FindBlocker4x4
(
	out float avgBlockerDepth,
	out float numBlockers,
	sampler2D depthMap, vec2 uv, float zReceiver, float zNear, float zFar, float lightSizeUV, bool linearDepth
)
{
	//This uses similar triangles to compute what //area of the shadow map we should search
	float searchWidth = lightSizeUV * (zReceiver - zNear) / zNear;

	float blockerSum = 0;
	numBlockers = 0;

    for (int i = 0; i < PCSS_FILTER_SIZE; i++)
    {
        float shadowMapDepth = texture( depthMap, uv + poissonDisk[i] * searchWidth ).r;
        if (linearDepth)
            shadowMapDepth = LinearizeDepth(shadowMapDepth, zNear, zFar);
        if ( shadowMapDepth < zReceiver ) 
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

float PCFForPCSS4X4( vec2 uv, sampler2D depthMap, float zReceiver, float zNear, float zFar, float filterRadiusUV, bool linearDepth )
{
    float sum = 0;
    
    for (int i = 0; i < PCSS_FILTER_SIZE; i++)
    {
        float shadowMapDepth = texture( depthMap, uv + poissonDisk[i] * filterRadiusUV ).r;
        if (linearDepth)
            shadowMapDepth = LinearizeDepth(shadowMapDepth, zNear, zFar);

        sum += shadowMapDepth < zReceiver ? 0.0625 : 0;
    }
	
	return sum;
}

float ShadowColor_PCSS4X4_PCF4X4( sampler2D depthMap, vec3 uvw, float zNear, float zFar, float lightSizeUV, bool linearDepth )
{
	float avgBlockerDepth = 0;
	float numBlockers = 0;

	FindBlocker4x4( avgBlockerDepth, numBlockers, depthMap, uvw.xy, uvw.z, zNear, zFar, lightSizeUV, linearDepth );

	float flOut = 0.0f;
	
	if( numBlockers >= 1 )
	{
		// STEP 2: penumbra size
		float penumbraRatio = PenumbraSize( uvw.z, avgBlockerDepth );
		float filterRadiusUV = penumbraRatio * lightSizeUV / uvw.z;

		flOut = PCFForPCSS4X4( uvw.xy, depthMap, uvw.z, zNear, zFar, filterRadiusUV, linearDepth );
	}

	return 1 - flOut;
}

float SimpleShadow(sampler2D DepthSampler, vec3 projCoords)
{
    float currentDepth = projCoords.z;

    float shadow = texture(DepthSampler, projCoords.xy).r;  

    return currentDepth < shadow ? 1.0 : 0.0;
}  

float SimplePCF(sampler2D DepthSampler, vec3 projCoords, int halfkernelWidth)
{
    float currentDepth = projCoords.z;

    float shadow = 0.0f;
    vec2 texelSize = vec2(1.0f / textureSize(DepthSampler, 0).x);
    for(int x = -halfkernelWidth; x <= halfkernelWidth; ++x)
    {
	    for(int y = -halfkernelWidth; y <= halfkernelWidth; ++y)
	    {
		    float pcfDepth = texture(DepthSampler, vec2(projCoords.xy + vec2(x, y) * texelSize)).r;
		    shadow += currentDepth < pcfDepth ? 1.0 : 0.0;
	    }
    }
    shadow /= ((halfkernelWidth*2+1)*(halfkernelWidth*2+1));

    return shadow;
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

