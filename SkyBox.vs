#version 330 core
layout (location = 0) in vec3 aPos;

out vec3 TexCoord;

uniform mat4 projection;
uniform mat4 view;

void main() {
	mat4 rotView = mat4(mat3(view));
	vec4 pos = projection * rotView * vec4(aPos, 1.0f);
	gl_Position = pos.xyww;
	TexCoord = aPos;
}