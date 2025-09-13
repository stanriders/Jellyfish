#version 460 core

out vec3 FragColor;
in vec3 localPos;

layout (binding = 0) uniform samplerCube environmentMap;

const float PI = 3.14159265359;

void main()
{
    // Normal from current cube direction
    vec3 N = normalize(localPos);

    // Tangent space basis (arbitrary)
    vec3 up    = abs(N.y) < 0.999 ? vec3(0.0, 1.0, 0.0) : vec3(1.0, 0.0, 0.0);
    vec3 right = normalize(cross(up, N));
    up         = normalize(cross(N, right));

    // Sample the hemisphere around N
    float sampleDelta = 0.1; // smaller = smoother, more expensive
    vec3 irradiance = vec3(0.0);
    float nrSamples = 0.0;

    for (float phi = 0.0; phi < 2.0 * PI; phi += sampleDelta)
    {
        for (float theta = 0.0; theta < 0.5 * PI; theta += sampleDelta)
        {
            // spherical → cartesian (in tangent space)
            vec3 tangentSample = vec3(
                sin(theta) * cos(phi),
                sin(theta) * sin(phi),
                cos(theta)
            );

            // transform to world space sample direction
            vec3 sampleVec = tangentSample.x * right + tangentSample.y * up + tangentSample.z * N;

            irradiance += texture(environmentMap, sampleVec).rgb * cos(theta) * sin(theta);
            nrSamples++;
        }
    }

    irradiance = PI * irradiance * (1.0 / float(nrSamples));
    FragColor = irradiance;
}