#version 460 core
precision highp float;

out vec4 FragColor;

in vec3 TexCoords;

uniform vec3 uSunPos;
uniform float uSunIntensity;
uniform vec3 uSunColor;
uniform float uTime;
uniform float uCoverage;

uniform vec3 A, B, C, D, E, F, G, H, I, Z;


vec3 hosek_wilkie(float cos_theta, float gamma, float cos_gamma)
{
	vec3 chi = (1 + cos_gamma * cos_gamma) / pow(1 + H * H - 2 * cos_gamma * H, vec3(1.5));
    return (1 + A * exp(B / (cos_theta + 0.01))) * (C + D * exp(E * gamma) + F * (cos_gamma * cos_gamma) + G * chi + I * sqrt(cos_theta));
}

vec3 hosek_wilkie_sky_rgb(vec3 v, vec3 sun_dir)
{
    float cos_theta = clamp(v.y, 0, 1);
	float cos_gamma = clamp(dot(v, sun_dir), 0, 1);
	float gamma_ = acos(cos_gamma);

	vec3 R = Z * hosek_wilkie(cos_theta, gamma_, cos_gamma);
    return R;
}

float hash(vec2 p)
{
    p = fract(p * vec2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return fract(p.x * p.y);
}

float noise(vec2 p)
{
    vec2 i = floor(p);
    vec2 f = fract(p);
    float a = hash(i);
    float b = hash(i + vec2(1.0, 0.0));
    float c = hash(i + vec2(0.0, 1.0));
    float d = hash(i + vec2(1.0, 1.0));
    vec2 u = f * f * (3.0 - 2.0 * f);
    return mix(mix(a, b, u.x), mix(c, d, u.x), u.y);
}

float fbm(vec2 p)
{
    float sum = 0.0;
    float amp = 0.5;
    for (int i = 0; i < 6; i++)
    {
        sum += amp * noise(p);
        p *= 2.02;
        amp *= 0.5;
    }
    return sum;
}

vec4 clouds(vec3 dir, vec3 sunDir)
{
    // only draw above the horizon, project the ray onto a flat plane
    if (dir.y < 0.01) return vec4(0.0);

    float cloudHeight = 1500.0;
    float t = cloudHeight / dir.y;
    vec2 uv = dir.xz * t * 0.0006; // scale controls cloud "size"

    vec2 wind = vec2(uTime * 0.015, uTime * 0.008);
    float n = fbm(uv + wind);

    float coverage = 1 - uCoverage;   // higher = more sky covered
    float softness  = 0.35;  // higher = softer edges
    float density = smoothstep(coverage - softness, coverage + softness, n);

    // cheap fake lighting: brighter where noise gradient faces the sun
    float nSun = fbm(uv + wind + normalize(sunDir.xz + 1e-4) * 0.02);
    float lightTerm = clamp((nSun - n) * 4.0 + 0.6, 0.2, 1.0);

    // sample the sky itself for lighting instead of hardcoded colors
    vec3 skyAmbient = hosek_wilkie_sky_rgb(vec3(0.0, 1.0, 0.0), sunDir);       // zenith skylight, for shadowed side
    vec3 sunColor   = hosek_wilkie_sky_rgb(normalize(sunDir), sunDir) + (uSunColor * vec3(uSunIntensity)); // sun-facing side

    vec3 cloudColor = mix(skyAmbient * 0.8, sunColor, lightTerm);

    // fade near horizon so it doesn't look like a hard flat plane
    float horizonFade = smoothstep(0.01, 0.15, dir.y);
    float alpha = density * horizonFade;

    return vec4(cloudColor, alpha);
}

void main()
{
    vec3 dir = normalize(TexCoords);
    vec3 color = hosek_wilkie_sky_rgb(dir, uSunPos);

    const float sunAngularRadius = 0.007;
    float cosAngle = dot(dir, normalize(uSunPos));
    float sun = smoothstep(cos(sunAngularRadius), cos(0.0), cosAngle);
    color += uSunColor * vec3(uSunIntensity) * sun;

    //vec3 groundColor = vec3(0.3, 0.25, 0.2);
    //color = mix(groundColor, color, smoothstep(0.0, 0.1, TexCoords.y));
    
    vec4 cloud = clouds(dir, normalize(uSunPos));
    color = mix(color, cloud.rgb, cloud.a);

    FragColor = vec4(color, 1);
}