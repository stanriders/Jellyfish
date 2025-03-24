#version 460

layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec2 aTexCoord;
layout(location = 2) in vec3 aNormal;
layout(location = 3) in vec4 aBoneIDs;
layout(location = 4) in vec4 aWeights;

const int MAX_BONES = 200;

uniform mat4 view;
uniform mat4 projection;
uniform mat4 transform;
uniform mat4 rotation;
uniform mat4 bones[200];
uniform int boneCount;

out vec2 frag_texCoord;
out vec3 frag_normal;
out vec3 frag_position;
out vec4 frag_position_sun;
out vec4 frag_position_lightspace[4];

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

uniform Light sun;

void main(void)
{
    frag_texCoord = aTexCoord;

    mat4 boneTransform = bones[int(floor(aBoneIDs[0]))] * aWeights[0];
    boneTransform += bones[int(floor(aBoneIDs[1]))] * aWeights[1];
    boneTransform += bones[int(floor(aBoneIDs[2]))] * aWeights[2];
    boneTransform += bones[int(floor(aBoneIDs[3]))] * aWeights[3];

    vec4 transformedNormal = rotation * vec4(aNormal, 1.0);
    frag_normal = transformedNormal.xyz;

    vec4 transformedPosition = transform * rotation * vec4(aPosition, 1.0);
    if (boneCount > 0)
    {
        transformedPosition = boneTransform * transformedPosition;
    }

    frag_position = transformedPosition.xyz;
    
    for (int i = 0; i < lightSourcesCount; i++)
    {
        frag_position_lightspace[i] = lightSources[i].lightSpaceMatrix * vec4(frag_position, 1.0);
    }

    frag_position_sun = sun.lightSpaceMatrix * vec4(frag_position, 1.0);

    gl_Position = projection * view * transformedPosition;
}