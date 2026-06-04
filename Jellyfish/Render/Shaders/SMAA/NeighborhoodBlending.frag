#version 460 core
#define SMAA_INCLUDE_VS 0

layout(binding = 0) uniform sampler2D uColorTexture;
layout(binding = 1) uniform sampler2D uBlendTexture;

uniform vec2 uViewportSize;
uniform vec2 uTexelSize;

#define SMAA_RT_METRICS vec4(uTexelSize.x, uTexelSize.y, uViewportSize.x, uViewportSize.y)
#include SMAA.shared

out vec3 FragColor;

in vec2 vTexCoord;
in vec4 vOffset;

void main() {
  vec4 color = SMAANeighborhoodBlendingPS(vTexCoord, vOffset, uColorTexture, uBlendTexture);

  FragColor = color.rgb;
}
