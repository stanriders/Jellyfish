#version 460

layout (binding = 0) uniform sampler2D diffuseSampler;
uniform bool hasAlphaTest;

in vec2 frag_texCoord;

void main()
{
    if (hasAlphaTest && texture(diffuseSampler, frag_texCoord * vec2(1.0, -1.0)).a < 0.5)
        discard;
}