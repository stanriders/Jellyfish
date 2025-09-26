
float LinearizeDepth(float z01, float near, float far)
{
    float n = near;
    float f = far;
    float z = z01 * 2.0 - 1.0;
    return (2.0 * n * f) / (f + n - z * (f - n));
}

float GetDepthNormalized(sampler2D depthSampler, vec2 uv, float near, float far)
{
    return LinearizeDepth(texture(depthSampler, uv).r, near, far) / far;
}

float GetDepth(sampler2D depthSampler, vec2 uv, float near, float far)
{
    return LinearizeDepth(texture(depthSampler, uv).r, near, far);
}

vec3 GetNormal(sampler2D normalSampler, vec2 uv)
{
    return normalize(textureLod(normalSampler, uv, 0.0).xyz);
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