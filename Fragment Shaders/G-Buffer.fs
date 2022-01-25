#version 330 core

layout (location = 0) out vec3 gPosition;
layout (location = 1) out vec3 gNormal;
layout (location = 2) out vec4 gAlbedoSpec;
layout (location = 3) out vec3 vPosition;

in vec2 TexCoords;
in vec3 FragPos;
in vec3 FragPosV;
in vec3 Normal;
in mat3 TBN;

uniform sampler2D texture_diffuse1;
uniform sampler2D texture_specular1;
uniform sampler2D texture_normal1;

float bias = 0.0;

void main() {
	// store the fragment position vector in the first gbuffer texture
    gPosition = FragPos;
	vPosition = FragPosV;
    // also store the per-fragment normals into the gbuffer
    vec3 norm = texture(texture_normal1, TexCoords).rgb;
	norm = normalize(norm * 2.0 - 1.0);

	mat3 tbn = transpose(TBN);

	//checking for mirroring in normal map
	vec3 tangent = tbn[0];
	vec3 bitangent = tbn[1];
	vec3 CalcN = cross(tangent, bitangent);

	float normalsAligned = dot(CalcN, tbn[2]);

	if (normalsAligned < 0) {
		tangent = -tangent;
		tbn[0] = tangent;
		norm = normalize(tbn * norm);
	}
	else {
		norm = normalize(tbn * norm);
	}
	gNormal = norm;
    // and the diffuse per-fragment color
	if (texture(texture_diffuse1, TexCoords).a < 0.5) {
		discard;
	}
    gAlbedoSpec.rgb = texture(texture_diffuse1, TexCoords).rgb;
    // store specular intensity in gAlbedoSpec's alpha component
    gAlbedoSpec.a = texture(texture_specular1, TexCoords).r;
}

