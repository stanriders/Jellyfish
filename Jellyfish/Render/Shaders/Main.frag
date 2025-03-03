#version 460

out vec4 outputColor;

in vec2 frag_texCoord;
in vec3 frag_normal;
in vec3 frag_position;
in vec4 frag_position_lightspace[4];

uniform vec3 cameraPos;
layout(binding=0) uniform sampler2D diffuseSampler;
layout(binding=1) uniform sampler2D normalSampler;
layout(binding=2) uniform sampler2D metroughSampler;

layout(binding=3) uniform sampler2D shadow1Sampler;
layout(binding=4) uniform sampler2D shadow2Sampler;
layout(binding=5) uniform sampler2D shadow3Sampler;
layout(binding=6) uniform sampler2D shadow4Sampler;

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

uniform bool useNormals;
uniform bool usePbr;

const float FLASHLIGHT_SHADOW_TEXTURE_RESOLUTION = 2048;
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

float SimpleShadow(sampler DepthSampler, vec3 projCoords, vec3 lightDir, vec3 normal)
{
    float closestDepth = texture(DepthSampler, projCoords.xy).r; 
    float currentDepth = projCoords.z;
    
    float bias = 0f;
    if (gl_FrontFacing)
        bias = max(0.05 * (1.0 - dot(normal, lightDir)), 0.005);

    float shadow = currentDepth - bias > closestDepth  ? 1.0 : 0.0;  

    return shadow;
}  

float SimplePCF(sampler DepthSampler, vec3 projCoords, vec3 lightDir, vec3 normal)
{
    float currentDepth = projCoords.z;

    float bias = 0f;
    if (gl_FrontFacing)
        bias = max(0.05 * (1.0 - dot(normal, lightDir)), 0.005);

    // PCF: percentage-closer filtering
    float shadow = 0.0;
    vec2 texelSize = vec2(1.0 / FLASHLIGHT_SHADOW_TEXTURE_RESOLUTION);
    const int halfkernelWidth = 3;
    for(int x = -halfkernelWidth; x <= halfkernelWidth; ++x)
    {
	    for(int y = -halfkernelWidth; y <= halfkernelWidth; ++y)
	    {
		    float pcfDepth = texture(DepthSampler, projCoords.xy + vec2(x, y) * texelSize).r;
		    shadow += currentDepth - bias > pcfDepth ? 1.0 : 0.0;
	    }
    }
    shadow /= ((halfkernelWidth*2+1)*(halfkernelWidth*2+1));

    return shadow;
}

