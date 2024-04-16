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

void main(void)
{
    frag_texCoord = aTexCoord;

    vec4 transformedNormal = vec4(aNormal, 1.0) * rotation;
    frag_normal = transformedNormal.xyz;

    vec4 transformedPosition = vec4(aPosition, 1.0) * rotation * transform;
    frag_position = transformedPosition.xyz;

    gl_Position = transformedPosition * view * projection;
}