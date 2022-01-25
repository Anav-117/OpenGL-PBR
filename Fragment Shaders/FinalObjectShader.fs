#version 330 core
#define NR_POINT_LIGHTS 1
layout (location = 0) out vec4 FragColor;
layout (location = 1) out vec4 BrightColor;

in VS_OUT {
	vec2 TexCoord;
	vec4 FragPosLightSpace;
	mat3 TBN;
	vec3 TangentViewPos;
	vec3 TangentFragPos;
	vec3 ViewPos;
	vec3 FragPos;
} fs_in;

struct Material {
	sampler2D texture_diffuse1;
	sampler2D texture_specular1;
	sampler2D texture_normal1;
	float shininess;
};

struct DirLight {
	vec3 direction;

	vec3 ambient;
	vec3 diffuse;
	vec3 specular;
};

struct PointLight {
	vec3 position;

	float AttConstant;
	float AttLinear;
	float AttQuadratic;

	vec3 ambient;
	vec3 diffuse;
	vec3 specular;
};

uniform Material material;
uniform DirLight dirLight;
uniform PointLight pointLight[NR_POINT_LIGHTS];
uniform sampler2D shadowMap;
uniform samplerCube shadowCube[NR_POINT_LIGHTS];
uniform float far_plane;
float shininess;

vec3 CalcDirLight(DirLight light, vec3 Normal, vec3 ViewDir);
vec3 CalcPointLight(PointLight light, vec3 Normal, vec3 FragPos, vec3 ViewDir, int depthCube);
float GenerateDirectionalShadow(vec4 fragPosLightSpace);
float GeneratePointShadows(PointLight light, int depthCube);

float bias = 0.005;

vec3 sampleOffsetDirections[20] = vec3[]
(
   vec3( 1,  1,  1), vec3( 1, -1,  1), vec3(-1, -1,  1), vec3(-1,  1,  1), 
   vec3( 1,  1, -1), vec3( 1, -1, -1), vec3(-1, -1, -1), vec3(-1,  1, -1),
   vec3( 1,  1,  0), vec3( 1, -1,  0), vec3(-1, -1,  0), vec3(-1,  1,  0),
   vec3( 1,  0,  1), vec3(-1,  0,  1), vec3( 1,  0, -1), vec3(-1,  0, -1),
   vec3( 0,  1,  1), vec3( 0, -1,  1), vec3( 0, -1, -1), vec3( 0,  1, -1)
);   

void main(){
	//vec3 norm = normalize(fs_in.Normal);
	vec3 norm = texture(material.texture_normal1, fs_in.TexCoord).rgb;
	norm = normalize(norm * 2.0 - 1.0);

	mat3 tbn = transpose(fs_in.TBN);

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

	vec3 ViewDir = normalize(fs_in.ViewPos - fs_in.FragPos);
	float gamma = 2.2;

	shininess = 32 * length(vec3(texture(material.texture_specular1, fs_in.TexCoord)));

	//Directional Lighting
	vec3 result = CalcDirLight(dirLight, norm, ViewDir);
	//vec3 result = vec3(0.0f);

	//Point Lights
	//for (int i=0; i < NR_POINT_LIGHTS; i++) {
	//	result += CalcPointLight(pointLight[i], norm, fs_in.FragPos, ViewDir, i);
	//}

	//float test = GeneratePointShadows(pointLight[0], 0);

	FragColor = vec4(result, 1.0f);
	//FragColor = vec4(pow(result, vec3(1.0/gamma)), 1.0f); // Gamma Corrected Output
	float brightness = dot(FragColor.rgb, vec3(0.2126, 0.7152, 0.0722));
    if(brightness > 0.5)
        BrightColor = vec4(FragColor.rgb/5.0, 1.0);
    else
        BrightColor = vec4(0.0, 0.0, 0.0, 1.0);
}