float DoShadowNvidiaPCF5x5GaussianPC( sampler DepthSampler, vec3 vProjCoords )
{
	float flTexelEpsilon    = 1.0f / FLASHLIGHT_SHADOW_TEXTURE_RESOLUTION;
	float flTwoTexelEpsilon = 2.0f * flTexelEpsilon;

	//float ooW = 1.0f / shadowMapPos.w;								// 1 / w
	vec3 shadowMapCenter_objDepth = vProjCoords;//shadowMapPos.xyz * ooW;		// Do both projections at once

	vec2 shadowMapCenter = shadowMapCenter_objDepth.xy;			// Center of shadow filter
	float objDepth = shadowMapCenter_objDepth.z;					// Object depth in shadow space

	vec4 c0 = vec4( 1.0f / 331.0f, 7.0f / 331.0f, 4.0f / 331.0f, 20.0f / 331.0f );
	vec4 c1 = vec4( 33.0f / 331.0f, 55.0f / 331.0f, -flTexelEpsilon, 0.0f );
	vec4 c2 = vec4( flTwoTexelEpsilon, -flTwoTexelEpsilon, 0.0f, flTexelEpsilon );
	vec4 c3 = vec4( flTexelEpsilon, -flTexelEpsilon, flTwoTexelEpsilon, -flTwoTexelEpsilon );

	vec4 vOneTaps;
	vOneTaps.x = texture2DProj( DepthSampler, vec4( shadowMapCenter + c2.xx, objDepth, 1 ) ).x;	//  2  2
	vOneTaps.y = texture2DProj( DepthSampler, vec4( shadowMapCenter + c2.yx, objDepth, 1 ) ).x;	// -2  2
	vOneTaps.z = texture2DProj( DepthSampler, vec4( shadowMapCenter + c2.xy, objDepth, 1 ) ).x;	//  2 -2
	vOneTaps.w = texture2DProj( DepthSampler, vec4( shadowMapCenter + c2.yy, objDepth, 1 ) ).x;	// -2 -2
	float flSum = dot( vOneTaps, c0.xxxx );

	vec4 vSevenTaps;
	vSevenTaps.x = texture2DProj( DepthSampler, vec4( shadowMapCenter + c2.xz, objDepth, 1 ) ).x;	//  2 0
	vSevenTaps.y = texture2DProj( DepthSampler, vec4( shadowMapCenter + c2.yz, objDepth, 1 ) ).x;	// -2 0
	vSevenTaps.z = texture2DProj( DepthSampler, vec4( shadowMapCenter + c2.zx, objDepth, 1 ) ).x;	// 0 2
	vSevenTaps.w = texture2DProj( DepthSampler, vec4( shadowMapCenter + c2.zy, objDepth, 1 ) ).x;	// 0 -2
	flSum += dot( vSevenTaps, c0.yyyy );

	vec4 vFourTapsA, vFourTapsB;
	vFourTapsA.x = texture2DProj( DepthSampler, vec4( shadowMapCenter + c2.xw, objDepth, 1 ) ).x;	// 2 1
	vFourTapsA.y = texture2DProj( DepthSampler, vec4( shadowMapCenter + c2.wx, objDepth, 1 ) ).x;	// 1 2
	vFourTapsA.z = texture2DProj( DepthSampler, vec4( shadowMapCenter + c3.yz, objDepth, 1 ) ).x;	// -1 2
	vFourTapsA.w = texture2DProj( DepthSampler, vec4( shadowMapCenter + c3.wx, objDepth, 1 ) ).x;	// -2 1
	vFourTapsB.x = texture2DProj( DepthSampler, vec4( shadowMapCenter + c3.wy, objDepth, 1 ) ).x;	// -2 -1
	vFourTapsB.y = texture2DProj( DepthSampler, vec4( shadowMapCenter + c3.yw, objDepth, 1 ) ).x;	// -1 -2
	vFourTapsB.z = texture2DProj( DepthSampler, vec4( shadowMapCenter + c3.xw, objDepth, 1 ) ).x;	// 1 -2
	vFourTapsB.w = texture2DProj( DepthSampler, vec4( shadowMapCenter + c3.zy, objDepth, 1 ) ).x;	// 2 -1
	flSum += dot( vFourTapsA, c0.zzzz );
	flSum += dot( vFourTapsB, c0.zzzz );

	vec4 v20Taps;
	v20Taps.x = texture2DProj( DepthSampler, vec4( shadowMapCenter + c3.xx, objDepth, 1 ) ).x;	// 1 1
	v20Taps.y = texture2DProj( DepthSampler, vec4( shadowMapCenter + c3.yx, objDepth, 1 ) ).x;	// -1 1
	v20Taps.z = texture2DProj( DepthSampler, vec4( shadowMapCenter + c3.xy, objDepth, 1 ) ).x;	// 1 -1
	v20Taps.w = texture2DProj( DepthSampler, vec4( shadowMapCenter + c3.yy, objDepth, 1 ) ).x;	// -1 -1
	flSum += dot( v20Taps, c0.wwww );

	vec4 v33Taps;
	v33Taps.x = texture2DProj( DepthSampler, vec4( shadowMapCenter + c2.wz, objDepth, 1 ) ).x;	// 1 0
	v33Taps.y = texture2DProj( DepthSampler, vec4( shadowMapCenter + c1.zw, objDepth, 1 ) ).x;	// -1 0
	v33Taps.z = texture2DProj( DepthSampler, vec4( shadowMapCenter + c1.wz, objDepth, 1 ) ).x;	// 0 -1
	v33Taps.w = texture2DProj( DepthSampler, vec4( shadowMapCenter + c2.zw, objDepth, 1 ) ).x;	// 0 1
	flSum += dot( v33Taps, c1.xxxx );

	flSum += texture2DProj( DepthSampler, vec4( shadowMapCenter, objDepth, 1 ) ).x * c1.y;
	
	flSum = pow( flSum, 1.4f );

	return 1f - flSum;
}

