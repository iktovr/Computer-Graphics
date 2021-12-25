#version 330 core

out vec4 fragColor;
in vec3 color;
uniform bool useSingleColor;
uniform vec3 singleColor;

void main()
{
	if (useSingleColor) {
		fragColor = vec4(singleColor, 1);
	} else {
		fragColor = vec4(color, 1);
	}
}