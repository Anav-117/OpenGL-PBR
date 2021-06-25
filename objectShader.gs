#version 330 core
layout (triangles) in;
layout (triangle_strip, max_vertices = 3) out;

in VS_OUT_G {
	vec2 TexCoord;
} gs_in[];

out vec2 TexCoord;

uniform float time;

vec3 getNormal() {
	vec3 a = vec3(gl_in[0].gl_Position) - vec3(gl_in[1].gl_Position);
	vec3 b = vec3(gl_in[2].gl_Position) - vec3(gl_in[1].gl_Position);
	return normalize(cross(a, b));
}

vec4 explode(vec4 position, vec3 normal) {
	float magnitude = 10.0;
	vec3 direction = normal * time * magnitude;
	return position + vec4(direction, 0.0);
}

void main() {
	vec3 normal = getNormal();

	gl_Position = explode(gl_in[0].gl_Position, normal);
	TexCoord = gs_in[0].TexCoord;
	EmitVertex();
	gl_Position = explode(gl_in[1].gl_Position, normal);
	TexCoord = gs_in[1].TexCoord;
	EmitVertex();
	gl_Position = explode(gl_in[2].gl_Position, normal);
	TexCoord = gs_in[2].TexCoord;
	EmitVertex();
	EndPrimitive();
}