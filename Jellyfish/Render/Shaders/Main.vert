#version 460
#include CommonVertex.vert

out vec2 frag_texCoord;
out vec3 frag_normal;
out vec3 frag_position;
out float frag_clipspaceZ;

void main(void)
{
    frag_texCoord = aTexCoord;

    vec4 transformedNormal = rotation * vec4(aNormal, 1.0);
    frag_normal = transformedNormal.xyz;

    vec4 transformedPosition = transform * rotation * vec4(aPosition, 1.0);
    if (boneCount > 0)
    {
        transformedPosition = boneTransform() * transformedPosition;
    }

    frag_position = transformedPosition.xyz;
    
    gl_Position = projection * view * transformedPosition;
    frag_clipspaceZ = gl_Position.z;
}