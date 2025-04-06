#version 460
#include CommonVertex.vert

out vec2 frag_texCoord;
out vec3 frag_normal;
out vec3 frag_position;
out vec4 frag_position_sun;
out vec4 frag_position_lightspace[12];

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
uniform Light lightSources[12];
uniform int lightSourcesCount;

uniform Light sun;

void main(void)
{
    frag_texCoord = aTexCoord;

    vec4 transformedNormal = rotation * vec4(aNormal, 1.0);
    frag_normal = transformedNormal.xyz;

    vec4 transformedPosition = transform * rotation * vec4(aPosition, 1.0);
    if (boneCount > 0)
    {
        transformedPosition = boneTransform() * transformedPosition;
    }

    frag_position = transformedPosition.xyz;
    
    for (int i = 0; i < lightSourcesCount; i++)
    {
        frag_position_lightspace[i] = lightSources[i].lightSpaceMatrix * vec4(frag_position, 1.0);
    }

    frag_position_sun = sun.lightSpaceMatrix * vec4(frag_position, 1.0);

    gl_Position = projection * view * transformedPosition;
}