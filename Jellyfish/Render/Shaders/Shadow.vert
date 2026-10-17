#version 460
#include CommonVertex.vert

out vec2 frag_texCoord;

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

    frag_texCoord = aTexCoord;
    gl_Position = lightSpaceMatrix * transformedPosition;
} 