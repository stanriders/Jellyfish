#version 460
layout(binding=0) uniform sampler2D scene;

uniform float threshold;

out vec4 FragColor;
in vec2 TexCoords;

void main()
{
    vec3 color = texture(scene, TexCoords).rgb;

    float brightness = dot(color, vec3(0.2126, 0.7152, 0.0722)); // luminance
    if (brightness > threshold)
        FragColor = vec4(color, 1.0);
    else
        FragColor = vec4(0.0);
}