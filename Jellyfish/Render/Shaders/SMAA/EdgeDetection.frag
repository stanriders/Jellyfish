#version 460 core
#define SMAA_INCLUDE_VS 0
#include SMAA.shared

layout(binding = 0) uniform sampler2D uColorTexture;

out vec4 FragColor;

in vec2 vTexCoord;
in vec4 vOffset[3];

void main() {
  vec2 color = SMAAColorEdgeDetectionPS(vTexCoord, vOffset, uColorTexture);

  FragColor = vec4(color, 0.0, 0.0);
}
