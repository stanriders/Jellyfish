#version 460 core
#define SMAA_INCLUDE_PS 0

layout (location = 0) in vec2 aPosition;
layout (location = 1) in vec2 aTexCoords;

uniform vec2 uTexelSize;

#define SMAA_RT_METRICS vec4(uTexelSize.x, uTexelSize.y, 0, 0)
#include SMAA.shared

out vec2 vTexCoord;
out vec4 vOffset;

void main() {
	vTexCoord = aTexCoords;

	SMAANeighborhoodBlendingVS(aTexCoords, vOffset);

	gl_Position = vec4(aPosition, 0.0, 1.0);
}
