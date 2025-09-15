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

vec3 RRTAndODTFit(vec3 v)
{
    vec3 a = v * (v + 0.0245786) - 0.000090537;
    vec3 b = v * (0.983729 * v + 0.4329510) + 0.238081;
    return a / b;
}

void main()
{ 

    if (!isEnabled)
    {
        FragColor = vec4(texture(screenTexture, TexCoords).rgb, 1.0);
        return;
    }

    vec3 screen = FxaaPixelShader(TexCoords, screenTexture, vec2(SourceSizeRecp.x, SourceSizeRecp.y));
    vec3 ao = vec3(texture(aoTexture, TexCoords).r);
    screen *= ao;

    const float gamma = 2.2;
    vec3 mapped = RRTAndODTFit(screen * exposure);
    mapped = pow(mapped, vec3(1.0 / gamma));

    FragColor = vec4(mapped, 1.0);
}