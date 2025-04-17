#define MAX_LIGHTS 12
#define CSM_CASCADES 4
#define SUN_SAMPLER_BINDING 3
//#define CSM_DEBUG

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
