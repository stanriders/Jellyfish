#version 460
out vec4 FragColor;
  
in vec2 TexCoords;

uniform float exposure;
uniform bool isEnabled;

layout(binding=0) uniform sampler2D screenTexture;

void main()
{ 
    vec3 screen = texture(screenTexture, TexCoords).rgb;

    if (!isEnabled)
    {
        FragColor = vec4(screen, 1.0);
        return;
    }

    const float gamma = 2.2;
    vec3 mapped = vec3(1.0) - exp(-screen * exposure);
    //mapped = pow(mapped, vec3(1.0 / gamma));

    FragColor = vec4(mapped, 1.0);
}