float ShadowCalculation(int lightIndex, vec3 lightDir, vec3 normal)
{
    vec4 fragPosLightSpace = frag_position_lightspace[lightIndex];
    vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;

    projCoords = projCoords * 0.5 + 0.5;
    
    if(projCoords.z > 1.0)
        return 0.0;
        
    if (lightIndex == 0)
    {
        //return SimplePCF(shadow1Sampler, projCoords, lightDir, normal);
        //return SimpleShadow(shadow1Sampler, projCoords, lightDir, normal);
        return DoShadowNvidiaPCF5x5GaussianPC(shadow1Sampler, projCoords);
    } 
    else if (lightIndex == 1) 
    {
        //return SimplePCF(shadow2Sampler, projCoords, lightDir, normal);
        //return SimpleShadow(shadow2Sampler, projCoords, lightDir, normal);
        return DoShadowNvidiaPCF5x5GaussianPC(shadow2Sampler, projCoords);
    }
    else if (lightIndex == 2) 
    {
        //return SimplePCF(shadow3Sampler, projCoords, lightDir, normal);
        //return SimpleShadow(shadow3Sampler, projCoords, lightDir, normal);
        return DoShadowNvidiaPCF5x5GaussianPC(shadow3Sampler, projCoords);
    }
    else if (lightIndex == 3) 
    {
        //return SimplePCF(shadow4Sampler, projCoords, lightDir, normal);
        //return SimpleShadow(shadow4Sampler, projCoords, lightDir, normal);
        return DoShadowNvidiaPCF5x5GaussianPC(shadow4Sampler, projCoords);
    }
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

    float shadow = 0f;
    if (light.hasShadows) 
    {
        shadow = ShadowCalculation(lightIndex, lightDir, normal);
    }

    outdiffuse *= (1.0f - shadow);

    return ambient + outdiffuse;
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

    float shadow = 0f;
    if (light.hasShadows) 
    {
        shadow = ShadowCalculation(lightIndex, lightDir, normal);
    }

    outdiffuse *= (1.0f - shadow);

    return ambient + outdiffuse;
}

vec3 CalcSun(int lightIndex, vec3 normal, vec3 fragPos, vec3 viewDir)
{
    Light light = lightSources[lightIndex];
    vec3 lightDir = normalize(-light.direction);

    vec3 outdiffuse = light.diffuse * light.brightness;

    float shadow = 0f;
    if (light.hasShadows) 
    {
        shadow = ShadowCalculation(lightIndex, lightDir, normal);
    }

    outdiffuse *= (1.0f - shadow);

    return light.ambient + outdiffuse;
}

vec3 CalcLighting(vec3 normal, vec3 fragPos, vec3 viewDir)
{
    vec3 result = vec3(0.0, 0.0, 0.0);

    for(int i = 0; i < lightSourcesCount; i++)
    {
        switch(lightSources[i].type)
        {
            case 0: // point
            {
              result += CalcPointLight(i, normal, fragPos, viewDir);
              break;
            }
            case 1: // sun
            {
              result += CalcSun(i, normal, fragPos, viewDir);
              break;
            }
            case 2: // spot
            {
              result += CalcSpotlight(i, normal, fragPos, viewDir);
              break;
            }
        }
    }

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
    //if (diffuseTex.a < 0.01)
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
            case 1: // sun
            {
              radiance = CalcSun(i, normal, frag_position, viewDir);
              break;
            }
            case 2: // spot
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

    result = lighting;

    if (!usePbr)
    {
        result *= diffuseTex.rgb;
    }

    outputColor = vec4(result, 1.0);
}

