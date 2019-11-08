#version 460

out vec4 outputColor;

in vec2 frag_texCoord;
in vec3 frag_normal;
in vec3 frag_position;

uniform sampler2D diffuseSampler;
uniform sampler2D normalSampler;
uniform vec3 cameraPos;

struct Light {
    vec3 position;

    float constant;
    float linear;
    float quadratic;

    vec3 ambient;
    vec3 diffuse;
    vec3 specular;
};  
uniform Light lightSources[4];

vec3 CalcPointLight(Light light, vec3 normal, vec3 fragPos, vec3 viewDir, vec3 diffuse)
{
    vec3 lightDir = normalize(light.position - fragPos);

    //diffuse shading
    float diff = max(dot(normal, lightDir), 0.0);

    //attenuation
    float distance    = length(light.position - fragPos);
    float attenuation = 1.0 / (light.constant + light.linear * distance + light.quadratic * (distance * distance));

    //combine results
    vec3 ambient = light.ambient * diffuse;
    vec3 outdiffuse = light.diffuse * diff * diffuse;

    //ambient  *= attenuation;
    //outdiffuse  *= attenuation;

    return (ambient + outdiffuse);
}

vec3 CalcLighting(vec3 normal, vec3 fragPos, vec3 viewDir, vec3 diffuse)
{
    vec3 result = vec3(0.0, 0.0, 0.0);

    for(int i = 0; i < 4; i++)
        result += CalcPointLight(lightSources[i], normal, fragPos, viewDir, diffuse);

    return result;
}

void main()
{
    vec4 diffuseTex = texture(diffuseSampler, frag_texCoord * vec2(1.0, -1.0));

	/*
    vec3 normalTex = texture(normalSampler, frag_texCoord * vec2(1.0, -1.0)).rgb;
    vec3 tangentSpaceNormal = normalize(normalTex * 2.0 - 1.0);
	*/

    vec3 pos = normalize(cameraPos - frag_position);

    vec3 lighting = CalcLighting(frag_normal, frag_position, pos, diffuseTex.rgb);

    outputColor = vec4(lighting, 1.0);
}

