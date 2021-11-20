#version 330 core

layout (location = 0) in vec3 position;
layout (location = 1) in vec3 inColor;
layout (location = 2) in vec3 inNormal;

out vec3 normal;
out vec3 fragCoord;
out vec3 color;

uniform mat4 proj;
uniform mat4 view;
uniform mat4 model;

uniform bool animate;
uniform uint curTime;

void main()
{
    mat4 viewmodel = view * model;
    normal = normalize(vec3(viewmodel * vec4(inNormal, 0.0)));
    fragCoord = vec3(viewmodel * vec4(position, 1.0));
    if (animate) {
        color = sin(asin(2.0 * inColor - 1.0) + float(curTime) / 3000000.0) * 0.5 + 0.5;
    } else {
        color = inColor;
    }
    gl_Position = proj * viewmodel * vec4(position, 1.0);
}