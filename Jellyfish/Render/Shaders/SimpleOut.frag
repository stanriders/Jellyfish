#version 330

out vec4 outputColor;

in vec2 texCoord;
in vec3 normal;
in vec3 position;

uniform sampler2D texture0;
uniform vec3 cameraPos;

void main()
{
    vec4 tex = texture(texture0, texCoord * vec2(1.0, -1.0));
    vec3 pos = normalize(cameraPos - position);
    outputColor = min(tex * dot(pos, normal), 1.0); // darken normals facing away from camera
}