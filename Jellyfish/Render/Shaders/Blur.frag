#version 460

layout(binding=0) uniform sampler2D sourceSampler;

uniform vec2 screenSize;
uniform int direction;
uniform int size;

in vec2 TexCoords;
out vec4 FragColor;

// https://github.com/Experience-Monks/glsl-fast-gaussian-blur
vec4 blur13(sampler2D image, vec2 uv, vec2 resolution, vec2 direction) {
  vec4 color = vec4(0.0);
  vec2 off1 = vec2(1.411764705882353) * direction;
  vec2 off2 = vec2(3.2941176470588234) * direction;
  vec2 off3 = vec2(5.176470588235294) * direction;
  color += texture2D(image, uv) * 0.1964825501511404;
  color += texture2D(image, uv + (off1 / resolution)) * 0.2969069646728344;
  color += texture2D(image, uv - (off1 / resolution)) * 0.2969069646728344;
  color += texture2D(image, uv + (off2 / resolution)) * 0.09447039785044732;
  color += texture2D(image, uv - (off2 / resolution)) * 0.09447039785044732;
  color += texture2D(image, uv + (off3 / resolution)) * 0.010381362401148057;
  color += texture2D(image, uv - (off3 / resolution)) * 0.010381362401148057;
  return color;
}

vec4 blur9(sampler2D image, vec2 uv, vec2 resolution, vec2 direction) {
  vec4 color = vec4(0.0);
  vec2 off1 = vec2(1.3846153846) * direction;
  vec2 off2 = vec2(3.2307692308) * direction;
  color += texture2D(image, uv) * 0.2270270270;
  color += texture2D(image, uv + (off1 / resolution)) * 0.3162162162;
  color += texture2D(image, uv - (off1 / resolution)) * 0.3162162162;
  color += texture2D(image, uv + (off2 / resolution)) * 0.0702702703;
  color += texture2D(image, uv - (off2 / resolution)) * 0.0702702703;
  return color;
}

vec4 blur5(sampler2D image, vec2 uv, vec2 resolution, vec2 direction) {
  vec4 color = vec4(0.0);
  vec2 off1 = vec2(1.3333333333333333) * direction;
  color += texture2D(image, uv) * 0.29411764705882354;
  color += texture2D(image, uv + (off1 / resolution)) * 0.35294117647058826;
  color += texture2D(image, uv - (off1 / resolution)) * 0.35294117647058826;
  return color; 
}

const float weight[13] = float[](
    0.196482, 0.176032, 0.120981, 0.064759, 0.027994,
    0.009300, 0.002841, 0.000736, 0.000157,
    0.000028, 0.000004, 0.000001, 0.0000002
);

vec4 blur_slow(sampler2D image, vec2 uv, vec2 resolution, int direction, int weightCount)
{             
    vec2 tex_offset = 1.0 / textureSize(image, 0); // gets size of single texel
    vec3 result = texture(image, uv).rgb * weight[0]; // current fragment's contribution

    if(direction == 0)
    {
        for(int i = 1; i < weightCount; ++i)
        {
            result += texture(image, uv + vec2(tex_offset.x * i, 0.0)).rgb * weight[i];
            result += texture(image, uv - vec2(tex_offset.x * i, 0.0)).rgb * weight[i];
        }
    }
    else
    {
        for(int i = 1; i < weightCount; ++i)
        {
            result += texture(image, uv + vec2(0.0, tex_offset.y * i)).rgb * weight[i];
            result += texture(image, uv - vec2(0.0, tex_offset.y * i)).rgb * weight[i];
        }
    }
    return vec4(result, 1.0);
}

void main()
{ 
    vec2 directionVec = vec2(1, 0);
    if (direction == 1)
        directionVec = vec2(0, 1);
    
    if (size == 0)
    {
        FragColor = blur5(sourceSampler, TexCoords, screenSize, directionVec);
        return;
    }
    else if (size == 1)
    {
        FragColor = blur9(sourceSampler, TexCoords, screenSize, directionVec);
        return;
    }
    else if (size == 2)
    {
        FragColor = blur13(sourceSampler, TexCoords, screenSize, directionVec);
        return;
    }
    else if (size == 3)
    {
        FragColor = blur_slow(sourceSampler, TexCoords, screenSize, direction, 5);
        return;
    }
    else if (size == 4)
    {
        FragColor = blur_slow(sourceSampler, TexCoords, screenSize, direction, 9);
        return;
    }
    else if (size == 5)
    {
        FragColor = blur_slow(sourceSampler, TexCoords, screenSize, direction, 13);
        return;
    }
}