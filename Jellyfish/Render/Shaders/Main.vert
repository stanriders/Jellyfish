#version 460
#include CommonVertex.vert

out vec2 frag_texCoord;
out vec3 frag_normal;
out vec3 frag_position;
out float frag_clipspaceZ;

void main(void)
{
    frag_texCoord = aTexCoord;

    vec4 localNormal = vec4(aNormal, 0.0);
    vec4 localPosition = vec4(aPosition, 1.0);
    if (boneCount > 0)
    {
        localPosition = boneTransform() * localPosition;
        localNormal = boneTransform() * localNormal;
    }
    
    vec4 transformedNormal = rotation * localNormal;
    vec4 transformedPosition = transform * rotation * localPosition;

    frag_position = transformedPosition.xyz;
    frag_normal = transformedNormal.xyz;
    
    gl_Position = projection * view * transformedPosition;
    frag_clipspaceZ = gl_Position.z;
}