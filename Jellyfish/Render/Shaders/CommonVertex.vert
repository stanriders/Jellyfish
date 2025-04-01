layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec2 aTexCoord;
layout(location = 2) in vec3 aNormal;
layout(location = 3) in vec4 aBoneIDs;
layout(location = 4) in vec4 aWeights;

uniform mat4 view;
uniform mat4 projection;

uniform mat4 transform;
uniform mat4 rotation;

const int MAX_BONES = 200;
uniform mat4 bones[200];
uniform int boneCount;

mat4 boneTransform()
{
    mat4 boneTransform = bones[int(floor(aBoneIDs[0]))] * aWeights[0];
    boneTransform += bones[int(floor(aBoneIDs[1]))] * aWeights[1];
    boneTransform += bones[int(floor(aBoneIDs[2]))] * aWeights[2];
    boneTransform += bones[int(floor(aBoneIDs[3]))] * aWeights[3];

    return boneTransform;
}