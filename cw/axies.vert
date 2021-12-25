#version 330

layout (location = 0) in vec3 position;
layout (location = 1) in vec3 inColor;

out vec3 color;

uniform mat4 view;
uniform float corner;

void main() {
    color = inColor;
    gl_Position = view * vec4(position / 6.0, 0.0);
    gl_Position.w = 1.0;
    gl_Position.z = 0.0;
    gl_Position += vec4(corner, corner, 0.0, 0.0);
}
