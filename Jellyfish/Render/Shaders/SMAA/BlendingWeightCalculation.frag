#version 460 core
#define SMAA_INCLUDE_VS 0

uniform vec2 uViewportSize;
uniform vec2 uTexelSize;

#define SMAA_RT_METRICS vec4(uTexelSize.x, uTexelSize.y, uViewportSize.x, uViewportSize.y)
#include SMAA.shared

layout(binding = 0) uniform sampler2D uEdgesTexture;
layout(binding = 1) uniform sampler2D uAreaTexture;
layout(binding = 2) uniform sampler2D uSearchTexture;

out vec4 FragColor;

in vec2 vTexCoord;
in vec2 vPixCoord;
in vec4 vOffset[3];

void main() {
  vec4 weights = SMAABlendingWeightCalculationPS(vTexCoord, vPixCoord, vOffset, uEdgesTexture, uAreaTexture, uSearchTexture, vec4(0));

  FragColor = weights;
}
