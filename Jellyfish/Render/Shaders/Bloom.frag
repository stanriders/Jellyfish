#version 460
layout(binding=0) uniform sampler2D scene;

out vec3 FragColor;
in vec2 TexCoords;

uniform float filterRadius;

void main()
{
    vec2 texelSize = 1.0 / vec2(textureSize(scene, 0));

    // The filter kernel is applied with a radius, specified in texture
    // coordinates, so that the radius will vary across mip resolutions.
    float x = (texelSize * filterRadius).x;
    float y = (texelSize * filterRadius).y;

    // Take 9 samples around current texel:
    // a - b - c
    // d - e - f
    // g - h - i
    // === ('e' is the current texel) ===
    vec3 a = texture(scene, vec2(TexCoords.x - x, TexCoords.y + y)).rgb;
    vec3 b = texture(scene, vec2(TexCoords.x,     TexCoords.y + y)).rgb;
    vec3 c = texture(scene, vec2(TexCoords.x + x, TexCoords.y + y)).rgb;

    vec3 d = texture(scene, vec2(TexCoords.x - x, TexCoords.y)).rgb;
    vec3 e = texture(scene, vec2(TexCoords.x,     TexCoords.y)).rgb;
    vec3 f = texture(scene, vec2(TexCoords.x + x, TexCoords.y)).rgb;

    vec3 g = texture(scene, vec2(TexCoords.x - x, TexCoords.y - y)).rgb;
    vec3 h = texture(scene, vec2(TexCoords.x,     TexCoords.y - y)).rgb;
    vec3 i = texture(scene, vec2(TexCoords.x + x, TexCoords.y - y)).rgb;

    // Apply weighted distribution, by using a 3x3 tent filter:
    //  1   | 1 2 1 |
    // -- * | 2 4 2 |
    // 16   | 1 2 1 |
    vec3 upsample = vec3(0);
    upsample = e*4.0;
    upsample += (b+d+f+h)*2.0;
    upsample += (a+c+g+i);
    upsample *= 1.0 / 16.0;

    FragColor = vec3(upsample);
}