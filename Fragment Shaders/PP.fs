#version 330 core

out vec4 FragColor;
in vec2 TexCoord;

uniform sampler2D screenTexture;
//uniform sampler2D bloomTexture;
//uniform float exposure;

const float offset = 1.0 / 300.0;

vec3 ApplyKernel(float kernel[9]);

void main() {
    vec3 HDRcol = vec3(texture(screenTexture, TexCoord));
    //vec3 Bloom = vec3(texture(bloomTexture, TexCoord));

    //HDRcol += Bloom;

    //Tone Mapping
    //vec3 mapped = vec3(1.0) - exp(-HDRcol* exposure);

    //float kernel[9] = float[] (
    //    1.0 / 16, 2.0 / 16, 1.0 / 16,
    //    2.0 / 16, 4.0 / 16, 2.0 / 16,
    //    1.0 / 16, 2.0 / 16, 1.0 / 16 
    //);

    //col = ApplyKernel(kernel);

	FragColor = vec4(HDRcol, 1.0f);
    //float depthValue = texture(screenTexture, TexCoord).r;
    //FragColor = vec4(vec3(depthValue), 1.0);
}

vec3 ApplyKernel(float kernel[9]) {
    vec2 offsets[9] = vec2[](
		vec2(-offset,  offset), // top-left
        vec2( 0.0f,    offset), // top-center
        vec2( offset,  offset), // top-right
        vec2(-offset,  0.0f),   // center-left
        vec2( 0.0f,    0.0f),   // center-center
        vec2( offset,  0.0f),   // center-right
        vec2(-offset, -offset), // bottom-left
        vec2( 0.0f,   -offset), // bottom-center
        vec2( offset, -offset)  // bottom-right    
	);

    vec3 sampleTex[9];
    for (int i=0; i<9; i++) {
        sampleTex[i] = vec3(texture(screenTexture, TexCoord.st + offsets[i]));
    }
    vec3 col = vec3(0.0f);
    for (int i=0; i<9; i++)
        col += sampleTex[i] * kernel[i];

    return col;
}