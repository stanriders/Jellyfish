#version 460
#include CommonFrag.frag

layout(binding=0) uniform sampler2D uSceneColor;  // scene color buffer
layout(binding=1) uniform sampler2D uDepth;       // depth buffer
layout(binding=2) uniform sampler2D uNormal;      // normal buffer

uniform mat4 uProjection;
uniform vec2 uCameraParams;
#define Near          uCameraParams.x
#define Far           uCameraParams.y

uniform float uThickness = 0.1;        // thickness bias (in view space units)
uniform int   uMaxSteps = 64;          // max ray-march steps
uniform int   uBinarySearchSteps = 4;  // binary refine iterations after hit
uniform float uStride = 5.0;           // initial stride multiplier (in view space units)
uniform float uMaxDistance = 1000.0;     // max ray march distance in view-space

in vec2 TexCoords;
out vec4 FragColor;

vec3 GetViewPos(vec2 uv, float near, float far) 
{
    float z = GetDepth(uDepth, uv, near, far); 
    float x = (uv.x * 2.0 - 1.0) * z / uProjection[0][0];
    float y = (uv.y * 2.0 - 1.0) * z / uProjection[1][1];
    return vec3(x, y, -z);
}

vec2 ProjectToUV(vec3 viewPos) 
{
    vec4 clip = uProjection * vec4(viewPos, 1.0);
    clip /= clip.w;
    return clip.xy * 0.5 + 0.5;
}

bool screenIntersect(in vec3 originVS, in vec3 dirVS, out vec2 hitUV, out vec3 hitVS, out float hitDepthView) 
{
    float step = uStride;
    float traveled = 0.0;

    // Make origin slightly offset to avoid self-intersection
    vec3 pos = originVS + dirVS * 0.001;

    for (int i = 0; i < uMaxSteps; ++i) {
        traveled += step;
        pos = originVS + dirVS * traveled;

        if (traveled > uMaxDistance) break;

        vec2 uv = ProjectToUV(pos);
        if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0) {
            continue;
        }

        float depthSample = GetDepth(uDepth, uv, Near, Far);
        vec3 sampleVS = GetViewPos(uv, Near, Far);

        // Because view-space z is negative forward, an intersection occurs when sampled geometry
        // is *in front of* the candidate point: i.e. sampleZ >= pos.z - thickness
        // (less negative means closer to camera)
        if (sampleVS.z >= pos.z - uThickness) {
            // found coarse hit, refine with binary search between previous and current traveled distance
            float t0 = max(0.0, traveled - step);
            float t1 = traveled;
            for (int b = 0; b < uBinarySearchSteps; ++b) {
                float tm = 0.5 * (t0 + t1);
                vec3 pm = originVS + dirVS * tm;
                vec2 uvm = ProjectToUV(pm);

                float dsm = GetDepth(uDepth, uvm, Near, Far);
                vec3 sampleVSm = GetViewPos(uvm, Near, Far);

                if (sampleVSm.z >= pm.z - uThickness) {
                    t1 = tm;
                } else {
                    t0 = tm;
                }
            }
            float tHit = t1;
            vec3 pHit = originVS + dirVS * tHit;
            vec2 uvHit = ProjectToUV(pHit);
            float dHit = GetDepth(uDepth, uvHit, Near, Far);
            vec3 sampleHitVS = GetViewPos(uvHit, Near, Far);

            // final acceptance check: make sure hit is plausibly close
            if (abs(sampleHitVS.z - pHit.z) <= max(0.01, abs(pHit.z) * 0.01)) {
                hitUV = uvHit;
                hitVS = pHit;
                hitDepthView = sampleHitVS.z;
                return true;
            } else {
                // treat as miss
                return false;
            }
        }

        step = uStride * (1.0 + float(i) * 0.05);
    }

    return false;
}

void main() 
{
    vec3 baseColor = texture(uSceneColor, TexCoords).rgb;
    vec3 normalVS = GetNormal(uNormal, TexCoords);

    float d = GetDepthNormalized(uDepth, TexCoords, Near, Far);
    if (d > 0.99) 
    {
        FragColor = vec4(0);
        return; // sky / no geometry
    }
    
    vec3 viewPos = GetViewPos(TexCoords, Near, Far);
    vec3 V = normalize(-viewPos); // view direction in view-space

    vec3 R = reflect(-V, normalVS); // reflect view vector around normal
    R = normalize(R);

    // Ray origin offset to avoid self intersection
    vec3 originVS = viewPos + normalVS * uThickness;

    // Perform screen-space intersection
    vec2 hitUV;
    vec3 hitVS;
    float hitDepthView;
    bool hit = screenIntersect(originVS, R, hitUV, hitVS, hitDepthView);

    float confidence = 1.0;
    vec3 reflColor = vec3(0.0);
    if (hit) 
    {
        reflColor = texture(uSceneColor, hitUV).rgb;

        float NdotV = max(dot(normalVS, V), 0.0);
        float fresnel = pow(1.0 - NdotV, 3.0);
        confidence *= fresnel;

        float edgeFade = clamp(min(min(hitUV.x, 1.0 - hitUV.x), min(hitUV.y, 1.0 - hitUV.y)) * 10.0, 0.0, 1.0);
        confidence *= edgeFade;
    } 
    else 
    {
        confidence = 0.0;
    }

    FragColor = vec4(reflColor, confidence);
}