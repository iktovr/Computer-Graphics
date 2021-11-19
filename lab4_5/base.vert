#version 330 core

layout (location = 0) in vec3 position;
layout (location = 1) in vec3 inNormal;

out vec3 normal;
out vec3 fragCoord;

uniform mat4 proj;
uniform mat4 view;
uniform mat4 model;

void main()
{
    mat4 viewmodel = view * model;
    normal = normalize(vec3(viewmodel * vec4(inNormal, 0.0)));
    fragCoord = vec3(viewmodel * vec4(position, 1.0));
    gl_Position = proj * viewmodel * vec4(position, 1.0);
}