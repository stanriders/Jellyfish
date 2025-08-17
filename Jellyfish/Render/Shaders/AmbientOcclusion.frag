#version 460

layout(binding=0) uniform sampler2D depthSampler;
layout(binding=1) uniform sampler2D normalSampler;

uniform vec2 screenSize;
uniform vec3 cameraParams;
uniform vec4 gtaoParams;

in vec2 TexCoords;
out vec4 FragColor;

#define BFGTAO_QUALITY int(gtaoParams.x) // 0=Low, 1=Medium, 2=High, 3=Ultra
#define BFGTAO_RADIUS gtaoParams.y      // sample radius
#define BFGTAO_INTENSITY gtaoParams.z   // 0..1
#define BFGTAO_THICKNESS gtaoParams.w   // Z thickness
#define BFGTAO_THICKNESS_M 0.01f        // Z distance multiplier
#define BFGTAO_FADEOUT 0.9f             // 0..1
#define BFGTAO_GTAO_ATT 0.5f            // attenuation
#define BFGTAO_THIN_AVD 0.75f           // thin-object heuristic

#define BFGTAO_FovYDegrees   cameraParams.x
#define BFGTAO_Near          cameraParams.y
#define BFGTAO_Far           cameraParams.z
#define BFGTAO_RES           screenSize
#define BFGTAO_IASPECT       vec2(1.0, BFGTAO_RES.x / BFGTAO_RES.y)
#define BFGTAO_SAT(x)        clamp((x), 0.0, 1.0)

float BFGTAO_LinearizeDepthFromHardware(float z01)
{
    float n = BFGTAO_Near;
    float f = BFGTAO_Far;
    float z = z01 * 2.0 - 1.0;
    return (2.0 * n * f) / (f + n - z * (f - n));
}

float BFGTAO_GetDepth(vec2 uv)
{
    return BFGTAO_LinearizeDepthFromHardware(texture(depthSampler, uv).r) / BFGTAO_Far;
}

float BFGTAO_FL_INV()
{
    return 1.0 / tan(0.5 * radians(BFGTAO_FovYDegrees));
}

vec3 BFGTAO_GetEyePos(vec2 uv, float z)
{
    float fl = BFGTAO_FL_INV();
    vec3  m  = vec3(fl / BFGTAO_IASPECT, (BFGTAO_Far / (BFGTAO_Far - 1.0)));
    vec3  xyz = vec3(2.0 * uv - 1.0, 1.0);
    return (z * BFGTAO_Far + 1.0) * xyz * m;
}

vec3 BFGTAO_GetNormal(vec2 uv)
{
    vec3 normal = textureLod(normalSampler, uv, 0.0).xyz;
    normal.z *= -1f; // opengl is Special
    return normalize(normal);
}

const int BFGTAO_RAYQUALITY[4]  = int[4](2, 2, 4, 4);
const int BFGTAO_STEPQUALITY[4] = int[4](4, 8, 12, 24);

float BFGTAO_dotnv(vec3 a, vec3 b) { return dot(a, b) * inversesqrt(max(dot(a, a), 1e-8)); }

vec2 BFGTAO_FastAcos2(vec2 x) {
    return (-0.69813170 * x * x - 0.87266463) * x + 1.57079633;
}

// Bayer dither
float BFGTAO_Bayer(uvec2 p, uint level)
{
    p = (p ^ (p << uvec2(8u))) & uvec2(0x00ff00ffu);
    p = (p ^ (p << uvec2(4u))) & uvec2(0x0f0f0f0fu);
    p = (p ^ (p << uvec2(2u))) & uvec2(0x33333333u);
    p = (p ^ (p << uvec2(1u))) & uvec2(0x55555555u);

    uint i = (p.x ^ p.y) | (p.x << 1u);
    i = bitfieldReverse(i);
    i >>= (32u - level * 2u);
    return float(i) / float(1u << (2u * level));
}

