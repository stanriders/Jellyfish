#version 330 core
precision highp float;
layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec2 aTexCoord;
layout (location = 2) in vec3 aNormal;

uniform mat4 lightSpaceMatrix;
uniform mat4 transform;
uniform mat4 rotation;

void main()
{
    vec4 transformedPosition = vec4(aPosition, 1.0) * rotation * transform;
    gl_Position = transformedPosition * lightSpaceMatrix;
} 