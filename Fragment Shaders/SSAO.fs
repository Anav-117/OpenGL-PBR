#version 330 core

out float FragColor;

in vec2 TexCoord;

uniform mat4 view;
uniform mat4 projection;
uniform sampler2D vPosition;
uniform sampler2D gNormal;
uniform sampler2D texNoise;

uniform vec3 samples[64];

const vec2 noiseScale = vec2(1920.0/2.0, 1080.0/2.0);

void main() {
	vec3 fragPos = (texture(vPosition, TexCoord)).rgb;
	vec3 normal = (texture(gNormal, TexCoord)).rgb;
	vec3 randomVec = texture(texNoise, TexCoord * noiseScale).rgb;

	vec3 tangent = normalize(randomVec - normal * dot(randomVec, normal));
	vec3 bitangent = cross(normal, tangent);
	mat3 TBN = mat3(tangent, bitangent, normal);
	
	float bias = 0.025;

	float occlusion = 0.0;
	for (int i=0; i < 16; i++) { //kernelSize = 16
		vec3 samplePos = samples[i];
		samplePos = fragPos + samplePos * 0.1; //radius = 0.1

		vec4 offset = vec4(samplePos, 1.0);
		offset = projection * offset;
		offset.xyz /= offset.w;
		offset.xyz = offset.xyz * 0.5 + 0.5;

		float sampleDepth = texture(vPosition, offset.xy).z;

		float rangeCheck = smoothstep(0.0, 1.0, 0.1 / abs(fragPos.z - sampleDepth)); //radius = 0.1
		occlusion += ((sampleDepth >= samplePos.z + bias) ? 1.0 : 0.0) * rangeCheck;
	}

	occlusion = 1.0 - (occlusion / 16); //kernelSize = 16
	FragColor = occlusion;
}