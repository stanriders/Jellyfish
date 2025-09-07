#version 460
#include CommonVertex.vert

precision highp float;

uniform mat4 lightSpaceMatrix;

void main()
{
    vec4 localPosition = vec4(aPosition, 1.0);
    if (boneCount > 0)
    {
        localPosition = boneTransform() * localPosition;
    }

    vec4 transformedPosition = transform * rotation * localPosition;
    gl_Position = lightSpaceMatrix * transformedPosition;
} 