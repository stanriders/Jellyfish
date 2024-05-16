#version 460

// The pragma below is critical for optimal performance
// in this fragment shader to let the shader compiler
// fully optimize the maths and batch the texture fetches
// optimally
//#pragma optionNV(unroll all)

out vec4 FragColor;
  
in vec2 TexCoords;
in vec2 ViewRay;

const int RANDOM_TEXTURE_WIDTH = 4;
const int MAX_KERNEL_SIZE = 64;


layout(binding=0) uniform sampler2D screenTexture;
layout(binding=1) uniform sampler2D depthTexture;
layout(binding=2) uniform sampler2D noiseTexture;

uniform bool isEnabled;
//uniform mat4 gProj;
uniform vec4 vScreenSize;
uniform vec4 vSettings;
uniform vec4 projInfo;

float radius = vSettings.x;
float strength = vSettings.y;
float bias = vSettings.z;
float radiusToScreen = vSettings.w;

#define M_PI 3.14159265f

#define SAMPLE_FIRST_STEP 1 // adds 1 texture slot, and approximately 40 arithmetic
//original high quality #define NUM_DIRECTIONS 7 // 8 for a 'fine' mode
//original high quality #define NUM_STEPS 5 // 6 for a 'fine' mode
#define NUM_DIRECTIONS 7 // 8 for a 'fine' mode
#define NUM_STEPS 5 // 6 for a 'fine' mode


float g_R = radius;
float g_R2 = (g_R * g_R);
float g_NegInvR2 = (-1.0f / g_R2);

#define DegToRad (M_PI / 180.0f)

float g_AngleBias = (strength * DegToRad);

float near = 0.05f;
float far = 10000.0f;


float LinearizeDepth(float depth) 
{
    //float z = depth * 2.0 - 1.0; // back to NDC 
    //return (2.0 * near * far) / (far + near - z * (far - near));	
    return (near * far) / (far - depth * (far - near));	
}


vec3 UVToView(vec2 uv, float eye_z)
{
  return vec3((uv * projInfo.xy + projInfo.zw) * eye_z, eye_z);
}


vec3 FetchViewPos(vec2 UV)
{
  float ViewDepth = LinearizeDepth(textureLod(depthTexture,UV,0).x);
  return UVToView(UV, ViewDepth);
}

vec3 MinDiff(vec3 P, vec3 Pr, vec3 Pl)
{
  vec3 V1 = Pr - P;
  vec3 V2 = P - Pl;
  return (dot(V1,V1) < dot(V2,V2)) ? V1 : V2;
}

vec3 ReconstructNormal(vec2 UV, vec3 P)
{
  vec3 Pr = FetchViewPos(UV + vec2(vScreenSize.x, 0));
  vec3 Pl = FetchViewPos(UV + vec2(-vScreenSize.x, 0));
  vec3 Pt = FetchViewPos(UV + vec2(0, vScreenSize.y));
  vec3 Pb = FetchViewPos(UV + vec2(0, -vScreenSize.y));
  return normalize(cross(MinDiff(P, Pr, Pl), MinDiff(P, Pt, Pb)));
}

//----------------------------------------------------------------------------------
float Falloff(float DistanceSquare)
{
  // 1 scalar mad instruction
  return DistanceSquare * g_NegInvR2 + 1.0;
}

//----------------------------------------------------------------------------------
// P = view-space position at the kernel center
// N = view-space normal at the kernel center
// S = view-space position of the current sample
//----------------------------------------------------------------------------------
float ComputeAO(vec3 P, vec3 N, vec3 S)
{
  vec3 V = S - P;
  float VdotV = dot(V, V);
  float NdotV = dot(N, V) * 1.0/sqrt(VdotV);

  // Use saturate(x) instead of max(x,0.f) because that is faster on Kepler
  return clamp(NdotV - bias,0,1) * clamp(Falloff(VdotV),0,1);
}

//----------------------------------------------------------------------------------
vec2 RotateDirection(vec2 Dir, vec2 CosSin)
{
  return vec2(Dir.x*CosSin.x - Dir.y*CosSin.y,
              Dir.x*CosSin.y + Dir.y*CosSin.x);
}

//----------------------------------------------------------------------------------
vec4 GetJitter()
{
  // (cos(Alpha),sin(Alpha),rand1,rand2)
  return textureLod( noiseTexture, (gl_FragCoord.xy / RANDOM_TEXTURE_WIDTH), 0);
}

//----------------------------------------------------------------------------------
float ComputeCoarseAO(vec2 FullResUV, float RadiusPixels, vec4 Rand, vec3 ViewPosition, vec3 ViewNormal)
{
  // Divide by NUM_STEPS+1 so that the farthest samples are not fully attenuated
  float StepSizePixels = RadiusPixels / (NUM_STEPS + 1);

  const float Alpha = 2.0 * M_PI / NUM_DIRECTIONS;
  float AO = 0;

  for (float DirectionIndex = 0; DirectionIndex < NUM_DIRECTIONS; ++DirectionIndex)
  {
    float Angle = Alpha * DirectionIndex;

    // Compute normalized 2D direction
    vec2 Direction = RotateDirection(vec2(cos(Angle), sin(Angle)), Rand.xy);

    // Jitter starting sample within the first step
    float RayPixels = (Rand.z * StepSizePixels + 1.0);

    for (float StepIndex = 0; StepIndex < NUM_STEPS; ++StepIndex)
    {
      vec2 SnappedUV = round(RayPixels * Direction) * vScreenSize.xy + FullResUV;
      vec3 S = FetchViewPos(SnappedUV);

      RayPixels += StepSizePixels;

      AO += ComputeAO(ViewPosition, ViewNormal, S);
    }
  }

  AO *= (1.0f / (1.0f - bias)) / (NUM_DIRECTIONS * NUM_STEPS);
  return clamp(1.0 - AO * 2.0,0,1);
}

float runSSAO()
{
    vec2 uv = TexCoords;
  vec3 ViewPosition = FetchViewPos(uv);

  // Reconstruct view-space normal from nearest neighbors
  vec3 ViewNormal = -ReconstructNormal(uv, ViewPosition);

  // Compute projection of disk of radius control.R into screen space
  float RadiusPixels = radiusToScreen / ViewPosition.z;

  // Get jitter vector for the current full-res pixel
  vec4 Rand = GetJitter();

  float AO = ComputeCoarseAO(uv, RadiusPixels, Rand, ViewPosition, ViewNormal);
  return pow(AO, strength); // control.PowExponent
}

void main()
{ 
    vec3 screen = texture(screenTexture, TexCoords).rgb;
    float depth = texture(depthTexture, TexCoords).r;

    if (!isEnabled)
    {
        FragColor = vec4(screen, depth);
        return;
    }

    //depth = pow(depth, 20);

    vec3 ssao = vec3(/*runSSAO()*/1.0f);

    FragColor = vec4(screen * ssao, depth);
}