vec3 CalcDirLight(DirLight light, vec3 Normal, vec3 ViewDir) {
	vec3 lightDir = normalize(light.direction);
	//vec3 lightDir = normalize(light.direction - fs_in.TangentFragPos);
	vec3 halfwayDir = normalize(lightDir + ViewDir);

	float diff = max(dot(Normal, lightDir), 0.0);

	float spec = pow(max(dot(Normal, halfwayDir), 0.0), shininess);

	if (diff == 0.0f){
		spec = 0.0f;
	}

	vec4 textureColour = texture(material.texture_diffuse1, fs_in.TexCoord);
	if(textureColour.a < 0.5) {
		discard;
	}

	spec = 0.0;

	vec3 ambient = light.ambient * vec3(textureColour);
	vec3 diffuse = light.diffuse * diff * vec3(textureColour);
	vec3 specular = light.specular * spec * vec3(texture(material.texture_specular1, fs_in.TexCoord));

	bias = max(0.05 * (1.0 - dot(Normal, lightDir)), 0.005);

	float shadow = GenerateDirectionalShadow(fs_in.FragPosLightSpace);

	diffuse *= (1.0 - shadow);
	specular *= (1.0 - shadow);

	return (ambient + diffuse + specular);
}

vec3 CalcPointLight(PointLight light, vec3 Normal, vec3 FragPos, vec3 ViewDir, int depthCube) {
	vec3 lightPos = light.position;
	vec3 lightDir = normalize(lightPos - FragPos);
	vec3 halfwayDir = normalize(lightDir + ViewDir);
	float distance = length(lightPos - FragPos);

	float diff = max(dot(Normal, lightDir), 0.0);
	
	float spec = pow(max(dot(Normal, halfwayDir), 0.0), shininess);

	if (diff == 0.0f) {
		spec = 0.0f;
	}

	float attenuation = 1.0 / (light.AttConstant + (light.AttLinear * distance) + (light.AttQuadratic * (distance * distance)));
	
	vec4 textureColour = texture(material.texture_diffuse1, fs_in.TexCoord);
	if(textureColour.a < 0.5) {
		discard;
	}

	spec = 0.0;

	vec3 ambient = light.ambient * vec3(textureColour);
	vec3 diffuse = light.diffuse * diff * vec3(textureColour);
	vec3 specular = light.specular * spec * vec3(texture(material.texture_specular1, fs_in.TexCoord));

	ambient *= attenuation;
	diffuse *= attenuation;
	specular *= attenuation;

	//float shadow = GeneratePointShadows(light, depthCube);

	//diffuse *= (1.0 - shadow);
	//specular *= (1.0 - shadow);

	return (ambient + diffuse + specular);
}

float GenerateDirectionalShadow(vec4 fragPosLightSpace) {
	//perspective divide
	vec3 projCoord = fragPosLightSpace.xyz / fragPosLightSpace.w;
	projCoord = projCoord * 0.5 + 0.5;
	float shadow = 0.0;
	float currentDepth = projCoord.z;
	float spread = 300.0;
	int samples  = 20;
	float viewDistance = length(fs_in.ViewPos - fs_in.FragPos);
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

float GeneratePointShadows(PointLight light, int depthCube) {
	vec3 fragToLight = fs_in.FragPos - light.position;
	float closestDepth = texture(shadowCube[depthCube], fragToLight).r;
	//closestDepth *= far_plane;
	float currentDepth = length(fragToLight);
	float bias   = 0.15;
	float shadow = 0.0;
	int samples  = 20;
	float viewDistance = length(fs_in.ViewPos - fs_in.FragPos);
	float diskRadius = (1.0 + (viewDistance / far_plane)) / 25.0;;

	//for(int i = 0; i < samples; ++i)
	//{
	//	float closestDepth = texture(depthCube, fragToLight + sampleOffsetDirections[i] * diskRadius).r;
	//	closestDepth *= far_plane;   // undo mapping [0,1]
	//	if(currentDepth - bias > closestDepth) {
	//		shadow += 1.0;
	//	}
	//}
	//shadow /= float(samples);  

	FragColor = vec4(vec3(closestDepth), 1.0);

	//shadow = 0.1;

	return shadow;
}