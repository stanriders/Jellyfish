#version 460
#include Fxaa.frag

out vec4 FragColor;
  
in vec2 TexCoords;

uniform float exposure;
uniform bool isEnabled;

uniform vec2 screenSize;
#define SourceSizeRecp vec2(1.0 / screenSize)

layout(binding=0) uniform sampler2D screenTexture;
layout(binding=1) uniform sampler2D aoTexture;

void main()
{ 

    if (!isEnabled)
    {
        FragColor = vec4(texture(screenTexture, TexCoords).rgb, 1.0);
        return;
    }

    vec3 screen = FxaaPixelShader(TexCoords, screenTexture, vec2(SourceSizeRecp.x, SourceSizeRecp.y));
    vec3 ao = texture(aoTexture, TexCoords).rgb;
    screen *= ao;

    const float gamma = 2.2;
    vec3 mapped = vec3(1.0) - exp(-screen * exposure);
    mapped = pow(mapped, vec3(1.0 / gamma));

    FragColor = vec4(mapped, 1.0);
}