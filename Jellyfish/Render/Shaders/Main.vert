#version 460

layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec2 aTexCoord;
layout(location = 2) in vec3 aNormal;

uniform vec3 cameraPos;

uniform mat4 view;
uniform mat4 projection;
uniform mat4 transform;
uniform mat4 rotation;

out vec2 frag_texCoord;
out vec3 frag_normal;
out vec3 frag_position;
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
};
uniform Light lightSources[4];
uniform int lightSourcesCount;

void main(void)
{
    frag_texCoord = aTexCoord;

    vec4 transformedNormal = vec4(aNormal, 1.0) * rotation;
    frag_normal = transformedNormal.xyz;

    vec4 transformedPosition = vec4(aPosition, 1.0) * rotation * transform;
    frag_position = transformedPosition.xyz;
    
    for (int i = 0; i < lightSourcesCount; i++)
    {
        frag_position_lightspace[i] = vec4(frag_position, 1.0) * lightSources[i].lightSpaceMatrix;
    }

    gl_Position = transformedPosition * view * projection;
}