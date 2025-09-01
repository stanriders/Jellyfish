
float LinearizeDepth(float z01, float near, float far)
{
    float n = near;
    float f = far;
    float z = z01 * 2.0 - 1.0;
    return (2.0 * n * f) / (f + n - z * (f - n));
}

float GetDepth(sampler2D depthSampler, vec2 uv, float near, float far)
{
    return LinearizeDepth(texture(depthSampler, uv).r, near, far) / far;
}

vec3 GetNormal(sampler2D normalSampler, vec2 uv)
{
    vec3 normal = textureLod(normalSampler, uv, 0.0).xyz;
    normal.z *= -1.0f; // opengl is Special
    return normalize(normal);
}