
#version 330 core

uniform float gAspectRatio;
uniform float gTanHalfFOV;

layout (location = 0) in vec2 aPos;
layout (location = 1) in vec2 aTexCoords;

out vec2 TexCoords;
out vec2 ViewRay;

void main()
{
    gl_Position = vec4(aPos.x, aPos.y, 0.0, 1.0); 
    TexCoords = aTexCoords;
    ViewRay.x = aPos.x * gAspectRatio * gTanHalfFOV;
    ViewRay.y = aPos.y * gTanHalfFOV;
}  