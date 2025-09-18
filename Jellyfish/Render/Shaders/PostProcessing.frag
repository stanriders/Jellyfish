#version 460
#include Fxaa.frag

out vec4 FragColor;
  
in vec2 TexCoords;

uniform float exposure;
uniform bool isEnabled;

uniform vec2 screenSize;
uniform int toneMappingMode;
#define SourceSizeRecp vec2(1.0 / screenSize)

layout(binding=0) uniform sampler2D screenTexture;
layout(binding=1) uniform sampler2D aoTexture;

#define WhitePoint_Hejl 1.0f
#define WhitePoint_Hable 6.0f
#define ShoulderStrength 4.0f
#define LinearStrength 5.0f
#define LinearAngle 0.12f
#define ToeStrength 13.0f

vec3 RRTAndODTFit(vec3 v)
{
    vec3 a = v * (v + 0.0245786) - 0.000090537;
    vec3 b = v * (0.983729 * v + 0.4329510) + 0.238081;
    return a / b;
}

const mat3 ACESInputMat = mat3(
     0.59719, 0.35458, 0.04823,
     0.07600, 0.90834, 0.01566,
     0.02840, 0.13383, 0.83777
);

const mat3 ACESOutputMat = mat3(
     1.60475, -0.53108, -0.07367,
    -0.10208,  1.10813, -0.00605,
    -0.00327, -0.07276,  1.07602
);

vec3 ACESFitted(vec3 color)
{
    color = ACESInputMat * color;
    color = RRTAndODTFit(color);
    color = ACESOutputMat * color;
    color = clamp(color, 0.0, 1.0);

    return color;
}

vec3 LinearTosRGB(in vec3 color)
{
    vec3 x = color * 12.92;
    vec3 y = 1.055 * pow(clamp(color, 0.0, 1.0), vec3(1.0 / 2.4)) - 0.055;

    vec3 clr = color;
    clr.r = (color.r < 0.0031308) ? x.r : y.r;
    clr.g = (color.g < 0.0031308) ? x.g : y.g;
    clr.b = (color.b < 0.0031308) ? x.b : y.b;

    return clr;
}

vec3 SRGBToLinear(in vec3 color)
{
    vec3 x = color / 12.92;
    vec3 y = pow(max((color + 0.055) / 1.055, 0.0), vec3(2.4));

    vec3 clr = color;
    clr.r = (color.r <= 0.04045) ? x.r : y.r;
    clr.g = (color.g <= 0.04045) ? x.g : y.g;
    clr.b = (color.b <= 0.04045) ? x.b : y.b;

    return clr;
}

// John Hable’s Filmic Curve
vec3 ToneMapFilmicALU(in vec3 color)
{
    color = max(vec3(0.0), color - 0.004);
    color = (color * (6.2 * color + 0.5)) / (color * (6.2 * color + 1.7) + 0.06);
    return color;
}

// Hejl 2015
vec3 ToneMap_Hejl2015(in vec3 hdr)
{
    vec4 vh = vec4(hdr, WhitePoint_Hejl);
    vec4 va = (1.435 * vh) + 0.05;
    vec4 vf = ((vh * va + 0.004) / (vh * (va + 0.55) + 0.0491)) - 0.0821;
    return LinearTosRGB(vf.xyz / vf.www);
}

// Hable function
vec3 HableFunction(in vec3 x)
{
    const float E = 0.01;
    const float F = 0.3;

    float A = ShoulderStrength;
    float B = LinearStrength;
    float C = LinearAngle;
    float D = ToeStrength;

    return ((x * (A * x + C * B) + D * E) /
            (x * (A * x + B) + D * F)) - E / F;
}

// Hable tonemap
vec3 ToneMap_Hable(in vec3 color)
{
    vec3 numerator = HableFunction(color);
    vec3 denominator = HableFunction(vec3(WhitePoint_Hable));

    return LinearTosRGB(numerator / denominator);
}

vec3 ToneMap(in vec3 color)
{
    vec3 outputColor = vec3(0.0);
    if (toneMappingMode == 0)
        outputColor = LinearTosRGB(color);
    else if (toneMappingMode == 1)
        outputColor = ToneMapFilmicALU(color);
    else if (toneMappingMode == 2)
        outputColor = LinearTosRGB(ACESFitted(color) * 1.8);
    else if (toneMappingMode == 3)
        outputColor = ToneMap_Hejl2015(color);
    else if (toneMappingMode == 4)
        outputColor = ToneMap_Hable(color);

    return outputColor;
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

    vec3 exposedColor = screen * exposure;
    vec3 mapped = ToneMap(exposedColor);
    FragColor = vec4(mapped, 1.0);
}