#version 460
#include Fxaa.frag

out vec4 FragColor;
  
in vec2 TexCoords;

uniform float exposure;
uniform vec2 screenSize;
uniform bool isEnabled;
#define SourceSize vec4(screenSize, 1.0 / screenSize)

layout(binding=0) uniform sampler2D screenTexture;

void main()
{ 
    if (!isEnabled)
    {
        FragColor = vec4(texture(screenTexture, TexCoords).rgb, 1.0);
        return;
    }

    vec3 screen = FxaaPixelShader(TexCoords, screenTexture, vec2(SourceSize.z, SourceSize.w));

    const float gamma = 2.2;
    vec3 mapped = vec3(1.0) - exp(-screen * exposure);
    //mapped = pow(mapped, vec3(1.0 / gamma));

    FragColor = vec4(mapped, 1.0);
}