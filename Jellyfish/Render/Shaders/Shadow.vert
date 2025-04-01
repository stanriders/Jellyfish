#version 460
#include CommonVertex.vert

precision highp float;

uniform mat4 lightSpaceMatrix;

void main()
{
    vec4 transformedPosition = transform * rotation * vec4(aPosition, 1.0);
    if (boneCount > 0)
    {
        transformedPosition = boneTransform() * transformedPosition;
    }
    gl_Position = lightSpaceMatrix * transformedPosition;
} 