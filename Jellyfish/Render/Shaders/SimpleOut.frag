#version 460

out vec4 FragColor;
in vec2 TexCoords;

layout(binding=0) uniform sampler2D screenTexture;

void main()
{ 
    FragColor = vec4(texture(screenTexture, TexCoords).rgb, 1.0);
}