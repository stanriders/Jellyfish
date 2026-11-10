
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
    vec3  F   = FresnelSchlickRoughness(max(dot(H, V), 0.0), F0, roughness);

    vec3 kS = F;
    vec3 kD = (1.0 - kS) * (1.0 - metalness);

    vec3 numerator = NDF * G * F;
    float denominator = max(0.00001, 4.0 * NdotV * NdotL);
    vec3 specular = numerator / denominator;

    return BRDFResult(specular, kD);
}

vec3 EvaluateProbeSH(int probe, vec3 n)
{
    vec4 result = lightProbes[probe].sh[0] * 0.282095
                + lightProbes[probe].sh[1] * 0.488603 * n.y
                + lightProbes[probe].sh[2] * 0.488603 * n.z
                + lightProbes[probe].sh[3] * 0.488603 * n.x
                + lightProbes[probe].sh[4] * 1.092548 * n.x * n.y
                + lightProbes[probe].sh[5] * 1.092548 * n.y * n.z
                + lightProbes[probe].sh[6] * 0.315392 * (3.0 * n.z * n.z - 1.0)
                + lightProbes[probe].sh[7] * 1.092548 * n.x * n.z
                + lightProbes[probe].sh[8] * 0.546274 * (n.x * n.x - n.y * n.y);

    // L2 SH can ring slightly negative opposite of very bright sources
    return max(result.rgb, 0.0);
}

vec3 ParallaxCorrect(vec3 worldPos, vec3 R, vec4 probe, float roughness)
{
    vec3 local = worldPos - probe.xyz;
    float radius = probe.w;

    float b = dot(local, R);
    float c = dot(local, local) - radius * radius;
    float discriminant = b * b - c;
    if (discriminant < 0.0)
    return R;

    float t = -b + sqrt(discriminant);
    if (t <= 0.0)
    return R;

    vec3 corrected = normalize(local + R * t);

    // blurry reflections don't show the error, and the correction gets worse far from the center
    return normalize(mix(corrected, R, roughness * roughness));
}

float ProbeWeightDistSq(float d2, float radius)
{
    float window = clamp(1.0 - d2 / (radius * radius), 0.0, 1.0);
    return window * window / (d2 + 1e-4);
}

void GetBlendedLightProbe(uint cluster, vec3 worldPos, vec3 N, vec3 R, float roughness, out vec3 outDiffuse, out vec3 outSpecular)
{
    outDiffuse = vec3(0);
    outSpecular = vec3(0);

    uint base = cluster * MAX_PROBES_PER_CLUSTER;
    uint count = clusterProbeCount[cluster];

    bool allProbes = count == PROBE_CLUSTER_ALL;
    if (allProbes)
        count = uint(probeCount);

    vec4 fallbackPosition = lightProbes[fallbackProbe].position;
    vec3 toFallback = worldPos - fallbackPosition.xyz;
    float fallbackWeight = 1e-6 / (dot(toFallback, toFallback) + 1e-4);

    float total = fallbackWeight;
    for (uint i = 0u; i < count; i++)
    {
        vec4 probe = lightProbes[allProbes ? i : clusterProbeIndices[base + i]].position;
        vec3 toProbe = worldPos - probe.xyz;
        total += ProbeWeightDistSq(dot(toProbe, toProbe), probe.w);
    }

    for (uint i = 0u; i <= count; i++)
    {
        // last iteration is the fallback probe
        bool isFallback = i == count;
        int probe = isFallback ? fallbackProbe : int(allProbes ? i : clusterProbeIndices[base + i]);
        vec4 probePosition = lightProbes[probe].position;
        vec3 toProbe = worldPos - probePosition.xyz;

        float w = (isFallback ? fallbackWeight : ProbeWeightDistSq(dot(toProbe, toProbe), probePosition.w)) / total;

        if (w <= 0.0)
            continue;

        outDiffuse += EvaluateProbeSH(probe, N) * w;

        if (iblPrefilterEnabled)
        {
            vec3 direction = isFallback ? R : ParallaxCorrect(worldPos, R, probePosition, roughness);
            vec3 prefiltered = textureLod(lightProbes[probe].prefilter, direction, roughness * (prefilterMips - 1)).rgb;
            outSpecular += prefiltered * w;
        }
    }
}

vec3 ComputeIBL(uint cluster, vec3 N, vec3 V, vec3 diffuseColor, float roughness, float metalness, vec2 uv)
{
    vec3 F0 = mix(vec3(0.04), diffuseColor, metalness);
    float NdotV = max(dot(N, V), 0.0);
    vec3 F_env = FresnelSchlickRoughness(NdotV, F0, roughness);
    vec3 kS = F_env;
    vec3 kD = (1.0 - kS) * (1.0 - metalness);
    vec3 R = normalize(reflect(-V, N));

    vec3 diffuseIBL = vec3(0);
    vec3 specularIBL = vec3(0);

    if (iblEnabled && probeCount > 0) 
    {
        vec3 blendedDiffuse;
        vec3 blendedSpecular;

        GetBlendedLightProbe(cluster, frag_position, N, R, roughness, blendedDiffuse, blendedSpecular);

        diffuseIBL = diffuseColor * blendedDiffuse;

        if (iblPrefilterEnabled)
        {
            if (sslrEnabled && roughness < 0.99)
            {
                vec4 ssrSample = texture(reflectionMap, uv);
                vec3 ssrColor = ssrSample.rgb;
                float ssrConfidence = ssrSample.a;

                float reflectionWeight = (1.0 - roughness);
                reflectionWeight = clamp(reflectionWeight, 0.0, 1.0);

                float roughFade = 1.0 - smoothstep(0.4, 1.0, roughness);
                reflectionWeight *= roughFade;

                blendedSpecular = mix(blendedSpecular, ssrColor, ssrConfidence * reflectionWeight);
            }
            
            vec2 brdf = integrateBRDFApprox(NdotV, roughness);
            specularIBL = blendedSpecular * (F0 * brdf.x + brdf.y);
        }
    }


    return kD * diffuseIBL + specularIBL;
}

// geometric specular anti-aliasing (Kaplanyan 2016, Tokuyoshi 2019)
float SpecularAntiAliasing(vec3 N, float roughness)
{
    const float varianceScale = 0.15;
    const float threshold = 0.2;

    vec3 du = dFdx(N);
    vec3 dv = dFdy(N);
    float variance = varianceScale * (dot(du, du) + dot(dv, dv));
    float kernel = min(2.0 * variance, threshold);

    float alpha = roughness * roughness;
    return sqrt(sqrt(clamp(alpha * alpha + kernel, 0.0, 1.0)));
}