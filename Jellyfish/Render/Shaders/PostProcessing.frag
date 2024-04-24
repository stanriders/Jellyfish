#version 460
out vec4 FragColor;
  
in vec2 TexCoords;

uniform bool isEnabled;

layout(binding=0) uniform sampler2D screenTexture;
layout(binding=1) uniform sampler2D depthTexture;

void main()
{ 
    vec3 screen = texture(screenTexture, TexCoords).rgb;
    float depth = texture(depthTexture, TexCoords).r;

    if (!isEnabled)
    {
        FragColor = vec4(screen, depth);
        return;
    }

    depth = pow(depth, 20);

    FragColor = vec4(depth, depth, depth, depth);
}