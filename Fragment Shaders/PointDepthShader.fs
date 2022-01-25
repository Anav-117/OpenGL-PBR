#version 330 core

in vec4 FragPos;

uniform vec3 lightPos;
uniform float far_plane;

out vec4 FragColor;

void main() {
	float lightDistance = length(FragPos.xyz - lightPos);

	lightDistance /= far_plane;

	gl_FragDepth = lightDistance;
}