#version 460 core
#define SMAA_INCLUDE_PS 0

layout (location = 0) in vec2 aPosition;
layout (location = 1) in vec2 aTexCoords;

uniform vec2 uViewportSize;
uniform vec2 uTexelSize;

#define SMAA_RT_METRICS vec4(uTexelSize.x, uTexelSize.y, uViewportSize.x, uViewportSize.y)
#include SMAA.shared

out vec2 vTexCoord;
out vec2 vPixCoord;
out vec4 vOffset[3];

void main() {
	vTexCoord = aTexCoords;

	SMAABlendingWeightCalculationVS(aTexCoords, vPixCoord, vOffset);

	gl_Position = vec4(aPosition, 0.0, 1.0);
}
