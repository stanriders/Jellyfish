#version 460
layout (location = 0) in vec3 aPos;

out vec3 WorldPos;

uniform mat4 projection;
uniform mat4 view;

void main()
{
    WorldPos = aPos; // cube vertex position → direction
    gl_Position = projection * view * vec4(WorldPos, 1.0);
}