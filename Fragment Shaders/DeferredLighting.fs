#version 330 core
out vec4 FragColor;

in vec2 TexCoord;

vec4 FragPosLightSpace;

uniform sampler2D gPosition;
uniform sampler2D gNormal;
uniform sampler2D gAlbedoSpec;
uniform sampler2D SSAO;
uniform mat4 lightSpaceMatrix;

struct Light {
	vec3 Position;
	vec3 Color;

	float Linear;
    float Quadratic;
};


struct DirLight {
    vec3 direction;

    vec3 ambient;
	vec3 diffuse;
};

const int NR_LIGHTS = 1;
uniform Light lights[NR_LIGHTS];
uniform vec3 viewPos;

uniform sampler2D shadowMap;
uniform DirLight dirlight;
uniform float far_plane;

float bias = 0.0;

vec3 CalculateDirectionalLight(DirLight light, vec3 Normal, vec3 Albedo, float AmbientOcclusion);
float GenerateDirectionalShadow(vec4 fragPosLightSpace);

vec3 FragPos;

vec3 sampleOffsetDirections[20] = vec3[]
(
   vec3( 1,  1,  1), vec3( 1, -1,  1), vec3(-1, -1,  1), vec3(-1,  1,  1), 
   vec3( 1,  1, -1), vec3( 1, -1, -1), vec3(-1, -1, -1), vec3(-1,  1, -1),
   vec3( 1,  1,  0), vec3( 1, -1,  0), vec3(-1, -1,  0), vec3(-1,  1,  0),
   vec3( 1,  0,  1), vec3(-1,  0,  1), vec3( 1,  0, -1), vec3(-1,  0, -1),
   vec3( 0,  1,  1), vec3( 0, -1,  1), vec3( 0, -1, -1), vec3( 0,  1, -1)
);   

void main() {
	FragPos = texture(gPosition, TexCoord).rgb;
	vec3 Normal = texture(gNormal, TexCoord).rgb;
	vec3 Albedo = texture(gAlbedoSpec, TexCoord).rgb;
	float Specular = texture(gAlbedoSpec, TexCoord).a;
	FragPosLightSpace = lightSpaceMatrix * vec4(FragPos, 1.0);
	float AmbientOcclusion = texture(SSAO, TexCoord).r;

	vec3 lighting = Albedo * 0.5 * AmbientOcclusion; //hardcoded ambient
	vec3 viewDir = normalize(viewPos - FragPos);
	//Directional Light
	lighting += CalculateDirectionalLight(dirlight, Normal, Albedo, AmbientOcclusion);

	for (int i=0; i<NR_LIGHTS; i++) {
		//diffuse
		vec3 lightDir = normalize(lights[i].Position - FragPos);
		vec3 diffuse = max(dot(lightDir, Normal), 0.0) * Albedo * lights[i].Color;
		// specular
        vec3 halfwayDir = normalize(lightDir + viewDir);  
        float spec = pow(max(dot(Normal, halfwayDir), 0.0), 16.0);
        vec3 specular = lights[i].Color * spec * Specular;
        // attenuation
        float distance = length(lights[i].Position - FragPos);
        float attenuation = 1.0 / (1.0 + lights[i].Linear * distance + lights[i].Quadratic * distance * distance);
        diffuse *= attenuation;
        specular *= attenuation;
        lighting += diffuse + specular;       
	}

	FragColor = vec4(lighting, 1.0);
}

vec3 CalculateDirectionalLight(DirLight light, vec3 Normal, vec3 Albedo, float AmbientOcclusion) {
    vec3 lightDir = normalize(-light.direction);

	float diff = max(dot(Normal, lightDir), 0.0);

	vec4 textureColour = vec4(Albedo, 1.0);
	if(textureColour.a < 0.5) {
		discard;
	}

	vec3 ambient = light.ambient * vec3(textureColour) * AmbientOcclusion;
	vec3 diffuse = vec3(textureColour);

	bias = max(0.05 * (1.0 - dot(Normal, lightDir)), 0.005);

	float shadow = GenerateDirectionalShadow(FragPosLightSpace);

	diffuse *= (1.0 - shadow);

	return (ambient + diffuse);
}

float GenerateDirectionalShadow(vec4 fragPosLightSpace) {
	//perspective divide
	vec3 projCoord = fragPosLightSpace.xyz / fragPosLightSpace.w;
	projCoord = projCoord * 0.5 + 0.5;
	float shadow = 0.0;
	float currentDepth = projCoord.z;
	float spread = 300.0;
	int samples  = 20;
	float viewDistance = length(viewPos - FragPos);
	float diskRadius = (1.0 + (viewDistance / far_plane)) / 25.0;;
	for(int i = 0; i < samples; ++i)
	{
		float closestDepth = texture(shadowMap, projCoord.xy + sampleOffsetDirections[i].xy * diskRadius / spread).r;
		if(currentDepth - bias > closestDepth)
			shadow += 1.0;
	}
	shadow /= float(samples); 

	if (projCoord.z > 1.0) {
		shadow = 0.0;
	}
	
	return shadow;
}