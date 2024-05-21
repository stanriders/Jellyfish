#version 460

out vec4 outputColor;

in vec2 frag_texCoord;
in vec3 frag_normal;
in vec3 frag_position;
in vec4 frag_position_lightspace[4];

uniform vec3 cameraPos;
layout(binding=0) uniform sampler2D diffuseSampler;
layout(binding=1) uniform sampler2D normalSampler;

layout(binding=2) uniform sampler2D shadow1Sampler;
layout(binding=3) uniform sampler2D shadow2Sampler;
layout(binding=4) uniform sampler2D shadow3Sampler;
layout(binding=5) uniform sampler2D shadow4Sampler;

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
};
uniform Light lightSources[4];
uniform int lightSourcesCount;

uniform bool usePhong;
uniform int phongExponent;

float FLASHLIGHT_SHADOW_TEXTURE_RESOLUTION = 2048;

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

float ShadowCalculation(vec4 fragPosLightSpace, int lightIndex, vec3 lightDir, vec3 normal)
{
    vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;

    projCoords = projCoords * 0.5 + 0.5;
    
    if(projCoords.z > 1.0)
        return 0.0;
        
    if (lightIndex == 0)
    {
        return DoShadowNvidiaPCF5x5GaussianPC(shadow1Sampler, projCoords);
    } 
    else if (lightIndex == 1) 
    {
        return DoShadowNvidiaPCF5x5GaussianPC(shadow2Sampler, projCoords);
    }
    else if (lightIndex == 2) 
    {
        return DoShadowNvidiaPCF5x5GaussianPC(shadow3Sampler, projCoords);
    }
    else if (lightIndex == 3) 
    {
        return DoShadowNvidiaPCF5x5GaussianPC(shadow4Sampler, projCoords);
    }
}  

vec3 CalcPointLight(Light light, int lightIndex, vec3 normal, vec3 fragPos, vec3 viewDir)
{
    vec3 lightDir = normalize(light.position - fragPos);

    //diffuse shading
    float diff = max(dot(normal, lightDir), 0.0);

    vec3 ambient = light.ambient;
    vec3 outdiffuse = light.diffuse * diff;

    float distanceToLight = length(light.position - fragPos);
    float attenuation = 255.0 / (light.constant + 
                        light.linear * distanceToLight + 
                        light.quadratic * (distanceToLight * distanceToLight));

    ambient  *= attenuation;
    outdiffuse  *= attenuation;

    // specular
    vec3 specular = vec3(0,0,0);
    if (usePhong)
    {
        float specularStrength = texture(normalSampler, frag_texCoord * vec2(1.0, -1.0)).a;
        specularStrength  *= attenuation;

        vec3 reflectDir = reflect(-lightDir, normal);  
        float spec = pow(max(dot(viewDir, reflectDir), 0.0), phongExponent);
        specular = specularStrength * spec * light.diffuse; 
    }

    float shadow = ShadowCalculation(frag_position_lightspace[lightIndex], lightIndex, lightDir, normal);
    
    outdiffuse *= (1.0f - shadow);
    specular *= (1.0f - shadow);

    return (ambient + (outdiffuse + specular)) * light.brightness;
}

vec3 CalcSpotlight(Light light, int lightIndex, vec3 normal, vec3 fragPos, vec3 viewDir)
{
    vec3 lightDir = normalize(light.position - fragPos);

    //diffuse shading
    float diff = max(dot(normal, lightDir), 0.0);

    vec3 ambient = light.ambient;
    vec3 outdiffuse = light.diffuse * diff;

    float distanceToLight = length(light.position - fragPos);
    float attenuation = 255.0 / (light.constant + 
                        light.linear * distanceToLight + 
                        light.quadratic * (distanceToLight * distanceToLight));

    ambient  *= attenuation;
    outdiffuse  *= attenuation;

    // specular
    vec3 specular = vec3(0,0,0);
    if (usePhong)
    {
        float specularStrength = texture(normalSampler, frag_texCoord * vec2(1.0, -1.0)).a;
        specularStrength  *= attenuation;

        vec3 reflectDir = reflect(-lightDir, normal);  
        float spec = pow(max(dot(viewDir, reflectDir), 0.0), phongExponent);
        specular = specularStrength * spec * light.diffuse; 
    }

    float theta = dot(lightDir, normalize(-light.direction));
    float epsilon   = light.cone - light.outcone;
    float intensity = clamp((theta - light.outcone) / epsilon, 0.0, 1.0); 

    outdiffuse *= intensity;
    specular *= intensity;
    ambient *= intensity;

    float shadow = ShadowCalculation(frag_position_lightspace[lightIndex], lightIndex, lightDir, normal);
    
    outdiffuse *= (1.0f - shadow);
    specular *= (1.0f - shadow);

    return (ambient + (outdiffuse + specular)) * light.brightness;
}

vec3 CalcSun(Light light, int lightIndex, vec3 normal, vec3 fragPos, vec3 viewDir)
{
    vec3 lightDir = normalize(light.direction);

    //diffuse shading
    float diff = max(dot(normal, lightDir), 0.0);

    vec3 ambient = light.ambient;
    vec3 outdiffuse = light.diffuse * diff;
        
    vec3 specular = vec3(0,0,0);
    if (usePhong)
    {
        float specularStrength = texture(normalSampler, frag_texCoord * vec2(1.0, -1.0)).a;

        vec3 reflectDir = reflect(-lightDir, normal);  
        float spec = pow(max(dot(viewDir, reflectDir), 0.0), phongExponent);
        specular = specularStrength * spec * light.diffuse; 
    }
    
    float shadow = ShadowCalculation(frag_position_lightspace[lightIndex], lightIndex, lightDir, normal);

    outdiffuse *= (1.0f - shadow);
    specular *= (1.0f - shadow);

    return (ambient + (outdiffuse + specular)) * light.brightness;
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
              result += CalcPointLight(lightSources[i], i, normal, fragPos, viewDir);
              break;
            }
            case 1: // sun
            {
              result += CalcSun(lightSources[i], i, normal, fragPos, viewDir);
              break;
            }
            case 2: // spot
            {
              result += CalcSpotlight(lightSources[i], i, normal, fragPos, viewDir);
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

    vec3 normalTex = texture(normalSampler, frag_texCoord * vec2(1.0, -1.0)).rgb;

    vec3 tangentSpaceNormal = normalize(normalTex * 2.0 - 1.0);
    mat3 tbn = GetTBN(frag_position, frag_texCoord, frag_normal);
    tangentSpaceNormal = normalize(tbn * tangentSpaceNormal);  

    vec3 viewDir = normalize(cameraPos - frag_position);

    vec3 result = diffuseTex.rgb;
    vec3 lighting = CalcLighting(tangentSpaceNormal, frag_position, viewDir);
    result *= lighting;

    outputColor = vec4(result, 1.0);
}

