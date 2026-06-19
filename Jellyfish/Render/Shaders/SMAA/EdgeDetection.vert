#version 460 core
#define SMAA_INCLUDE_PS 0
#include SMAA.shared

layout (location = 0) in vec2 aPosition;
layout (location = 1) in vec2 aTexCoords;

uniform vec2 uTexelSize;

out vec2 vTexCoord;
out vec4 vOffset[3];

void main() {
	vTexCoord = aTexCoords;

	SMAAEdgeDetectionVS(aTexCoords, vOffset);

	gl_Position = vec4(aPosition, 0.0, 1.0);
}
