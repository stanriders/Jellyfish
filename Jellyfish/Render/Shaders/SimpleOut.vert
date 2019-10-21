#version 330 core

layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec2 aTexCoord;
layout(location = 2) in vec3 aNormal;

uniform mat4 view;
uniform mat4 projection;
uniform mat4 transform;
uniform mat4 rotation;

out vec2 texCoord;
out vec3 normal;
out vec3 position;

void main(void)
{
	texCoord = aTexCoord;
	vec4 transformedNormal = vec4(aNormal, 1.0) * rotation;
	normal = transformedNormal.xyz;

	vec4 transformedPosition = vec4(aPosition, 1.0) * rotation * transform;
	position = transformedPosition.xyz;

	gl_Position = transformedPosition * view * projection;
}