vec2 BFGTAO_TraceSliceBF(
    vec2 xy, vec3 verPos, vec3 viewV, vec3 normal,
    vec2 vec2dir, float jit, inout uint BITFIELD, float dsign, float N, vec3 prjN)
{
    vec2 no  = 1.4 * dsign * vec2dir / BFGTAO_RES;
    vec2dir *= vec2(1.0, BFGTAO_RES.x / BFGTAO_RES.y);
    float h  = dsign * sin(N);
    float p  = 0.0;

    const int STEPS = BFGTAO_STEPQUALITY[BFGTAO_QUALITY];

    for (int i = 0; i < STEPS; i++)
    {
        float o = (float(i) + jit) / float(STEPS);
        o *= o * o;

        vec2 nxy = xy + no + dsign * 0.1 * BFGTAO_RADIUS * vec2dir * o;

        /*float samD = (o < 1.0)
                   ? BFGTAO_GetDepth(round(nxy * BFGTAO_RES) / BFGTAO_RES)
                   : BFGTAO_SampleDepth(nxy, 0.0);*/
        float samD = BFGTAO_GetDepth(round(nxy * BFGTAO_RES) / BFGTAO_RES);

        vec3 samPos = BFGTAO_GetEyePos(nxy, samD * 1.001);
        vec3 tv     = samPos - verPos;

        float tmx   = BFGTAO_dotnv(tv, viewV);
        vec2  minmax = BFGTAO_FastAcos2(vec2(
            tmx,
            BFGTAO_dotnv(normalize(samPos) * BFGTAO_THICKNESS +
                         (1.0 + BFGTAO_THICKNESS_M) * samPos - verPos, viewV)
        ));

        h = mix(h, max(h, tmx),
                mix(1.0, o, BFGTAO_THIN_AVD) *
                (1.0 / (BFGTAO_GTAO_ATT * dot(tv, tv) / length(samPos) + 1.0)));

        // angle span → bit coverage
        minmax = BFGTAO_SAT((dsign * -minmax - N + 1.5707) / 3.14159);
        if (minmax.x > minmax.y) minmax = minmax.yx;

        ivec2 abI = ivec2(round(32.0 * vec2(minmax.x, minmax.y - minmax.x)));
        abI = clamp(abI, ivec2(0), ivec2(32));

        uint mask;
        if (abI.y >= 32) mask = 0xffffffffu;
        else             mask = (abI.y == 0) ? 0u : ((1u << uint(abI.y)) - 1u);

        BITFIELD |= (mask << uint(abI.x));
    }

    return vec2(h, p);
}

float BFGTAO_BFAO(vec2 xy, vec3 verPos, vec3 viewV, vec3 n, vec2 noise)
{
    const int SLICES = BFGTAO_RAYQUALITY[BFGTAO_QUALITY];

    vec2 AOacc = vec2(0.0);
    for (int i = 0; i < SLICES; i++)
    {
        noise.x += 3.14159 / float(SLICES);
        vec2 v2  = vec2(sin(noise.x), cos(noise.x));

        vec3 slcN = normalize(cross(vec3(v2, 0.0), viewV));
        vec3 T    = cross(viewV, slcN);
        vec3 prjN = n - slcN * dot(n, slcN);
        float N   = -sign(dot(prjN, T)) * acos(clamp(dot(normalize(prjN), viewV), -1.0, 1.0));
        vec3 prjNN = normalize(prjN);

        uint BITFIELD = 0u;
        vec2 r0 = BFGTAO_TraceSliceBF(xy, verPos, viewV, n, v2, noise.y, BITFIELD,  1.0, N, prjNN);
        vec2 r1 = BFGTAO_TraceSliceBF(xy, verPos, viewV, n, v2, noise.y, BITFIELD, -1.0, N, prjNN);

        float coverage = 1.0 - float(bitCount(BITFIELD)) / 32.0;

        AOacc += length(prjN) * vec2(coverage, 1.0);
    }
    return BFGTAO_SAT(AOacc.x / max(AOacc.y, 1e-6));
}

float BFGTAO_ComputeAO(vec2 uv, ivec2 fragCoord)
{
    const float Bayer5[25] = {
        0.00, 0.48, 0.12, 0.60, 0.24,
        0.32, 0.80, 0.44, 0.92, 0.56,
        0.08, 0.40, 0.04, 0.52, 0.16,
        0.64, 0.96, 0.76, 0.28, 0.88,
        0.72, 0.20, 0.84, 0.36, 0.68
    };

    ivec2 pp = ivec2(fragCoord.x % 5, fragCoord.y % 5);
    float dir = Bayer5[pp.x + 5 * pp.y];

    float d = BFGTAO_GetDepth(uv);
    if (d > 0.99) return 1.0; // sky / no geometry

    vec3 verPos = BFGTAO_GetEyePos(uv, d);
    float vl    = length(verPos);
    vec3 normal = BFGTAO_GetNormal(uv);
    vec3 viewV  = -verPos / max(vl, 1e-6);

    vec2 noise = vec2(dir, BFGTAO_Bayer(uvec2(fragCoord), 3u));

    verPos += 0.002 * normal * vl;

    float AO = BFGTAO_BFAO(uv, verPos, viewV, normal,
                           vec2(3.14159 / float(BFGTAO_RAYQUALITY[BFGTAO_QUALITY]), 1.0) * noise);

    AO = mix(1.0, AO, BFGTAO_INTENSITY);
    AO = mix(1.0, AO, exp(-2.0 * (1.0 - BFGTAO_FADEOUT) * d));
    return AO;
}

void main()
{ 
    float ao = BFGTAO_ComputeAO(TexCoords, ivec2(gl_FragCoord.xy)); // todo: blur
    FragColor = vec4(vec3(ao), 1.0);
}