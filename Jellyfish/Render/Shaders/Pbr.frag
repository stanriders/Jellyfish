
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

vec2 integrateBRDFApprox(float NdotV, float roughness)
{
    const vec4 c0 = vec4(-1.0, -0.0275, -0.572, 0.022);
    const vec4 c1 = vec4( 1.0,  0.0425,  1.04, -0.04);
    vec4 r = roughness * c0 + c1;
    float a004 = min(r.x * r.x, exp2(-9.28 * NdotV)) * r.x + r.y;
    return vec2(-1.04, 1.04) * a004 + r.zw;
}

struct BRDFResult {
    vec3 specular;
    vec3 kD;
};

BRDFResult ComputeBRDF(vec3 N, vec3 V, vec3 L, vec3 F0, float roughness, float metalness)
{
    vec3 H = normalize(V + L);
    float NdotL = max(dot(N, L), 0.0);
    float NdotV = max(dot(N, V), 0.0);

    float NDF = DistributionGGX(N, H, roughness);
    float G   = GeometrySmith(N, V, L, roughness);
    vec3  F   = fresnelSchlick(max(dot(H, V), 0.0), F0);

    vec3 kS = F;
    vec3 kD = (1.0 - kS) * (1.0 - metalness);

    vec3 numerator = NDF * G * F;
    float denominator = max(0.00001, 4.0 * NdotV * NdotL);
    vec3 specular = numerator / denominator;

    return BRDFResult(specular, kD);
}

void GetBlendedLightProbe(vec3 worldPos, vec3 N, vec3 R, float roughness, out vec3 outDiffuse, out vec3 outSpecular)
{
    float distSq[PROBE_BLEND_COUNT];
    int idx[PROBE_BLEND_COUNT];
    for (int i = 0; i < PROBE_BLEND_COUNT; i++) {
        distSq[i] = 1e20;
        idx[i] = -1;
    }

    for (int i = 0; i < probeCount; i++) {
        vec3 probePos = lightProbes[i].position;
        float d2 = dot(worldPos - probePos, worldPos - probePos);

        // Insert sorted (smallest first)
        for (int j = 0; j < PROBE_BLEND_COUNT; j++) {
            if (d2 < distSq[j]) {
                for (int k = PROBE_BLEND_COUNT - 1; k > j; k--) {
                    distSq[k] = distSq[k - 1];
                    idx[k] = idx[k - 1];
                }
                distSq[j] = d2;
                idx[j] = i;
                break;
            }
        }
    }

    float weights[PROBE_BLEND_COUNT];
    float total = 0.0;
    for (int i = 0; i < PROBE_BLEND_COUNT; i++) {
        if (idx[i] < 0) { weights[i] = 0.0; continue; }
        float w = 1.0 / (distSq[i] + 1e-4);
        weights[i] = w;
        total += w;
    }

    outDiffuse = vec3(0);
    outSpecular = vec3(0);

    for (int i = 0; i < PROBE_BLEND_COUNT; i++) 
    {
        if (idx[i] < 0) continue;
        float w = weights[i] / total;
        LightProbe probe = lightProbes[idx[i]];

        vec3 irradiance = texture(probe.irradiance, N).rgb;
        vec3 prefiltered = textureLod(probe.prefilter, R, roughness * prefilterMips).rgb;

        outDiffuse += irradiance * w;
        outSpecular += prefiltered * w;
    }
}

vec3 ComputeIBL(vec3 N, vec3 V, vec3 diffuseColor, float roughness, float metalness, vec2 uv)
{
    vec3 F0 = mix(vec3(0.04), diffuseColor, metalness);
    float NdotV = max(dot(N, V), 0.0);
    vec3 F_env = fresnelSchlick(NdotV, F0);
    vec3 kS = F_env;
    vec3 kD = (1.0 - kS) * (1.0 - metalness);
    vec3 R = normalize(reflect(-V, N));

    vec3 diffuseIBL = vec3(0);
    vec3 specularIBL = vec3(0);

    if (iblEnabled && probeCount > 0) 
    {
        vec3 blendedDiffuse;
        vec3 blendedSpecular;

        GetBlendedLightProbe(frag_position, N, R, roughness, blendedDiffuse, blendedSpecular);

        diffuseIBL = diffuseColor * blendedDiffuse;

        if (iblPrefilterEnabled)
        {
            vec2 brdf = integrateBRDFApprox(NdotV, roughness);
            specularIBL = blendedSpecular * (F_env * brdf.x + brdf.y);
        }
    }

    if (sslrEnabled && roughness < 0.99)
    {
        vec4 ssrSample = texture(reflectionMap, uv);
        vec3 ssrColor = ssrSample.rgb;
        float ssrConfidence = ssrSample.a;

        float reflectionWeight = (1.0 - roughness) * F_env.r;
        reflectionWeight = clamp(reflectionWeight, 0.0, 1.0);

        float roughFade = 1.0 - smoothstep(0.4, 1.0, roughness);
        reflectionWeight *= roughFade;

        specularIBL = mix(specularIBL, ssrColor, ssrConfidence * reflectionWeight);
    }

    return kD * diffuseIBL + specularIBL;
}
