#version 330 core
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec2 aTexCoord;
layout (location = 3) in vec3 aTangent; 
layout (location = 4) in vec3 aBitangent;

out VS_OUT {
	vec2 TexCoord;
	vec4 FragPosLightSpace;
	mat3 TBN;
	vec3 TangentViewPos;
	vec3 TangentFragPos;
	vec3 ViewPos;
	vec3 FragPos;
} vs_out;

uniform mat4 projection;
uniform mat4 view;
uniform mat4 model;
uniform mat4 lightSpaceMatrix;
uniform vec3 lightPos;
uniform vec3 viewPos;

void main() {
gl_Position = projection * view * model * vec4(aPos, 1.0f);//texture attribute
vs_out.TexCoord = aTexCoord;
vs_out.FragPosLightSpace = lightSpaceMatrix * model * vec4(aPos, 1.0);
vec3 T = normalize(vec3(model * vec4(aTangent, 0.0)));
vec3 B = normalize(vec3(model * vec4(aBitangent, 0.0)));
vec3 N = normalize(vec3(model * vec4(aNormal, 0.0)));
mat3 TBN = mat3(T, B, N);
vs_out.TBN = transpose(TBN);
vs_out.TangentViewPos = TBN * viewPos;
vs_out.ViewPos = viewPos;
vs_out.FragPos = vec3(model * vec4(aPos, 1.0));
vs_out.TangentFragPos = TBN * vec3(model * vec4(aPos, 1.0f));
};