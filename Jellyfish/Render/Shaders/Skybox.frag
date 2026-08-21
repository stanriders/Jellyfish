#version 460 core
precision highp float;

out vec4 FragColor;

in vec3 TexCoords;

uniform vec3 uSunPos;
uniform float uSunIntensity;

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

void main()
{
    vec3 color = hosek_wilkie_sky_rgb(normalize(TexCoords), uSunPos);

    const float sunAngularRadius = 0.007;
    float cosAngle = dot(normalize(TexCoords), normalize(uSunPos));
    float sun = smoothstep(cos(sunAngularRadius), cos(0.0), cosAngle);
    color += vec3(uSunIntensity * 0.25) * sun;

    //vec3 groundColor = vec3(0.3, 0.25, 0.2);
    //color = mix(groundColor, color, smoothstep(0.0, 0.1, TexCoords.y));
    
    FragColor = vec4(color, 1);
}